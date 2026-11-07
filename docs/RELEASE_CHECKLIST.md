# Release checklist

Merging to `main` publishes a release when the version is new, so go through this first.

## Before merging
- [ ] Set the new version in `src/GameModeManager/Plugin.cs` (`ModuleVersion`). The NuGet package uses the same number.
- [ ] The pull request's checks pass, including the bundled plugin and Kus config checks.
- [ ] `scripts\Check-Visuals.ps1` in the server repo prints nothing. It covers the addon's build and upload, map pictures, screenshots and the README's versions.
- [ ] If menu files changed, you've seen the uploaded addon on a PC that isn't using your local build.
- [ ] Tested on the test server with `.\test-build.ps1`: `css_pmm_gg1`, `!game`, `!modes`, `!rtv`, `!nominate`, `!menu`, a match ending, and a workshop map change.
- [ ] If `docs/wiki` changed, it reads correctly. It replaces the matching pages on the wiki.
- [ ] The pull request description is ready, because it becomes the release notes.

## After merging
- [ ] The release and its zip are on GitHub.
- [ ] Paste the Discord post from the Actions run's summary into the channel.
- [ ] The new version of `GameModeManager.Shared` is on NuGet and in the repo's Packages list.
- [ ] Install the release in the server repo's `bisect` branch and deploy it.
- [ ] Close the issues it fixes, and tell Kus if it affects his repo.
