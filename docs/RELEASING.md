# Versioning and releasing QuestLog

Versions are not written by hand anywhere. They are calculated from the git
history by [GitVersion][gitversion], and the GitHub release itself is assembled
by [GitReleaseManager][grm]. Both run as pinned .NET local tools.

| File | Purpose |
| --- | --- |
| `.config/dotnet-tools.json` | Pins `gitversion.tool` and `gitreleasemanager.tool` |
| `GitVersion.yml` | How a version is derived from a branch and its history |
| `GitReleaseManager.yaml` | How release notes are assembled from milestones and labels |
| `Directory.Build.props` | Placeholder versions for local builds; CI overrides them |

## Running the tools locally

```bash
dotnet tool restore                       # once per clone
dotnet tool run dotnet-gitversion         # every calculated value, as JSON
dotnet tool run dotnet-gitversion -showvariable SemVer
dotnet tool run dotnet-gitreleasemanager --help
```

GitVersion needs the full history, so a shallow clone will give the wrong
answer — both workflows check out with `fetch-depth: 0` for that reason.

## What each branch produces

Assuming the newest release tag is `0.1.0`:

| Branch | Example version | Increment |
| --- | --- | --- |
| `master` | `0.1.1` | patch |
| `development` | `0.2.0-alpha.3` | minor |
| `release/0.2.0` | `0.2.0-beta.1` | from the branch name |
| `hotfix/*` | `0.1.1-beta.1` | patch |
| `feature/*` | `0.2.0-my-feature.1` | inherited from `development` |
| `fix/*` | `0.1.1-my-fix.1` | patch |
| anything else | `0.1.1-my-branch.1` | patch |

Because the repository has no tags yet, `next-version: 0.1.0` in
`GitVersion.yml` seeds the first release. Once a higher tag exists that setting
is ignored and can be deleted.

### Forcing a different increment

Add one of these to any commit message:

```
+semver: major      (or breaking)
+semver: minor      (or feature)
+semver: patch      (or fix)
+semver: none       (or skip)
```

### Getting a minor release onto master

`master` increments the patch by default, so merging `development` straight
into it produces `0.1.1`, not `0.2.0`. To ship a minor release, either

* cut a `release/0.2.0` branch from `development` and merge **that** into
  `master` — the version in the branch name carries across the merge; or
* put `+semver: minor` in the merge commit message.

## Cutting a release

Run the **Continuous Delivery** workflow from the Actions tab
(`workflow_dispatch`). It will:

1. calculate the version with GitVersion and tag the commit with it,
2. build and publish with that version stamped into the assembly,
3. package the build with Velopack, generating deltas against the last release,
4. create the milestone if it does not exist yet,
5. draft the release notes with GitReleaseManager and publish the release,
6. upload the Velopack artifacts to it and close the milestone.

To release a specific version instead, push the tag yourself
(`git tag 1.0.0 && git push origin 1.0.0`). GitVersion honours the tag, and
every version calculated afterwards continues from it.

## Release notes

Notes come from the GitHub milestone whose title matches the version, so an
issue or pull request only appears in a release once it is assigned to that
milestone. The sections are driven by labels, which mirror `.github/release.yml`:

`breaking`, `feature`, `enhancement`, `bugfix`, `dependencies`

Issues labelled `ignore-for-release` or `release` are left out. To create all of
these labels in the repository at once:

```bash
dotnet tool run dotnet-gitreleasemanager label \
  --token "$GITHUB_TOKEN" -o marcin-przywoski -r QuestLog
```

Note that this command **deletes existing labels** before recreating the set
defined in `GitReleaseManager.yaml`.

[gitversion]: https://gitversion.net/docs/
[grm]: https://gittools.github.io/GitReleaseManager/docs/
