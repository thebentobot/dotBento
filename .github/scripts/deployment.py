"""Plan deployments on the runner and execute scoped Compose commands over SSH."""

import argparse
import fcntl
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile
import time


PRODUCTION = ("bot-prod", "webapi")
STAGING = ("bot-sta", "webapi-sta")
STAGING_STACK = (*STAGING, "postgres-sta", "valkey-sta")
# Image tags owned by the release and pre-release workflows, never by the
# infrastructure sync. Everything else is reconciled from the repository.
APPLICATION_SERVICES = (*PRODUCTION, *STAGING)
BACKUP_KEEP = 5
STATE_PREFIX = "BENTO_DEPLOYMENT_STATE="
APPLICATION_PATHS = (
    "src/dotBento.Bot", "src/dotBento.WebApi", "src/dotBento.Domain",
    "src/dotBento.Infrastructure", "src/dotBento.EntityFramework",
    "src/Dockerfile", ".dockerignore", "global.json", ":(glob)**/*.props",
    ":(glob)**/*.targets", ":(glob)**/packages.lock.json",
)


def run(args, **kwargs):
    return subprocess.run(args, check=True, text=True, **kwargs)


def git(*args):
    return run(["git", *args], capture_output=True).stdout.strip()


def application_changed(before, after):
    # Missing image metadata/history must not be mistaken for matching production.
    if not before or not re.fullmatch(r"[0-9a-f]{40}", before):
        return True
    exists = subprocess.run(
        ["git", "cat-file", "-e", f"{before}^{{commit}}"], capture_output=True
    )
    if exists.returncode:
        return True
    result = subprocess.run(
        ["git", "diff", "--quiet", before, after, "--", *APPLICATION_PATHS]
    )
    if result.returncode not in (0, 1):
        raise RuntimeError("Could not compare deployed application source")
    return result.returncode == 1


def matches(state, services, revision):
    return all(
        state.get(service, {}).get("healthy", False)
        and not application_changed(state[service].get("revision"), revision)
        for service in services
    )


def staging_plan(state, revision, master_revision):
    if revision != master_revision:
        return "skip"
    if matches(state, PRODUCTION, revision):
        return "stop"
    if matches(state, STAGING, revision):
        return "keep"
    return "deploy"


def release_plan(state, revision, master_revision):
    if not all(
        state.get(service, {}).get("healthy", False)
        and state[service].get("revision") == revision
        for service in PRODUCTION
    ):
        raise RuntimeError("Production has not successfully deployed this release")
    return "keep" if application_changed(revision, master_revision) else "stop"


def parse_deployment_state(output):
    # ssh-action captures drone-ssh's success banner along with remote stdout.
    # Read one explicit record rather than treating logs as JSON or guessing
    # which JSON-looking line describes the intended host.
    records = [line[len(STATE_PREFIX):] for line in output.splitlines() if line.startswith(STATE_PREFIX)]
    if len(records) != 1:
        raise ValueError("Expected exactly one deployment state record from SSH")
    state = json.loads(records[0])
    if not isinstance(state, dict):
        raise ValueError("Invalid deployment state object")
    for service in (*PRODUCTION, *STAGING):
        snapshot = state.get(service)
        if (not isinstance(snapshot, dict) or not isinstance(snapshot.get("healthy"), bool)
                or "id" not in snapshot or "revision" not in snapshot
                or not isinstance(snapshot["id"], (str, type(None)))
                or not isinstance(snapshot["revision"], (str, type(None)))):
            raise ValueError(f"Invalid deployment state for {service}")
    return state


def inspect_deployment():
    state = {}
    for service in (*PRODUCTION, *STAGING):
        result = subprocess.run(
            ["docker", "inspect", service], capture_output=True, text=True
        )
        if result.returncode:
            state[service] = {"id": None, "healthy": False, "revision": None}
            continue
        container = json.loads(result.stdout)[0]
        labels = container["Config"].get("Labels") or {}
        health = container["State"].get("Health", {}).get("Status", "none")
        state[service] = {
            "id": container["Id"],
            "healthy": container["State"]["Running"] and health in ("healthy", "none"),
            "revision": labels.get("org.opencontainers.image.revision"),
        }
    return state


def set_images(text, images):
    """Replace the image of every named service, leaving all other config alone."""
    for service, image in images.items():
        if not re.fullmatch(r"[A-Za-z0-9._/:@-]+", image):
            raise ValueError(f"Invalid image reference for {service}")
        pattern = rf"(^  {re.escape(service)}:\n)(.*?)(?=^  [\w-]+:\s*$|^\S|\Z)"

        def replace(match):
            body, count = re.subn(
                r"^    image: .*$",
                f"    image: {image}",
                match[2], count=1, flags=re.MULTILINE,
            )
            if count != 1:
                raise ValueError(f"Missing image for {service}")
            return match[1] + body

        text, count = re.subn(pattern, replace, text, flags=re.MULTILINE | re.DOTALL)
        if count != 1:
            raise ValueError(f"Missing service {service}")
    return text


def update_images(text, services, tag):
    if not re.fullmatch(r"v\d+\.\d+\.\d+(?:-pre-[0-9a-f]{8})?", tag):
        raise ValueError("Invalid deployment image tag")
    images = {}
    for service in services:
        image = "dotbento-webapi" if service.startswith("webapi") else "dotbento"
        images[service] = f"ghcr.io/thebentobot/{image}:{tag}"
    return set_images(text, images)


def parse_images(text):
    """Read the image of every service in a Compose file, ignoring other config."""
    images, service, in_services = {}, None, False
    for line in text.splitlines():
        if re.match(r"^services:\s*$", line):
            in_services, service = True, None
            continue
        if not in_services:
            continue
        if re.match(r"^\S", line):
            break
        declaration = re.match(r"^  ([\w-]+):\s*$", line)
        if declaration:
            service = declaration.group(1)
            continue
        image = re.match(r"^    image:\s*(\S+)\s*$", line)
        if image and service:
            images[service] = image.group(1)
    return images


def postgres_major(image):
    tag = image.rsplit(":", 1)[-1]
    major = re.match(r"(\d+)", tag)
    return major.group(1) if major else None


def inspect_compose_services(path, services):
    """Confirm the given services are running and healthy in the live Compose project."""
    result = subprocess.run(
        ["docker", "compose", "-f", str(path), "ps", "--format", "json", *services],
        capture_output=True, text=True,
    )
    if result.returncode:
        raise RuntimeError("Could not inspect deployed services")
    state = {}
    for line in result.stdout.splitlines():
        if line.strip():
            item = json.loads(line)
            state[item["Service"]] = {
                "state": item.get("State"),
                "health": item.get("Health") or "none",
            }
    missing = [service for service in services if service not in state]
    if missing:
        raise RuntimeError(f"Missing deployed services: {', '.join(missing)}")
    if not all(
        snapshot["state"] == "running" and snapshot["health"] in ("healthy", "none")
        for snapshot in state.values()
    ):
        raise RuntimeError("Deployed infrastructure services are not healthy")
    return state


def prune_backups(directory):
    backups = sorted(directory.glob("docker-compose.yml.bak-*"))
    for backup in backups[:-BACKUP_KEEP]:
        backup.unlink()


def sync_infra(directory, desired_text):
    """Apply image tags from the repository Compose file to the live one.

    Only ``image:`` values change, so VPS-specific config in the live file is
    preserved. Services owned by the release workflows are never touched.
    """
    path = directory / "docker-compose.yml"
    current_text = path.read_text()
    current = parse_images(current_text)
    changes = {
        service: image
        for service, image in parse_images(desired_text).items()
        # Adding or removing services is a config change, not a tag change.
        if service in current
        and service not in APPLICATION_SERVICES
        and current[service] != image
    }
    if not changes:
        print("Infrastructure images already match the repository")
        return {}

    # A tag swap cannot upgrade an existing PostgreSQL data directory, and the
    # container would fail to start against the old volume.
    for service, image in changes.items():
        if service.startswith("postgres"):
            before, after = postgres_major(current[service]), postgres_major(image)
            if before and after and before != after:
                raise RuntimeError(
                    f"Refusing {service} {current[service]} -> {image}: "
                    "a PostgreSQL major upgrade needs a data migration"
                )

    candidate = None
    try:
        with tempfile.NamedTemporaryFile(mode="w", suffix=".yml", dir=directory, delete=False) as file:
            candidate = Path(file.name)
            file.write(set_images(current_text, changes))
        # Validate and pull before replacing the live config or recreating containers.
        compose(candidate, "config", "--quiet")
        compose(candidate, "pull", *changes)
        candidate.chmod(path.stat().st_mode & 0o777)
        candidate.replace(path)
    finally:
        if candidate is not None:
            candidate.unlink(missing_ok=True)

    stamp = time.strftime("%Y%m%dT%H%M%SZ", time.gmtime())
    (directory / f"docker-compose.yml.bak-{stamp}").write_text(current_text)
    try:
        compose(path, "up", "-d", "--no-deps", "--wait", "--wait-timeout", "300", *changes)
        inspect_compose_services(path, list(changes))
    except Exception:
        path.write_text(current_text)
        try:
            compose(path, "up", "-d", "--no-deps", *changes)
        except subprocess.CalledProcessError:
            print("Rollback restart failed; restore docker-compose.yml manually", file=sys.stderr)
        raise
    prune_backups(directory)
    print("Updated " + ", ".join(f"{service} -> {image}" for service, image in changes.items()))
    return changes


def compose(path, *args):
    run(["docker", "compose", "-f", str(path), *args], cwd=path.parent)


def deploy(directory, environment, tag, revision):
    if not re.fullmatch(r"[0-9a-f]{40}", revision):
        raise ValueError("Invalid deployment source revision")
    services = STAGING if environment == "staging" else PRODUCTION
    path = directory / "docker-compose.yml"
    text = update_images(path.read_text(), services, tag)
    # Validate and pull before replacing the live config or recreating containers.
    with tempfile.NamedTemporaryFile(mode="w", suffix=".yml", dir=directory, delete=False) as file:
        candidate = Path(file.name)
        file.write(text)
    try:
        compose(candidate, "config", "--quiet")
        compose(candidate, "pull", *services)
        candidate.chmod(path.stat().st_mode & 0o777)
        candidate.replace(path)
    finally:
        candidate.unlink(missing_ok=True)
    if environment == "staging":
        compose(path, "up", "-d", "--wait", "--wait-timeout", "180", "postgres-sta", "valkey-sta")
    compose(path, "up", "-d", "--no-deps", "--wait", "--wait-timeout", "300", *services)
    state = inspect_deployment()
    if not all(state[service]["healthy"] and state[service]["revision"] == revision for service in services):
        raise RuntimeError("Deployed containers are not healthy at the expected source revision")


def stop_staging(directory, expected):
    current = inspect_deployment()
    if not all(current[service]["healthy"] for service in PRODUCTION):
        raise RuntimeError("Refusing to stop staging while production is unhealthy")
    if any(current[service]["id"] != expected[service]["id"] for service in (*PRODUCTION, *STAGING)):
        raise RuntimeError("Deployment changed after planning; retry without stopping staging")
    compose(directory / "docker-compose.yml", "stop", "--timeout", "60", *STAGING_STACK)


def render_remote(action, destination, payload=None):
    # ssh-action sends this generated file; no helper installation on the VPS is needed.
    source = Path(__file__).read_text()
    script = "#!/bin/sh\nset -eu\n"
    if payload is not None:
        # Ship the repository Compose file inside the script so the VPS needs no
        # checkout, and expose it to the action through an environment variable.
        script += (
            "BENTO_PAYLOAD=$(mktemp)\n"
            "trap 'rm -f \"$BENTO_PAYLOAD\"' EXIT\n"
            "cat > \"$BENTO_PAYLOAD\" <<'BENTO_COMPOSE_PAYLOAD'\n"
            + payload.rstrip("\n") + "\nBENTO_COMPOSE_PAYLOAD\n"
            "export BENTO_DESIRED_COMPOSE=\"$BENTO_PAYLOAD\"\n"
        )
    script += (
        "python3 - " + action
        + " <<'BENTO_DEPLOYMENT_PYTHON'\n" + source + "\nBENTO_DEPLOYMENT_PYTHON\n"
    )
    Path(destination).write_text(script)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("action", choices=(
        "plan-staging", "plan-release", "render", "inspect", "deploy-staging",
        "deploy-production", "stop-staging", "apply-infra",
    ))
    parser.add_argument("arguments", nargs="*")
    args = parser.parse_args()
    if args.action == "render":
        action, destination, *payload = args.arguments
        render_remote(action, destination, Path(payload[0]).read_text() if payload else None)
        return
    if args.action.startswith("plan-"):
        state = parse_deployment_state(os.environ["DEPLOYMENT_STATE"])
        revision = git("rev-parse", "HEAD")
        master = git("rev-parse", "origin/master")
        action = (staging_plan if args.action == "plan-staging" else release_plan)(state, revision, master)
        output = {"action": action, "revision": revision}
        if action == "deploy":
            version = Path("version.txt").read_text().strip()
            output["tag"] = f"v{version}-pre-{revision[:8]}"
        with open(os.environ["GITHUB_OUTPUT"], "a") as file:
            file.write("".join(f"{key}={value}\n" for key, value in output.items()))
        print(json.dumps(output))
        return
    directory = Path(os.environ.get("BENTO_DIRECTORY", str(Path.home() / "bento")))
    # Also protect against two manually triggered deployments writing Compose together.
    with (directory / ".deployment.lock").open("a") as lock:
        fcntl.flock(lock, fcntl.LOCK_EX)
        if args.action == "inspect":
            print(STATE_PREFIX + json.dumps(inspect_deployment()))
        elif args.action == "stop-staging":
            stop_staging(directory, parse_deployment_state(os.environ["DEPLOYMENT_STATE"]))
        elif args.action == "apply-infra":
            sync_infra(directory, Path(os.environ["BENTO_DESIRED_COMPOSE"]).read_text())
        else:
            environment = "staging" if args.action == "deploy-staging" else "production"
            deploy(directory, environment, os.environ["DEPLOY_TAG"], os.environ["DEPLOY_REVISION"])


if __name__ == "__main__":
    main()
