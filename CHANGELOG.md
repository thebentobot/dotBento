# Changelog

## [1.1.0](https://github.com/thebentobot/dotBento/compare/v1.0.0...v1.1.0) (2024-08-09)


### Features

* **bentoService:** add update bento sender date method ([97526dd](https://github.com/thebentobot/dotBento/commit/97526ddd70e799d6c7c38928893267f5be293603))
* **cmd:** add basic about bento cmd ([1c2d80d](https://github.com/thebentobot/dotBento/commit/1c2d80dd3ec9af7c4578076b8a7752c4c44c573b))
* **cmd:** add bento command ([49360c9](https://github.com/thebentobot/dotBento/commit/49360c909427408a9788d2bd7aec78fa9be09aa1))
* **cmd:** add choose cmd ([8cae0fb](https://github.com/thebentobot/dotBento/commit/8cae0fb96bc9ae319e3560bbe2bd490826cf0bb3))
* **cmd:** add game cmds ([4d99296](https://github.com/thebentobot/dotBento/commit/4d99296bbcb626ab1a31f49e8e1041e8eb6a9b2d))
* **cmd:** add guild member info and guild info ([d1771ed](https://github.com/thebentobot/dotBento/commit/d1771ed34e2ec1d4641241fffc9c5f0aa0807ef4))
* **cmd:** add urban dict ([9369ab8](https://github.com/thebentobot/dotBento/commit/9369ab8a3c3d011242f90e57e427484b3feb4587))
* **cmd:** add user info cmd ([61061c0](https://github.com/thebentobot/dotBento/commit/61061c01349edb2611ebe054dbca95234fd9d355))
* **cmd:** add weather command ([fcf160c](https://github.com/thebentobot/dotBento/commit/fcf160cb386f51a48417ac20e973dbfc5bcbf38e))
* **docker-prerelease:** fix ssh job ([0bcad0e](https://github.com/thebentobot/dotBento/commit/0bcad0e13fab261e9d0a9dd74ca7c217f67866f4))
* **dotBento.Infrastructure:** add inital dotbento.Infrastructure ([50a9bf3](https://github.com/thebentobot/dotBento/commit/50a9bf3e13e62d7f45423fcaedd9d02364f0c30f))
* **game:** add game entities, enums and extensions to domain ([0b4be1e](https://github.com/thebentobot/dotBento/commit/0b4be1e9b4cef16b31eab8cf26ac71b2067a3d25))
* **lastFm:** add lastfm commands ([3faa242](https://github.com/thebentobot/dotBento/commit/3faa242ea45cf496398e1b2a47aa8c964518e2b7))
* **LastFmCommand:** Add text command ([b134bd0](https://github.com/thebentobot/dotBento/commit/b134bd0578392633a88578c8e432e11b61443f37))
* **LastFmTimePeriodUtilities:** Add LastFmTimeSpanFromUserOptionTextCommand ([8f6dfc3](https://github.com/thebentobot/dotBento/commit/8f6dfc32e0df367e764fe3c471f60e9369ae771a))
* **legal:** add tos and privacy policy ([3dcb3ad](https://github.com/thebentobot/dotBento/commit/3dcb3adff705e903d9c8396dd854cd3f1d3b7138))
* open source ([0c4a9e8](https://github.com/thebentobot/dotBento/commit/0c4a9e874f792a4d928d38a4d3cc6a3a6e8edc0d))
* **OpenWeatherApi:** add nullable error message to object from API ([dd8c29f](https://github.com/thebentobot/dotBento/commit/dd8c29f9cff58ddcc988d8164331c63e5d54d959))
* **startup:** add async runmode to interactions by default ([77d1ec5](https://github.com/thebentobot/dotBento/commit/77d1ec563093af1683588cb516d291b1e4f1b8e9))
* **startup:** add infrastructure and urbanService to startup ([b84432d](https://github.com/thebentobot/dotBento/commit/b84432defdd952f00dd4cca6eb215dffdb92d4d0))
* **startup:** add shared commands support ([ffb22e8](https://github.com/thebentobot/dotBento/commit/ffb22e891a42cb31448c0096595054b4568448fd))
* **stringExtensions:** add CapitalizeFirstLetter method ([ff543a6](https://github.com/thebentobot/dotBento/commit/ff543a6f5107f01767a216ddda26447ff372f4dd))
* **StringExtensions:** add TrimToMaxLength ([971e630](https://github.com/thebentobot/dotBento/commit/971e6301da288a936aa2215ae6e1e11a1a3225a1))
* **Styling:** add base bento yellow ([e4d9ae4](https://github.com/thebentobot/dotBento/commit/e4d9ae4282ca71879eaf80788893b60e68692f10))
* **UserExtensions:** add check if bot and add to user input cmds ([553bf7a](https://github.com/thebentobot/dotBento/commit/553bf7a7da429fdc49064ab9925ab8007c9dc50c))


### Bug Fixes

* **AvatarSlashCommand:** fix names in command ([880576b](https://github.com/thebentobot/dotBento/commit/880576be9f4875d12803eb54dcdd1a02c6b88a5d))
* **AvatarTextCommand:** fix names in command ([afc5ba6](https://github.com/thebentobot/dotBento/commit/afc5ba67d2739d293a082b6adb75618ace48db94))
* **Bot.Extensions:** warnings ([0564a99](https://github.com/thebentobot/dotBento/commit/0564a998d1e6a86eabff32f1b66f6bab1e56f4a2))
* **Bot.Handlers:** warnings ([6e33369](https://github.com/thebentobot/dotBento/commit/6e333694f8dc260c2d492f5ee83aaf2423e5e0dd))
* **Bot.Startup:** warnings ([0df78ad](https://github.com/thebentobot/dotBento/commit/0df78ad462ebde7c819edd1ed9a041c2c0c37c97))
* **botservice:** unused import ([ed999ff](https://github.com/thebentobot/dotBento/commit/ed999ffe51f7d5cfded5324e4ffbe61f23d54844))
* **cmd:** add current datetime to bento sender, fix format ([bbee593](https://github.com/thebentobot/dotBento/commit/bbee59348c3b2a05ded7f303c48c481b5e7edee9))
* **cmd:** error handling and help cmd for slash and text commands ([6937efd](https://github.com/thebentobot/dotBento/commit/6937efddcc39e43289d8db8e3ebd95c0e5a77b17))
* **cmd:** move commands to sharedCommands and Commands folder ([c2a1b8e](https://github.com/thebentobot/dotBento/commit/c2a1b8e4aaebae322c26efd6a06eddfda09996d1))
* **cmd:** remove unused imports ([c34bb3a](https://github.com/thebentobot/dotBento/commit/c34bb3aec14ce681dab28515b77ebc55a43463e1))
* **docker-prerelease:** add password ([10fb799](https://github.com/thebentobot/dotBento/commit/10fb79995c933115ff219fe0b92e5cfdf48f17da))
* **docker:** add new project ([dbe2567](https://github.com/thebentobot/dotBento/commit/dbe2567f4c487102b2588b29cd6b18fb16c0fc4d))
* **husky:** accomodate husky major version ([94dcdd9](https://github.com/thebentobot/dotBento/commit/94dcdd9ee4756bae1ead1d2dc6a598a3416b9d5b))
* **Infrastructure.Services.PrefixService:** warnings ([e865172](https://github.com/thebentobot/dotBento/commit/e865172ad9baac561c1f7d57dbdbbe76bbacb1a5))
* **InteractionHandler:** catch cmd error and return to user ([476e658](https://github.com/thebentobot/dotBento/commit/476e658ede5a188ca16ee9efacfdc265c8a0e580))
* **LastFmTextCommand:** change namespace structure ([a857fb5](https://github.com/thebentobot/dotBento/commit/a857fb52820431e4d42c36f5d37e02ff6cb7c1b4))
* **MessageHandler:** catch cmd error and return to user ([ceec492](https://github.com/thebentobot/dotBento/commit/ceec49253b691986c3e037f608b568be8d0139cd))
* **ping:** add hide option ([f2ba012](https://github.com/thebentobot/dotBento/commit/f2ba012588b13c0a1ce00e9a58d2badc20bb8234))
* **pingSlashCommand:** correct namespace ([d16ccde](https://github.com/thebentobot/dotBento/commit/d16ccde342f41cd11e72b7e291612c0dc4ea82ee))
* **ResponseModel:** change init to set to support flow for pagination cmds ([ed0d308](https://github.com/thebentobot/dotBento/commit/ed0d308d1cf4ab1cfc370f5daa1a6ed104eebbf7))
* **startup:** load env for local dev ([484ff1e](https://github.com/thebentobot/dotBento/commit/484ff1efe7c0ee264d6955f81171d8f746ff3f86))
* **StringExtensions:** add this to static functions ([f82d7d9](https://github.com/thebentobot/dotBento/commit/f82d7d9e39d2a4672edf06cd0a14bcf72ba617ec))
* **StylingUtilities:** remove unused function ([309658f](https://github.com/thebentobot/dotBento/commit/309658fe3ba43dd75a5316569a4557ed7fe011fa))
* **StylingUtilities:** remove unused httpclient ([c1a6220](https://github.com/thebentobot/dotBento/commit/c1a622069e071a39a47a709da049014284432cec))
* **urbanDictionary:** move urban dictionary service to infra ([665244a](https://github.com/thebentobot/dotBento/commit/665244aecf6ee5b0918b1a505255f2d6724eb577))
* **UserSlashCommand:** remove extra m in description ([e02beac](https://github.com/thebentobot/dotBento/commit/e02beac7f92345aa81c7e59ae4c5607df8002356))
* **weatherService:** use Ordefault method to support Maybe ([c4ac520](https://github.com/thebentobot/dotBento/commit/c4ac520e87ec2f4b0f0acb61124b19552261fa35))

## 1.0.0 (2024-01-07)


### Features

* **docker-prerelease:** add auto deploy to VPS ([52d2a2b](https://github.com/thebentobot/dotBento/commit/52d2a2b542827cca0dc9c76ff1b1618886722097))


### Bug Fixes

* **messageHandler:** change order of user check ([64ace2c](https://github.com/thebentobot/dotBento/commit/64ace2c1beb012f0124949edb47ec9c5e78f01b6))

## [0.1.0](https://github.com/thebentobot/dotBento/compare/v0.0.1...v0.1.0) (2023-11-28)


### Features

* add git checkout ([c1bd87b](https://github.com/thebentobot/dotBento/commit/c1bd87bc8c9b726215d63a0516c931b0c28a78eb))
* docker-release github action ([15d4e56](https://github.com/thebentobot/dotBento/commit/15d4e5658d03c10cccb733f474b5cd70817aec86))
* manual trigger prod release ([a34cfe6](https://github.com/thebentobot/dotBento/commit/a34cfe60c31da1f7ae6b1745cf2b5afe3b45aaa3))
* **tooling:** add vscode debugging ([b37adb8](https://github.com/thebentobot/dotBento/commit/b37adb883fe236b56f53b3ec26d779cf50717a77))


### Bug Fixes

* actually add .idea to gitignore ([0b3b5f3](https://github.com/thebentobot/dotBento/commit/0b3b5f30e48617c896abe29b39fc421bb71aabff))
* add .idea to gitignore ([201a111](https://github.com/thebentobot/dotBento/commit/201a111fb5ce5031e76fa777f258d60dc6193e00))
* add allow unrelated histories to trigger yml ([ca81edf](https://github.com/thebentobot/dotBento/commit/ca81edffd73d7f4fc093991fe912011724fdb127))
* add logging to fix release-please ([be8fba8](https://github.com/thebentobot/dotBento/commit/be8fba8319e099a11c7aa014c574e03dbf0b5da7))
* adjust release according to release-dates ([7900788](https://github.com/thebentobot/dotBento/commit/790078820f4fcb55221bcdb2ffa65b629d00dc8b))
* adjust release-please to accomodate potential release instead of default pre-release ([094d5fe](https://github.com/thebentobot/dotBento/commit/094d5fe622d6f2e3418708e8931d53cab4863c21))
* adjust yaml again ([80a1c02](https://github.com/thebentobot/dotBento/commit/80a1c0230bdd1c1f5dc5e8e0f4cc2311ad599635))
* correct grep in release-please ([ad1c14f](https://github.com/thebentobot/dotBento/commit/ad1c14f7c1bfc0d85631210f484cbb83d517a9d7))
* delete separate trigger ([5ab03f4](https://github.com/thebentobot/dotBento/commit/5ab03f43b3a784f32ce0b41f38ee8989a7733355))
* **devops:** Delete redundant CHANGELOG.MD ([1991bf8](https://github.com/thebentobot/dotBento/commit/1991bf86e35eec1e3d30c2a88e2dc0ae7d138f99))
* diff logging ([66e4405](https://github.com/thebentobot/dotBento/commit/66e4405181387bd09f1c7f7a215ed239eeae3f74))
* docker-prerelease adjusted to pre-release only ([50f2378](https://github.com/thebentobot/dotBento/commit/50f2378163e3d25ee50e18998f82242c437c54ed))
* dotnet build github action ([a05cbe6](https://github.com/thebentobot/dotBento/commit/a05cbe6e13dec6c5a2ec571388e0473a276bb054))
* dotnet yml ([817438e](https://github.com/thebentobot/dotBento/commit/817438e57c2aec356bec24799d51f7700b471b49))
* paths dotnet yml ([66e2205](https://github.com/thebentobot/dotBento/commit/66e2205115860b6a0a3459ab29b768c9d96682bd))
* release please yaml ([1a575a5](https://github.com/thebentobot/dotBento/commit/1a575a5992628408749db8f31f747611bb41f9f5))
* release-please with manual trigger for release ([104d20f](https://github.com/thebentobot/dotBento/commit/104d20f5a970a49dc750ce7a4678e2e3573d97e1))
* remove logging for debug workflow purposes ([a861006](https://github.com/thebentobot/dotBento/commit/a86100633db71056737a4d1048dabb7711149fce))
* remove logging for debugging dotnet.yml ([72e587d](https://github.com/thebentobot/dotBento/commit/72e587d43f98c3b9803dda05a008249f12b708fb))
* restore changelog ([7a94e23](https://github.com/thebentobot/dotBento/commit/7a94e2367637b24cb29f7987f2e86d8ce8cb4b13))
* revert release-please ([5ac8051](https://github.com/thebentobot/dotBento/commit/5ac8051a78dc308b4481d24caa553ba18dcf9247))
* syntax bash in release-please ([e72eeae](https://github.com/thebentobot/dotBento/commit/e72eeae2554fa60944fadbd8b7fcc1b2a7f52f1a))
* Update dotnet.yml ([6207986](https://github.com/thebentobot/dotBento/commit/6207986491757b0d524830df871292e91e78b79a))
* Update trigger-prod-release.yml ([53b9451](https://github.com/thebentobot/dotBento/commit/53b945143e6159e3ee427b60092348238264381f))

## 0.0.1 (2023-11-27)


### Features

* **devops:** add changelog and version file for release please ([57d89fb](https://github.com/thebentobot/dotBento/commit/57d89fb9cc9ae3c3b9c8d6765f06894fa43f2e6f))
* docker workflow and other github stuff ([31bb218](https://github.com/thebentobot/dotBento/commit/31bb2184c90eb7bf24a342880e4e58cfcbd7171c))
* dockerfile ([7fcd948](https://github.com/thebentobot/dotBento/commit/7fcd948b8e00eac756602e2476a59f233b024878))
* move into src ([43ce73f](https://github.com/thebentobot/dotBento/commit/43ce73f0d21d2db3fe4b6698261714bc462d6905))


### Bug Fixes

* change path to find dockerfile ([8e27fe6](https://github.com/thebentobot/dotBento/commit/8e27fe6315ece49a36b73bb9604f405dab1c9318))
* **devops:** Update dotnet.yml ([1971dca](https://github.com/thebentobot/dotBento/commit/1971dca089e24d4bf462250936f109bf97f83019))
* **devops:** Update dotnet.yml ([5036314](https://github.com/thebentobot/dotBento/commit/5036314aa03084d795d236f480fb1cea4c119417))
* **devops:** Update release-please.yaml ([e6a7a48](https://github.com/thebentobot/dotBento/commit/e6a7a4815f2be1f1bc3f6970e46f51544da6f3c1))
* more explicit path ([b18faa5](https://github.com/thebentobot/dotBento/commit/b18faa5249b947fe31bf6bea65a312a16550f045))
* move dockerfile, add path ([78aed07](https://github.com/thebentobot/dotBento/commit/78aed0747481d84024a6bd8c91afbb31b8d5a2db))
* path to dockerfile ([56e1ba4](https://github.com/thebentobot/dotBento/commit/56e1ba411daad1c0c0d5a6798988e3515dc2624d))
* remove context from docker github actions ([be78676](https://github.com/thebentobot/dotBento/commit/be78676c2c2ee055e2bcc94111b09edd8fd4a1d1))
