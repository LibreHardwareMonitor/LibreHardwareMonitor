# Releasing LibreHardwareMonitorLib

This document describes a recommended, repeatable release process for producing library builds (`.nupkg`) and publishing release artifacts.

Prerequisites
- Windows runner or machine with Visual Studio Build Tools (msbuild) or `dotnet` SDK available.
- Access to the repository and permissions to create Git tags and GitHub releases.
- (Optional) NuGet API key for publishing to nuget.org.

Typical release steps (manual)

1. Update version

   - Update the package version in the library project (`LibreHardwareMonitorLib.csproj`) or `.nuspec` as appropriate.

2. Commit and tag

```powershell
git add -A
git commit -m "Bump version to vX.Y.Z"
git tag -a vX.Y.Z -m "Release vX.Y.Z"
git push origin master --follow-tags
```

3. Let CI build and produce artifacts

- The CI workflow (`.github/workflows/ci.yml`) builds on Windows and uploads the `artifacts` directory as a workflow artifact. Confirm the workflow run completes successfully.

4. Create GitHub Release (from tag)

- On GitHub, create a Release from the tag `vX.Y.Z` (or use the web UI). Attach the built `.nupkg` and any other assets from the workflow run, or use the `Upload release asset` action to automate this from CI.

5. Publish to NuGet (optional)

- From a local machine with `dotnet` and your NuGet API key configured in `~/.nuget/NuGet.Config` or via environment variable, run:

```powershell
dotnet nuget push .\artifacts\Release\LibreHardwareMonitorLib.*.nupkg -k $env:NUGET_API_KEY -s https://api.nuget.org/v3/index.json
```

Automating publish in CI

- You can extend the CI workflow to run `dotnet pack` and `dotnet nuget push` only on `push` events for annotated tags or `release` events. Keep API keys as repository secrets (`NUGET_API_KEY`).

Tips and notes
- Do not commit built artifacts (`artifacts/`) to the repository. They are stored as workflow artifacts or GitHub Release assets.
- Keep the library project(s) as the single source of truth for packaging metadata (authors, description, license).
- If the project is not SDK-style, provide a `.nuspec` in `build/` and use `nuget.exe pack` in CI.

If you'd like, I can add a CI job to automatically `dotnet pack` and `dotnet nuget push` on tagged releases (requires a `NUGET_API_KEY` secret). Let me know if you want that now.
