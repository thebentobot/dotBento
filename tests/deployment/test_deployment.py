import copy
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch


ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / ".github/scripts/deployment.py"
spec = importlib.util.spec_from_file_location("deployment", SCRIPT)
deployment = importlib.util.module_from_spec(spec)
spec.loader.exec_module(deployment)


def state_at(revision):
    return {
        service: {"id": f"container-{service}", "healthy": True, "revision": revision}
        for service in (*deployment.PRODUCTION, *deployment.STAGING)
    }


def ssh_output(state):
    # capture_stdout includes drone-ssh's banner, as seen in failed run 39331479075.
    return (
        "SSH session notice\n{\"unrelated\": true}\n"
        "BENTO_DEPLOYMENT_STATE=" + json.dumps(state) + "\n"
        "===============================================\n"
        "✅ Successfully executed commands to all hosts.\n"
        "===============================================\n"
    )


class DeploymentPlansTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.original_directory = Path.cwd()
        os.chdir(self.temp.name)
        self.addCleanup(os.chdir, self.original_directory)
        self.git("init", "-q")
        self.change("src/dotBento.Bot/Bot.cs", "original application")
        self.change("version.txt", "2.3.1")
        self.production = self.commit()

    def git(self, *args):
        return subprocess.run(["git", *args], check=True, capture_output=True, text=True).stdout.strip()

    def change(self, name, text):
        path = Path(name)
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text)

    def commit(self):
        self.git("add", ".")
        self.git("-c", "core.hooksPath=/dev/null", "-c", "commit.gpgsign=false",
                 "-c", "user.name=Deployment Tests", "-c", "user.email=tests@example.invalid",
                 "commit", "-qm", "fixture")
        return self.git("rev-parse", "HEAD")

    def test_unreleased_application_changes_start_staging(self):
        self.change("src/dotBento.Bot/Bot.cs", "unreleased application")
        master = self.commit()
        self.assertEqual("deploy", deployment.staging_plan(state_at(self.production), master, master))

    def test_docs_release_metadata_and_workflow_changes_do_not_start_staging(self):
        self.change("README.md", "documentation")
        self.change("version.txt", "2.3.2")
        self.change("CHANGELOG.md", "release notes")
        self.change(".github/workflows/docker-release.yml", "deployment workflow")
        master = self.commit()
        self.assertEqual("stop", deployment.staging_plan(state_at(self.production), master, master))

    def test_shared_code_dockerfile_and_build_settings_require_staging(self):
        for name in ["src/dotBento.Infrastructure/Service.cs", "src/dotBento.WebApi/Program.cs",
                     "src/dotBento.EntityFramework/Migration.cs", "src/Dockerfile",
                     ".dockerignore", "global.json", "Directory.Build.props", "Directory.Packages.props"]:
            with self.subTest(path=name):
                self.change(name, "changed build input")
                candidate = self.commit()
                self.assertTrue(deployment.application_changed(self.production, candidate))
                self.production = candidate

    def test_stale_queued_prerelease_does_not_deploy_or_stop_staging(self):
        self.change("src/dotBento.Bot/Bot.cs", "newer master")
        master = self.commit()
        self.assertEqual("skip", deployment.staging_plan(state_at(self.production), self.production, master))

    def test_already_running_candidate_is_kept_without_rebuilding(self):
        self.change("src/dotBento.Bot/Bot.cs", "candidate")
        master = self.commit()
        state = state_at(self.production)
        for service in deployment.STAGING:
            state[service]["revision"] = master
        self.assertEqual("keep", deployment.staging_plan(state, master, master))

    def test_both_production_images_must_match(self):
        self.change("src/dotBento.Bot/Bot.cs", "candidate")
        master = self.commit()
        state = state_at(self.production)
        state["bot-prod"]["revision"] = master
        self.assertEqual("deploy", deployment.staging_plan(state, master, master))

    def test_missing_metadata_unknown_history_or_unhealthy_production_keeps_staging_available(self):
        for revision, healthy in [(None, True), ("f" * 40, True), (self.production, False)]:
            with self.subTest(revision=revision, healthy=healthy):
                state = state_at(self.production)
                for service in (*deployment.PRODUCTION, *deployment.STAGING):
                    state[service].update(revision=revision, healthy=healthy)
                self.assertEqual("deploy", deployment.staging_plan(state, self.production, self.production))

    def test_successful_release_stops_staging_when_master_has_caught_up(self):
        self.change("version.txt", "2.3.2")
        master = self.commit()
        self.assertEqual("stop", deployment.release_plan(state_at(self.production), self.production, master))

    def test_newer_application_changes_during_release_keep_staging_running(self):
        self.change("src/dotBento.Bot/Bot.cs", "next release")
        master = self.commit()
        self.assertEqual("keep", deployment.release_plan(state_at(self.production), self.production, master))

    def test_failed_or_different_production_release_cannot_stop_staging(self):
        for field, value in [("healthy", False), ("revision", "f" * 40)]:
            with self.subTest(field=field):
                state = state_at(self.production)
                state["webapi"][field] = value
                with self.assertRaisesRegex(RuntimeError, "not successfully deployed"):
                    deployment.release_plan(state, self.production, self.production)

    def test_cli_emits_candidate_tag_and_revision(self):
        self.change("src/dotBento.Bot/Bot.cs", "candidate")
        candidate = self.commit()
        self.git("update-ref", "refs/remotes/origin/master", candidate)
        output = Path("outputs.txt")
        result = subprocess.run(
            [sys.executable, str(SCRIPT), "plan-staging"], check=True, capture_output=True, text=True,
            env={**os.environ, "DEPLOYMENT_STATE": ssh_output(state_at(self.production)),
                 "GITHUB_OUTPUT": str(output)},
        )
        self.assertEqual("deploy", json.loads(result.stdout)["action"])
        self.assertEqual({"action": "deploy", "revision": candidate, "tag": f"v2.3.1-pre-{candidate[:8]}"},
                         dict(line.split("=", 1) for line in output.read_text().splitlines()))

    def test_cli_plans_ignore_ssh_action_banners(self):
        self.change("src/dotBento.Bot/Bot.cs", "candidate")
        candidate = self.commit()
        self.git("update-ref", "refs/remotes/origin/master", candidate)
        for action, state, expected in [
            ("plan-staging", state_at(self.production), "deploy"),
            ("plan-release", state_at(candidate), "stop"),
        ]:
            with self.subTest(action=action):
                output = Path(f"{action}.txt")
                result = subprocess.run(
                    [sys.executable, str(SCRIPT), action], check=True, capture_output=True, text=True,
                    env={**os.environ, "DEPLOYMENT_STATE": ssh_output(state), "GITHUB_OUTPUT": str(output)},
                )
                self.assertEqual(expected, json.loads(result.stdout)["action"])
                self.assertIn(f"action={expected}\n", output.read_text())

    def test_state_parser_rejects_missing_duplicate_or_invalid_records(self):
        valid = ssh_output(state_at(self.production))
        for output in ["", json.dumps(state_at(self.production)), valid + valid,
                       "BENTO_DEPLOYMENT_STATE=not-json", "BENTO_DEPLOYMENT_STATE=[]",
                       "BENTO_DEPLOYMENT_STATE={}"]:
            with self.subTest(output=output):
                with self.assertRaises(ValueError):
                    deployment.parse_deployment_state(output)


class RemoteDeploymentTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.directory = Path(self.temp.name)
        self.compose = self.directory / "docker-compose.yml"
        self.original = (ROOT / "src/docker-compose.prod.yml").read_text()
        self.compose.write_text(self.original)
        self.revision = "a" * 40
        self.state = state_at(self.revision)

    def retag(self, service, image):
        """Change one image so tests never depend on Renovate's current tags."""
        return deployment.set_images(self.original, {service: image})

    def postgres_upgrade(self, bump):
        """Build a postgres fixture image one minor or major version ahead."""
        current = deployment.parse_images(self.original)["postgres-prod"]
        major, minor, suffix = re.match(r"postgres:(\d+)\.(\d+)(.*)", current).groups()
        target = (f"postgres:{major}.{int(minor) + 1}{suffix}" if bump == "minor"
                  else f"postgres:{int(major) + 1}.0{suffix}")
        return deployment.set_images(
            self.original, {"postgres-prod": target, "postgres-sta": target}
        ), target

    def test_production_updates_only_production_images(self):
        updated = deployment.update_images(self.original, deployment.PRODUCTION, "v2.3.9")
        self.assertIn("ghcr.io/thebentobot/dotbento:v2.3.9", updated)
        self.assertIn("ghcr.io/thebentobot/dotbento-webapi:v2.3.9", updated)
        for service in deployment.STAGING:
            before = self.original.split(f"  {service}:\n", 1)[1].split("    container_name:", 1)[0]
            after = updated.split(f"  {service}:\n", 1)[1].split("    container_name:", 1)[0]
            self.assertEqual(before, after)

    def test_staging_deployment_starts_database_and_cache_before_apps(self):
        with patch.object(deployment, "run") as run, patch.object(deployment, "inspect_deployment", return_value=self.state):
            deployment.deploy(self.directory, "staging", "v2.3.9-pre-aaaaaaaa", self.revision)
        commands = [call.args[0][4:] for call in run.call_args_list]
        self.assertEqual(["pull", "bot-sta", "webapi-sta"], commands[1])
        self.assertEqual(["up", "-d", "--wait", "--wait-timeout", "180", "postgres-sta", "valkey-sta"], commands[2])
        self.assertEqual(["up", "-d", "--no-deps", "--wait", "--wait-timeout", "300", "bot-sta", "webapi-sta"], commands[3])
        self.assertIn("ghcr.io/thebentobot/dotbento:v2.3.9-pre-aaaaaaaa", self.compose.read_text())
        self.assertNotIn("bot-prod", [arg for command in commands for arg in command])

    def test_pull_failure_keeps_original_config_and_does_not_restart_services(self):
        def fail_pull(args, **kwargs):
            if "pull" in args:
                raise subprocess.CalledProcessError(1, args)
        with patch.object(deployment, "run", side_effect=fail_pull) as run:
            with self.assertRaises(subprocess.CalledProcessError):
                deployment.deploy(self.directory, "production", "v2.3.9", self.revision)
        self.assertEqual(self.original, self.compose.read_text())
        self.assertFalse(any("up" in call.args[0] for call in run.call_args_list))

    def test_failed_production_health_check_never_stops_staging(self):
        def fail_wait(args, **kwargs):
            if "up" in args:
                raise subprocess.CalledProcessError(1, args)
        with patch.object(deployment, "run", side_effect=fail_wait) as run:
            with self.assertRaises(subprocess.CalledProcessError):
                deployment.deploy(self.directory, "production", "v2.3.9", self.revision)
        self.assertFalse(any("stop" in call.args[0] for call in run.call_args_list))

    def test_production_deployment_requires_both_healthy_images_at_expected_revision(self):
        for field, value in [("revision", "b" * 40), ("healthy", False)]:
            with self.subTest(field=field):
                changed = copy.deepcopy(self.state)
                changed["webapi"][field] = value
                with patch.object(deployment, "run"), patch.object(deployment, "inspect_deployment", return_value=changed):
                    with self.assertRaisesRegex(RuntimeError, "expected source revision"):
                        deployment.deploy(self.directory, "production", "v2.3.9", self.revision)

    def test_staging_stop_preserves_data_and_only_stops_four_staging_services(self):
        with patch.object(deployment, "inspect_deployment", return_value=self.state), patch.object(deployment, "run") as run:
            deployment.stop_staging(self.directory, self.state)
        self.assertEqual(["stop", "--timeout", "60", "bot-sta", "webapi-sta", "postgres-sta", "valkey-sta"], run.call_args.args[0][4:])
        self.assertEqual(self.original, self.compose.read_text())

    def test_changed_containers_or_unhealthy_production_prevent_staging_stop(self):
        for service, field, value in [("bot-sta", "id", "new-container"),
                                      ("webapi", "id", "new-production"),
                                      ("webapi", "healthy", False)]:
            with self.subTest(service=service, field=field):
                changed = copy.deepcopy(self.state)
                changed[service][field] = value
                with patch.object(deployment, "inspect_deployment", return_value=changed), patch.object(deployment, "run") as run:
                    with self.assertRaises(RuntimeError):
                        deployment.stop_staging(self.directory, self.state)
                run.assert_not_called()

    def test_invalid_tag_or_missing_service_cannot_modify_compose(self):
        for services, tag in [(deployment.PRODUCTION, "v2.3.9; echo unsafe"), (("missing",), "v2.3.9")]:
            with self.subTest(services=services, tag=tag):
                with self.assertRaises(ValueError):
                    deployment.update_images(self.original, services, tag)
        self.assertEqual(self.original, self.compose.read_text())

    def test_infrastructure_sync_changes_only_image_tags(self):
        desired = self.retag("prometheus", "prom/prometheus:v99.9.9")
        with patch.object(deployment, "run") as run, \
                patch.object(deployment, "inspect_compose_services", return_value={}):
            changes = deployment.sync_infra(self.directory, desired)
        self.assertEqual({"prometheus": "prom/prometheus:v99.9.9"}, changes)
        self.assertEqual(desired, self.compose.read_text())
        commands = [call.args[0][4:] for call in run.call_args_list]
        self.assertEqual(["config", "--quiet"], commands[0])
        self.assertEqual(["pull", "prometheus"], commands[1])
        self.assertEqual(["up", "-d", "--no-deps", "--wait", "--wait-timeout", "300", "prometheus"], commands[2])
        backups = list(self.directory.glob("docker-compose.yml.bak-*"))
        self.assertEqual([self.original], [backup.read_text() for backup in backups])

    def test_infrastructure_sync_ignores_application_and_unknown_services(self):
        desired = deployment.update_images(
            self.original, deployment.APPLICATION_SERVICES, "v9.9.9"
        )
        with patch.object(deployment, "run") as run:
            self.assertEqual({}, deployment.sync_infra(self.directory, desired))
        run.assert_not_called()
        self.assertEqual(self.original, self.compose.read_text())

    def test_infrastructure_sync_is_a_noop_when_tags_already_match(self):
        with patch.object(deployment, "run") as run:
            self.assertEqual({}, deployment.sync_infra(self.directory, self.original))
        run.assert_not_called()
        self.assertFalse(list(self.directory.glob("docker-compose.yml.bak-*")))

    def test_infrastructure_sync_does_not_add_services_missing_from_the_vps(self):
        desired = self.original.replace(
            "  # Optional receiver for the bot metrics pusher; consumed by Prometheus.",
            "  extra-exporter:\n    image: prom/example:v1.0.0\n\n"
            "  # Optional receiver for the bot metrics pusher; consumed by Prometheus.",
        )
        self.assertIn("extra-exporter", desired)
        with patch.object(deployment, "run") as run:
            self.assertEqual({}, deployment.sync_infra(self.directory, desired))
        run.assert_not_called()
        self.assertEqual(self.original, self.compose.read_text())

    def test_postgres_major_upgrade_is_refused_before_touching_the_live_file(self):
        desired, image = self.postgres_upgrade("major")
        with patch.object(deployment, "run") as run:
            with self.assertRaisesRegex(RuntimeError, "major upgrade"):
                deployment.sync_infra(self.directory, desired)
        run.assert_not_called()
        self.assertEqual(self.original, self.compose.read_text())

    def test_postgres_patch_upgrade_is_applied(self):
        desired, image = self.postgres_upgrade("minor")
        with patch.object(deployment, "run") as run, \
                patch.object(deployment, "inspect_compose_services", return_value={}):
            changes = deployment.sync_infra(self.directory, desired)
        self.assertEqual({"postgres-prod": image, "postgres-sta": image}, changes)
        self.assertIn(f"    image: {image}", self.compose.read_text())
        self.assertEqual(["pull", "postgres-prod", "postgres-sta"],
                         [call.args[0][4:] for call in run.call_args_list][1])

    def test_infrastructure_pull_failure_keeps_the_live_file(self):
        desired = self.retag("nginx", "nginx:9.9.9-alpine")

        def fail_pull(args, **kwargs):
            if "pull" in args:
                raise subprocess.CalledProcessError(1, args)
        with patch.object(deployment, "run", side_effect=fail_pull) as run:
            with self.assertRaises(subprocess.CalledProcessError):
                deployment.sync_infra(self.directory, desired)
        self.assertEqual(self.original, self.compose.read_text())
        self.assertFalse(any("up" in call.args[0] for call in run.call_args_list))

    def test_failed_infrastructure_restart_restores_the_previous_file(self):
        desired = self.retag("prometheus", "prom/prometheus:v99.9.9")

        def fail_up(args, **kwargs):
            if "up" in args:
                raise subprocess.CalledProcessError(1, args)
        with patch.object(deployment, "run", side_effect=fail_up):
            with self.assertRaises(subprocess.CalledProcessError):
                deployment.sync_infra(self.directory, desired)
        self.assertEqual(self.original, self.compose.read_text())

    def test_unhealthy_infrastructure_after_restart_rolls_back(self):
        desired = self.retag("prometheus", "prom/prometheus:v99.9.9")
        with patch.object(deployment, "run"), \
                patch.object(deployment, "inspect_compose_services",
                             side_effect=RuntimeError("Deployed infrastructure services are not healthy")):
            with self.assertRaisesRegex(RuntimeError, "not healthy"):
                deployment.sync_infra(self.directory, desired)
        self.assertEqual(self.original, self.compose.read_text())

    def test_inspect_compose_services_rejects_unhealthy_or_missing_services(self):
        healthy = json.dumps({"Service": "prometheus", "State": "running", "Health": ""})
        restarting = json.dumps({"Service": "prometheus", "State": "restarting", "Health": ""})
        unhealthy = json.dumps({"Service": "prometheus", "State": "running", "Health": "unhealthy"})
        for output, services in [(healthy, ["prometheus"]), (healthy + "\n" + healthy, ["prometheus"])]:
            with self.subTest(output=output):
                result = subprocess.CompletedProcess([], 0, stdout=output, stderr="")
                with patch.object(subprocess, "run", return_value=result):
                    deployment.inspect_compose_services(self.compose, services)
        for output in [restarting, unhealthy, ""]:
            with self.subTest(output=output):
                result = subprocess.CompletedProcess([], 0, stdout=output, stderr="")
                with patch.object(subprocess, "run", return_value=result):
                    with self.assertRaises(RuntimeError):
                        deployment.inspect_compose_services(self.compose, ["prometheus"])

    def test_generated_infrastructure_script_embeds_the_desired_compose_file(self):
        script = self.directory / "infra.sh"
        deployment.render_remote("apply-infra", script, self.original)
        subprocess.run(["sh", "-n", str(script)], check=True)
        text = script.read_text()
        self.assertIn("BENTO_COMPOSE_PAYLOAD", text)
        self.assertIn("export BENTO_DESIRED_COMPOSE=", text)
        self.assertIn("python3 - apply-infra", text)
        self.assertIn(f"    image: {deployment.parse_images(self.original)['prometheus']}", text)

    def test_generated_remote_script_is_valid_shell(self):
        script = self.directory / "inspect.sh"
        deployment.render_remote("inspect", script)
        subprocess.run(["sh", "-n", str(script)], check=True)
        self.assertIn("python3 - inspect", script.read_text())

    def test_remote_scripts_inspect_and_stop_using_environment_payload(self):
        # Exercise the exact standalone scripts transferred by ssh-action, using
        # a fake Docker executable so this cannot touch running containers.
        docker = self.directory / "docker"
        containers = {}
        for service, state in self.state.items():
            containers[service] = [{
                "Id": state["id"],
                "Config": {"Labels": {"org.opencontainers.image.revision": state["revision"]},
                           "Env": ["PASSWORD=must-not-be-returned"]},
                "State": {"Running": True, "Health": {"Status": "healthy"}},
            }]
        docker.write_text(
            f"#!{sys.executable}\nimport json, sys\nfrom pathlib import Path\n"
            f"containers = {containers!r}\n"
            "if sys.argv[1] == 'inspect':\n"
            "    print(json.dumps(containers[sys.argv[2]]))\n"
            "else:\n"
            f"    Path({str(self.directory / 'commands.json')!r}).write_text(json.dumps(sys.argv[1:]))\n"
        )
        docker.chmod(0o700)
        env = {**os.environ, "PATH": f"{self.directory}:{os.environ['PATH']}",
               "BENTO_DIRECTORY": str(self.directory)}
        script = self.directory / "remote.sh"
        deployment.render_remote("inspect", script)
        result = subprocess.run(["sh", str(script)], env=env, check=True, capture_output=True, text=True)
        self.assertEqual("BENTO_DEPLOYMENT_STATE=" + json.dumps(self.state) + "\n", result.stdout)
        self.assertNotIn("must-not-be-returned", result.stdout)
        deployment.render_remote("stop-staging", script)
        subprocess.run(["sh", str(script)], env={**env, "DEPLOYMENT_STATE": ssh_output(self.state)}, check=True)
        self.assertEqual(
            ["compose", "-f", str(self.compose), "stop", "--timeout", "60", *deployment.STAGING_STACK],
            json.loads((self.directory / "commands.json").read_text()),
        )
        (self.directory / "commands.json").unlink()
        for invalid in ["missing state record", ssh_output(self.state) * 2,
                        "BENTO_DEPLOYMENT_STATE=not-json"]:
            with self.subTest(invalid=invalid):
                result = subprocess.run(["sh", str(script)], env={**env, "DEPLOYMENT_STATE": invalid},
                                        capture_output=True, text=True)
                self.assertNotEqual(0, result.returncode)
                self.assertFalse((self.directory / "commands.json").exists())


if __name__ == "__main__":
    unittest.main()
