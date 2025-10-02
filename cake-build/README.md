# Cake Build System for ably-dotnet

This directory contains the Cake build scripts for the ably-dotnet SDK.

## Why Cake Build?

Migrated from FAKE (F#) to Cake (C#) to:
- Make build system accessible to all C# developers
- Improve maintainability
- Better IDE support and debugging
- Larger community and better documentation

## Structure

```
cake-build/
├── build.cake           # Main entry point
├── tasks/
│   ├── build.cake       # Build tasks (NetFramework, NetStandard, Xamarin)
│   ├── test.cake        # Test tasks (Unit, Integration, with retry)
│   └── package.cake     # Package tasks (NuGet creation, Unity)
├── helpers/
│   ├── paths.cake       # Path definitions and constants
│   ├── tools.cake       # Tool helpers (ILRepack, etc.)
│   ├── test-retry.cake  # Test retry logic for flaky tests
│   ├── build-config.cake # Build configuration helpers
│   └── frameworks.cake  # Framework detection helpers
└── README.md            # This file
```

## Usage

### Basic Commands

**Windows (PowerShell/CMD):**
```powershell
# Build
.\build-cake.cmd --target=Build.NetStandard
.\build-cake.cmd --target=Build.NetFramework

# Test
.\build-cake.cmd --target=Test.NetStandard.Unit
.\build-cake.cmd --target=Test.NetStandard.Unit --framework=net6.0

# Package
.\package-cake.cmd 1.2.3
.\package-push-cake.cmd 1.2.3
```

**Linux/macOS (Bash):**
```bash
# Build
./build-cake.sh --target=Build.NetStandard
./build-cake.sh --target=Build.Xamarin

# Test
./build-cake.sh --target=Test.NetStandard.Unit
./build-cake.sh --target=Test.NetStandard.Unit --framework=net6.0

# Package
./package-cake.sh 1.2.3
./package-push-cake.sh 1.2.3  # Run on macOS for iOS packages
```

### Available Build Targets

**Preparation Targets:**
- `Prepare` - Clean build directories and restore NuGet packages (runs automatically for build/test/package targets)

**Build Targets:**
- `Build.NetFramework` - Build .NET Framework projects (Windows only)
- `Build.NetStandard` - Build .NET Standard projects
- `Build.Xamarin` - Build Xamarin projects

**Test Targets:**
- `Test.NetFramework.Unit` - Run .NET Framework unit tests
- `Test.NetFramework.Unit.WithRetry` - Run with automatic retry for failed tests
- `Test.NetFramework.Integration` - Run .NET Framework integration tests
- `Test.NetFramework.Integration.WithRetry` - Run with retry
- `Test.NetStandard.Unit` - Run .NET Standard unit tests
- `Test.NetStandard.Unit.WithRetry` - Run with retry
- `Test.NetStandard.Integration` - Run .NET Standard integration tests
- `Test.NetStandard.Integration.WithRetry` - Run with retry

**Package Targets:**
- `Package` - Create core NuGet package (ably.io)
- `Package.WithUnity` - Create core package + Unity package
- `PushPackage` - Create push notification packages (Android & iOS)

### Complete Target List

To see all available targets (including internal helper tasks), run:
```bash
# On Windows
.\build-cake.cmd --description

# On Linux/macOS
./build-cake.sh --description
```

**Public Targets (15):** These are the main targets you should use:
- `Prepare` - Clean and restore (runs automatically as dependency)
- `Build.NetFramework`, `Build.NetStandard`, `Build.Xamarin`
- `Test.NetFramework.Unit`, `Test.NetFramework.Unit.WithRetry`
- `Test.NetFramework.Integration`, `Test.NetFramework.Integration.WithRetry`
- `Test.NetStandard.Unit`, `Test.NetStandard.Unit.WithRetry`
- `Test.NetStandard.Integration`, `Test.NetStandard.Integration.WithRetry`
- `Package`, `Package.WithUnity`, `PushPackage`

**Internal Helper Tasks (22):** These run automatically as dependencies:
- `Clean`, `Restore`, `Version`
- `NetFramework-Build`, `NetStandard-Build`, `Xamarin-Build`, `Restore-Xamarin`
- `NetFramework-Unit-Tests`, `NetFramework-Unit-Tests-WithRetry`
- `NetFramework-Integration-Tests`, `NetFramework-Integration-Tests-WithRetry`
- `NetStandard-Unit-Tests`, `NetStandard-Unit-Tests-WithRetry`
- `NetStandard-Integration-Tests`, `NetStandard-Integration-Tests-WithRetry`
- `Package-Build-All`, `Package-Merge-JsonNet`, `Package-Create-NuGet`, `Package-Unity`
- `PushPackage-Build-All`, `PushPackage-Create-NuGet`

### Advanced Options

```bash
# Build with specific configuration
.\build-cake.cmd --target=Build.NetStandard --configuration=Debug

# Test specific framework
.\build-cake.cmd --target=Test.NetStandard.Unit --framework=net6.0

# Build with custom constants
.\build-cake.cmd --target=Build.NetStandard --define=MY_CONSTANT

# Verbose output
.\build-cake.cmd --target=Build.NetStandard --verbosity=diagnostic

# List all targets
.\build-cake.cmd --description

# Show task dependency tree
.\build-cake.cmd --tree
```

**Note:** On Linux/macOS, use `./build-cake.sh` instead of `.\build-cake.cmd`

## Prerequisites

### Required Tools
- .NET SDK 6.0+ (for building)
- Cake.Tool (installed via `dotnet tool restore`)
- NuGet CLI (for package creation)

### Platform-Specific Requirements

**Windows:**
- Visual Studio 2022 or Build Tools
- Windows SDK (for .NET Framework builds)

**macOS:**
- Xcode (for iOS builds)
- Xamarin workload

**Linux:**
- .NET SDK only (limited to .NET Standard builds)

## Adding New Tasks

1. Determine which file the task belongs in:
   - `tasks/build.cake` - Build-related tasks
   - `tasks/test.cake` - Test-related tasks
   - `tasks/package.cake` - Package-related tasks

2. Add the task with appropriate dependencies:
   ```csharp
   Task("MyNewTask")
       .IsDependentOn("SomeOtherTask")
       .Does(() =>
   {
       Information("Doing something...");
       // Your code here
   });
   ```

3. Create a public target if needed:
   ```csharp
   Task("My.Public.Target")
       .IsDependentOn("MyNewTask");
   ```

4. Test locally before committing
5. Update this documentation

## Debugging

### View Task Dependencies

Shows the complete dependency tree for all targets:

```bash
# On Windows
.\build-cake.cmd --tree

# On Linux/macOS
./build-cake.sh --tree
```

### Dry Run (Test Without Execution)

The `--dryrun` flag shows which tasks would be executed and in what order, **without actually running them**. This is useful for:
- Verifying task configuration
- Understanding execution order
- Testing changes safely before actual execution

```bash
# On Windows
.\build-cake.cmd --target=Build.NetStandard --dryrun

# On Linux/macOS
./build-cake.sh --target=Build.NetStandard --dryrun
```

**Example output:**
```
Performing dry run...
Target is: Build.NetStandard
1. Clean
2. Restore
3. Prepare
4. NetStandard-Build
5. Build.NetStandard

This was a dry run.
No tasks were actually executed.
```

### Verbose Logging

Get detailed diagnostic information during execution:

```bash
# On Windows
.\build-cake.cmd --target=Build.NetStandard --verbosity=diagnostic

# On Linux/macOS
./build-cake.sh --target=Build.NetStandard --verbosity=diagnostic
```

**Verbosity levels:** `Quiet`, `Minimal`, `Normal`, `Verbose`, `Diagnostic`

### Common Issues

**Issue:** `build/` directory conflicts  
**Solution:** We use `cake-build/` for scripts and `build-output/` for artifacts to avoid conflicts with .NET's reserved `build/` directory

**Issue:** Tool not found  
**Solution:** Run `dotnet tool restore`

**Issue:** Permission denied on scripts  
**Solution:** Run `chmod +x build-cake.sh package-cake.sh package-push-cake.sh`

**Issue:** Paket restore fails  
**Solution:** Ensure Paket is installed: `dotnet tool restore`

**Issue:** ILRepack fails  
**Solution:** Ensure IO.Ably.snk key file exists in the root directory

## Migration Notes

This build system replaces the previous FAKE (F#) build system. All commands remain functionally identical.

### Command Mapping

| FAKE Command | Cake Command | Notes |
|--------------|--------------|-------|
| `./build.sh Build.NetStandard` | `./build-cake.sh --target=Build.NetStandard` | Identical functionality |
| `./build.cmd Build.NetFramework` | `./build-cake.cmd --target=Build.NetFramework` | Windows only |
| `./build.sh Test.NetStandard.Unit -f net6.0` | `./build-cake.sh --target=Test.NetStandard.Unit --framework=net6.0` | Framework parameter |
| `./package.sh 1.2.3` | `./package-cake.sh 1.2.3` | Version parameter |
| `./package-push.sh 1.2.3` | `./package-push-cake.sh 1.2.3` | Version parameter |

### Key Differences from FAKE

1. **Language:** C# instead of F# (more accessible to team)
2. **IDE Support:** Full IntelliSense and debugging in VS/VSCode
3. **Directory Structure:** `cake-build/` instead of `build-script/`
4. **Output Directory:** `build-output/` instead of `build/` (avoids .NET conflicts)
5. **Addins:** Uses NuGet packages instead of Paket for build dependencies

## Performance

Build performance is comparable to FAKE:
- **NetStandard Build:** ~30-45 seconds
- **NetFramework Build:** ~45-60 seconds (Windows)
- **Unit Tests:** ~2-5 minutes
- **Integration Tests:** ~5-10 minutes
- **Package Creation:** ~2-3 minutes

## CI/CD Integration

The Cake build system integrates with GitHub Actions. See:
- `.github/workflows/build-cake.yml` - Build and test workflow
- `.github/workflows/package-cake.yml` - Package creation workflow

## Support

For issues or questions:
1. Check this README
2. Review the migration plan: `build-migration-plan.md`
3. Check Cake documentation: https://cakebuild.net/docs/
4. Ask in team chat or create a GitHub issue

## Resources

- [Cake Build Official Docs](https://cakebuild.net/docs/)
- [Cake Build API Reference](https://cakebuild.net/api/)
- [Migration Plan](../build-migration-plan.md)
- [Migration Review](../build-migration-review.md)

---

*Last Updated: 2025-10-02*
*Cake Version: 4.0.0*
*Migrated from: FAKE 5.23.1*
