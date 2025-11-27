AppHost — Short-term plan and handoff notes
============================================

Goal (today)
-------------
- Extracted User service composition into a typed helper and fixed endpoint/reference typing issues so AppHost builds cleanly.
- Created a short plan file for handoff.

What I changed (summary)
------------------------
- Files edited/added in `KunstButikken.AppHost`:
  - Edited: `Composition.cs` — replaced inline user setup with a call to a typed helper, fixed a number of minor typos and braces issues while refactoring.
  - Added: `Composition.UserService.cs` — typed helper `SetupUserServiceHelper(...)` that returns (IResourceBuilder<IResourceWithEnvironment>, IResourceBuilder<ProjectResource>). It casts builders to the correct interfaces so `.WithReference(...)` overloads resolve correctly and throws explicit exceptions when incompatible.
  - Added: `DEVELOPMENT_PLAN.md` (this file) to record the plan forward for the next agent.

Build status (after edits)
--------------------------
- `dotnet build KunstButikken.AppHost.csproj` succeeded (net10.0).
- No compiler errors remain in `KunstButikken.AppHost` as of this commit.

High-level next steps (for tomorrow / next agent)
-------------------------------------------------
We should continue the same incremental extraction and cleanup approach. Work in small, compile-safe chunks and run a build after each extraction.

1) Extract Art service wiring
   - Create `Composition.ArtService.cs` with a typed helper `SetupArtServiceHelper(...)` that mirrors the shape of the user helper: return typed tuple (IResourceBuilder<IResourceWithEnvironment>, IResourceBuilder<ProjectResource>) or appropriate concrete builder types.
   - Cast references to the concrete IResourceBuilder<T> interface the `.WithReference(...)` expects (e.g. `IResourceBuilder<IResourceWithConnectionString>` for DBs, `IResourceBuilder<IResourceWithServiceDiscovery>` for other services) and add clear null-checks.
   - Replace the local `SetupArtService()` in `Composition.cs` with a thin call to the helper.
   - Build and fix any overload/type issues that appear.

2) Extract remaining services one-by-one
   - Follow the same pattern for Auction, Payment, Admin, AuthGateway, Frontends.
   - For AuthGateway and frontends, pay careful attention to service discovery vs endpoint usage (gateway vs web ports) and pass the right builder types.

3) Sweep and replace GetEndpoint(...)! patterns repository-wide
   - Strategy:
     a) Replace occurrences in AppHost first — convert `resource.GetEndpoint(...)!` (nullable-forgiving) to either:
        - `ResourceDefaults.GetEndpointString(resource, "name")` (for environment string values), or
        - `resource.GetEndpoint("name") ?? throw new InvalidOperationException("<meaningful message>")` where an EndpointReference is required.
     b) For other services, search for `GetEndpoint(` and `GetEndpoint(...)!` and apply the same pattern; run `dotnet build` per project.
   - Use `KunstButikken.ServiceDefaults.ResourceBuilderExtensions.GetEndpointString(...)` helper when an environment URL/string is needed.

4) Break `Composition.cs` into multiple smaller files and functions
   - After helper extraction, the `Configure` method should call small, typed helpers only. Keep `AppComposition` record in `Composition.cs` or move to `Composition.Model.cs` if preferred.
   - Keep local functions only when absolutely necessary for overload resolution; prefer typed helpers in partial class files.

5) Add repository-level improvements
   - Add `GlobalUsings.cs` at the repo root to centralize common using directives for net10 projects (Aspire types, shared namespaces), which will reduce `using` clutter in each file.
   - Ensure `Directory.Build.props`/`Directory.Packages.props` are correct and consistent across services (you mentioned repo root is set in Directory.Build.props already).

6) Sweep/resolve warnings
   - Fix Sonar/StyleCop warnings incrementally: braces style, redundant casts, unused assigned values (these will shrink as services are extracted), and method complexity.
   - Aim to reduce "Value assigned is not used" warnings by removing unused local variables or by using them explicitly in the helper that consumes them.

7) Tests and verification
   - After each service extraction, run `dotnet build` for the AppHost solution and for any impacted services.
   - If unit tests exist for services, run `dotnet test <projectOrSolution>`.

Checklist for next agent (concrete)
-----------------------------------
- [ ] Start branch from `sweep/getendpoint` (or continue it).
- [ ] Extract Art service into `Composition.ArtService.cs` and build.
- [ ] Extract Auction, Payment, Admin as typed helpers and build after each change.
- [ ] Extract AuthGateway and Frontends. Ensure endpoint references and gateway ports are correct.
- [ ] Run a repo-wide search for `GetEndpoint(` and `GetEndpoint(...)!` and apply the endpoint/string replacement plan.
- [ ] Add `GlobalUsings.cs` and fix using directives across `AppHost` to follow `.editorconfig`.
- [ ] Run `dotnet build` and fix remaining analyzer warnings.

Useful commands
---------------
- Build AppHost only:

```bash
cd /path/to/KunstButikken.AppHost/KunstButikken.AppHost
dotnet build KunstButikken.AppHost.csproj
```

- Build all service projects from repo root:

```bash
cd /path/to/KunstButikken-asp
dotnet build **/*.csproj
```

- Run a repo-wide search for GetEndpoint occurrences (macOS / bash):

```bash
# show usages
grep -R "GetEndpoint(" -n . | sed -n '1,200p'
```

- Replace `GetEndpoint(...)!` → `GetEndpointString(...)` where appropriate (manual review recommended).

Handoff notes for the next agent
--------------------------------
- Be conservative: extract one service at a time and build. When you see ambiguous `.WithReference(...)` overload failures, cast the referenced builder to the appropriate interface (IResourceBuilder<IResourceWithConnectionString> or IResourceBuilder<IResourceWithServiceDiscovery>, etc.) and add explicit null-checks.
- Prefer `GetEndpointString(...)` for environment variables; prefer typed builder references or `GetEndpoint(...) ?? throw` where the builder reference/EndpointReference is required.
- Keep meaningful exception messages; they make debugging wiring errors simple.

If you want, I can also:
- Start extracting `ArtService` now (create `Composition.ArtService.cs`) and iterate until build passes, or
- Run the repo-wide `GetEndpoint(...)` pattern sweep and prepare an automated patch (careful — needs review).

— End of plan

