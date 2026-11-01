# dotBento memory investigation

## Evidence from the VPS

Read-only inspection on October 19–20, 2026 found a 1.9 GiB RAM VPS running production, monitoring and image/media services. Staging bot/API/database/cache containers were stopped. Production bot and API were v2.4.1, image revision `86bd3c061c9742857015412fd7303c2b11093f58`; stopped staging was `ecd25a7328f00f47e27b7ceec48cd1261666d91a`.

- Previous-boot kernel journals confirm global OOM kills involving Chrome and `dotnet` on October 13 and 15. On October 15 at 08:23 UTC the kernel reported no swap configured and killed a `dotnet` process with roughly 321 MiB resident memory. The victim alone does not establish the source of exhaustion.
- At the first snapshot, production bot used about 363 MiB, API 149 MiB, and host swap usage was 401 MiB of the now-configured 2 GiB swap.
- Week-long Prometheus samples showed available memory as low as 25 MiB and bot working-set spikes around 696 MiB. These are sampled maxima; narrower spikes can be missed.
- The API managed-memory estimate stayed below 27 MiB over the inspected day. Several bot process lifetimes returned to a managed-memory estimate around 40–60 MiB after much larger peaks. This does not establish an indefinitely growing retained heap.
- The production bot reported 573 guilds. Automatic full-member downloading was enabled. The old scheduled cleanup purged the global user index but not each guild's separate member collection.
- Pushgateway continued exposing stale staging bot values after staging stopped. The API scrape list contained only production, leaving staging API memory unobserved.

These findings establish host memory exhaustion and unnecessary cache pressure, not a proven managed-memory leak. Hangfire job retention, other local caches, native allocations and image-renderer concurrency remain hypotheses if growth persists after these changes. Do not present them as confirmed causes.

## Install the monitoring artifacts (separate deployment)

These files are reviewable artifacts; implementation does not modify the VPS or deploy application images.

1. Merge `prometheus-memory.example.yml` into the existing VPS-managed Prometheus configuration. Keep existing scrape jobs and alert routing; avoid duplicate job names. The production API container is named `webapi`, not `webapi-prod`.
2. Mount/copy `memory-alerts.yml` under `/etc/prometheus/` and add its path to `rule_files`. Existing Compose mounts `./prometheus` there. Retain `honor_labels: false` for Pushgateway so the `exported_job` selectors remain valid. Verify node-exporter exposes `node_vmstat_oom_kill`.
3. Validate with `promtool check rules memory-alerts.yml` and `promtool check config prometheus.yml` before reloading Prometheus through the existing deployment procedure. No API credentials are needed for `/metrics`.
4. Provision the dashboards from files: mount `grafana-provisioning/` at `/etc/grafana/provisioning` (read-only) and copy `dotbento-memory.json` and `dotbento-bot-overview.json` into `grafana-provisioning/dashboards/`, next to `dashboards.yaml`. Grafana re-reads the directory every 30 seconds, so no restart is needed after a change. Do not replace existing provisioning or datasources; to import by hand instead, use the Grafana UI and pick the Prometheus and Loki datasources.
5. Check all panels after deploying the new application image. The new cache gauges are absent on older images. Bot memory panels exclude pushes older than 120 seconds. GC rate panels and restart panels use reset-aware rates and process start times respectively; sampled start-time changes cannot detect every restart between samples.

Staging targets are included so a controlled validation can observe them, but stopped staging is expected to show `up=0`. Only production API availability and production bot freshness alert by default. Existing Loki labels (`app`, `environment`, `level`) remain compatible.

## Dashboards

Both dashboards are provisioned into the `dotBento` folder from `grafana-provisioning/dashboards/`.

`dotBento memory investigation` (`dotbento-memory.json`) is the read-only investigation view described above: working set, managed heap, GC generation sizes and fragmentation, host RAM, swap, cache counts and metric age.

`dotBento bot overview` (`dotbento-bot-overview.json`) is the day-to-day view and refreshes every minute:

- **At a glance** — guilds, registered users and guilds, supporters, gateway latency and how stale the pushed metrics are.
- **Slash commands** — executions and failures per command, p95 handler time per command, the top commands over 24 hours and a failure ratio.
- **Interactions and gateway events** — user commands, autocompletes, buttons, select menus, modals, and the Discord gateway events the handlers observe.
- **Audience and cache** — database populations next to the Discord client cache (guild members, channels, guilds) and the local application caches.
- **Performance** — working set, private and managed memory, CPU, GC collections by generation, allocation rate, exceptions, process restarts and gateway connection state.
- **Database** — EF Core query and SaveChanges rates, Npgsql pool state, query p95 and execution-strategy failures.
- **Host (VPS)** — available RAM, swap, CPU, load, disk, detected OOM kills and the API scrape state.
- **Activity, reminders and media** — daily, weekly and monthly active users from the activity tracker, reminder delivery and failure rates, p95 image render/upload latency, and Discord 429 rate limits.
- **Logs** — warning/error lines and recent logs from Loki.

Active users are tracked per UTC day by `ActivityTracker`, which buffers the day sets in memory and flushes them to the distributed cache every two minutes; the cache retains 40 days. A restart loses at most the interactions recorded since the last flush, so `bot_active_users_count_1d` can dip briefly after a deployment.

Panels using `bot_gateway_*`, `dotbento_discord_cached_*`, `bot_active_users_count_*`, `bot_reminders_*`, `bot_discord_rate_limits_total` and the render/upload histograms stay empty until an image carrying those metrics is deployed. The rate-limit counter reads Discord.Net's `Rate limit triggered` warning, so it counts HTTP 429 responses only; the library's preemptive throttling is verbose and excluded. Both Loki datasources point at `http://loki:3100`; the older one used to target `http://localhost:3100`, which resolves to the Grafana container itself.

## Configuration and behavior

`Discord__AlwaysDownloadUsers=false` is the default. Setting it to `true` restores full-member downloading on guild availability for comparison or compatibility; scheduled member cleanup still runs every two hours. Guild-member intents remain enabled. Commands use interaction members, cached members or one targeted REST lookup, rather than a full-member download. Required-member failures produce a controlled response; decorative details can use global identity. Permission checks deny when a member cannot be resolved.

The Spotify result cache retains at most 512 selected results, each for at most one day; search misses and failures are not cached. It is separately owned and does not impose a size limit on unrelated application caches. Both Loki sinks buffer at most 1,000 queued log events, plus an active batch; when full, the sink can drop new events. Console logging remains available.

## Staging acceptance and production handoff

Build Release and run the full MTP suite and command manifest check before deployment. Keep staging stopped outside controlled validation; do not start all staging services alongside production when host available RAM is already below 200 MiB. Perform builds and synthetic stress on a development/CI host, not the small production VPS.

After a separately reviewed staging deployment:

- Exercise profile/user/server/avatar, Last.fm, weather, leaderboards, tag management/autocomplete and server settings for an actor and an uncached target member. Check missing members, DMs where supported, owner/admin permission grants and ordinary-member denial. Confirm private replies stay private.
- Test cold startup and a reconnect. Verify the bot's own permissions and role rendering remain correct. Run the scheduled member cleanup and repeat the commands. Cache member count should fall toward one bot member per available guild, then grow with relevant gateway events.
- Exercise profile/collage images and media uploads; confirm streams close after successful and failed uploads. Simulate Loki failure only on an isolated staging/development logger. Confirm console logs continue and buffered log objects remain bounded.
- Observe at least 24 hours and multiple two-hour cleanup cycles. Compare equal workloads within each process lifetime, correlate managed heap with Gen2 collections, and compare working set with host available RAM and swap activity. Do not treat reduced RSS alone as proof of a fixed leak.
- Acceptance: no OOM kills; production remains healthy; available RAM stays above 200 MiB during representative workload; cache counts stay bounded; post-GC retained-memory lows do not rise continuously across cleanup cycles. If this budget cannot be met, reduce co-located services or increase capacity before production rollout; do not tighten arbitrary heap limits.

Live staging validation and production deployment are pending. A separate staging bot with few guilds cannot prove the 573-guild production memory savings; report that limitation explicitly in any rollout review. Keep the previous image available for rollback if command behavior regresses. Any continuing sustained heap growth requires a separately controlled diagnostic capture, not forced GC or periodic restarts in production.
