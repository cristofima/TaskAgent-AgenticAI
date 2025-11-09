# Aspire Version Management

This solution uses centralized version management for .NET Aspire components.

## How to Update Aspire Version

To update the Aspire version across the entire solution, you need to update **TWO files**:

### 1. `global.json` (SDK Version)

Controls the MSBuild SDK version for AppHost project.

```json
{
  "msbuild-sdks": {
    "Aspire.AppHost.Sdk": "9.5.0" // ← Update this version
  }
}
```

### 2. `Directory.Build.props` (Property Variable)

Defines the `$(AspireVersion)` property used by NuGet packages.

```xml
<PropertyGroup>
  <AspireVersion>9.5.0</AspireVersion>  <!-- ← Update this version -->
</PropertyGroup>
```

### What Gets Updated Automatically

Once you update the two files above, these packages will automatically use the new version:

- `Aspire.Hosting.AppHost` (via `$(AspireVersion)`)
- `Microsoft.Extensions.ServiceDiscovery` (via `$(AspireVersion)`)

### Example: Upgrading from 9.5.0 to 9.6.0

1. Update `global.json`:

   ```json
   "Aspire.AppHost.Sdk": "9.6.0"
   ```

2. Update `Directory.Build.props`:

   ```xml
   <AspireVersion>9.6.0</AspireVersion>
   ```

3. Restore and build:
   ```bash
   dotnet restore
   dotnet build
   ```

## Why Two Files?

- **`global.json`**: Controls MSBuild SDK tooling (build-time)
- **`Directory.Build.props`**: Controls NuGet package versions (runtime)

Both must be synchronized to ensure compatibility.
