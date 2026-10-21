# Implementation and validation

The code and monitoring artifacts are implemented. Live deployment, startup/reconnect checks against Discord, private-response checks and the 24-hour staging soak have not been performed. No VPS files, service configuration, container images or running services were changed. Read-only promtool and Prometheus queries were used for monitoring validation.

## Completed checks

- Release solution build: exit 0, zero errors. Existing paginator-obsolescence and test-cancellation warnings remain.
- Full Microsoft.Testing.Platform suite: **718 passed, 0 failed, 0 skipped**, across Bot, Infrastructure and WebApi.
- Deployment Python suite: **23 passed**.
- Slash-command manifest: `docs/slash-commands.json is current`.
- Existing VPS promtool: **4 alert rules valid**; example scrape configuration passes syntax validation. The example is a merge fragment; validate the full VPS configuration again before any deployment.
- All **18 dashboard PromQL queries** successfully evaluated against the existing Prometheus API.
- `git diff --check`: passes.

## Requirement evidence

| Requirement from the approved plan | Evidence |
| --- | --- |
| “Disable automatic full-member downloads by default” | `DiscordConfig.AlwaysDownloadUsers` defaults to false, Startup binds it, and `.env.example` documents `Discord__AlwaysDownloadUsers=false`. |
| “Add a shared guild-member resolver returning Result<Maybe<IGuildUser>>” | `DiscordGuildMemberResolverTests.ResolveAsync_InteractionMember_AvoidsCacheAndRest`, `ResolveAsync_CachedMember_AvoidsRest`, `ResolveAsync_UnrelatedInteractionMember_UsesTargetedRest`, `ResolveAsync_AbsentMember_ReturnsSuccessfulNone`, `ResolveAsync_TransportFailure_ReturnsFailureAndDoesNotCacheIt`. |
| “Keep permission checks fail-closed.” | `DiscordGuildMemberResolverTests.HasAsync_UncachedMember_ResolvesGuildRolesAndDeniesUnrelatedPermissions`, `HasAsync_Owner_IsGrantedWithoutRoleLookup`, `HasAsync_MissingMemberOrLookupFailure_Denies`. Tag autocomplete also uses this guard; `SearchTagsWhenModifyAutoCompleteTests.GenerateSuggestionsAsync_OnlyVerifiedManagersSeeOtherAuthorsTags` pins manager, ordinary-member and failed-lookup behavior. |
| “Update profile, server, Last.fm, weather, avatar, leaderboard, tag, and settings paths to tolerate incomplete caches.” | All affected commands use the shared resolver. `Appearance_MissingGuildMember_UsesGlobalDetails`, `Appearance_RestGuildMember_UsesGuildDetails`, and `ProfileCommandsTests.GetProfileAsync_RestCompatibleMember_RendersAndCreatesGuildMemberWithoutSocketCache` verify the shared display path and public profile renderer. Live command smoke tests remain pending. |
| “Change member-dependent service and presentation interfaces from SocketGuildUser to IGuildUser where REST fallback is needed.” | Public profile-render test uses a strict `IGuildUser` mock, checks posted guild identity/image bytes and persisted member creation, and verifies the member's `Guild` property is never read. Server rendering receives the guild separately for roles and owner mentions. |
| “Update the existing two-hour cache cleanup to purge guild member caches before the global user cache” | `BackgroundServiceTests.ClearUserCache_ReleasesGuildAndGlobalMembersButPreservesBotAndInteractionMember` exercises real Discord.NET collections. `ResolveAsync_AfterCacheCleared_FetchesMemberWithoutRepopulatingCache` verifies targeted resolution after cache loss. |
| “Limit Spotify caching to 512 successful selected artist/track results with the existing one-day expiry” | `SpotifyApiServiceTests.GetArtist_CachesOnlySelectedResultAndSeparatesArtistAndTrackKeys`, `GetArtist_EmptySearch_IsNotCached`, `Cache_OverCapacity_EvictsOldResultsAndRetainsNewResults`, `Cache_FailedLoad_IsNotRetained`; dedicated size-limited cache sets one-day absolute expiration. |
| “Set each Loki sink’s queue limit to 1,000 events” | Both sinks use `RemoteLoggingConfig.LokiQueueLimit`. `LokiMemoryTests.UnavailableLoki_BufferRetainsAtMostConfiguredQueueCapacity` blocks HTTP delivery, exceeds capacity, and verifies excess log-event objects are released. An active delivery batch is additional to the queue. |
| “Add aggregate cached-member and application-cache entry gauges.” | `MemoryMetricsTests.Register_ExportsCurrentAggregateCountsWithFixedLabels` checks initial/updated values and excludes user/guild identifiers. The bot exports application and Spotify counts; API exports its application count. |
| “Filter bot panels by Pushgateway freshness.” | Dashboard memory, GC and restart queries gate pushed samples on a 120-second freshness window; the metric-age panel explicitly exposes stale staging values. All queries evaluated successfully. |
| “Public HTTP routes, slash-command names, and database schemas remain unchanged.” | Full WebApi suite and command-metadata suite pass; manifest check is current; no migrations or route changes. |
| “Validate in staging for 24 hours” | **Pending separately reviewed deployment**. See README for the controlled workload, two-hour cleanup cycles, memory-budget acceptance criteria and rollback steps. No production-memory savings or leak-resolution claim is made without this observation. |

## Reproduce local verification

```bash
dotnet build dotBento.sln --configuration Release --no-restore -m:1 -p:UseSharedCompilation=false -nodeReuse:false
dotnet test --solution dotBento.sln --configuration Release --no-build
dotnet run --project tools/dotBento.CommandDocs --configuration Release --no-build -- check --input docs/slash-commands.json
python3 -m unittest discover -s tests/deployment -v
```

The workspace had restored dependencies already; no package changes were needed. Microsoft.Testing.Platform requires local IPC sockets, so tests used the environment's approved execution outside the filesystem/network sandbox. Docker image builds were not run because the local Docker socket is inaccessible to this user. No Dockerfiles were changed.

## Remaining operational verification

Follow `README.md` before enabling monitoring or deploying images. The established failure was global host OOM, not an independently proven managed-memory leak. Reduced member downloading and bounded buffering address identifiable pressure sources, but a small staging bot cannot reproduce the production bot's 573-guild footprint. Any continuing post-GC retained-heap growth needs controlled diagnostics before further changes.
