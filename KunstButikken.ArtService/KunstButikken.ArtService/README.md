# KunstButikken.ArtService - Azure Blob configuration

This project reads Azure Blob configuration from the application's configuration system.

We intentionally remove `AzureBlob` from `appsettings.Development.json` so that secrets are not committed to source
control. Use one of the approaches below to provide the blob connection string in development.

## Using dotnet user-secrets (recommended for local development)

This project already has a `UserSecretsId` configured in the `.csproj`.

Set the connection string and optional container name like this (from the repo root):

```bash
# Set connection string (replace with your storage account connection string)
# use the --project flag to target the ArtService project
dotnet user-secrets set "AzureBlob:ConnectionString" "<your-azure-blob-connection-string>" --project ./KunstButikken.ArtService/KunstButikken.ArtService.csproj

# Optionally set container name (defaults to 'images' in code if not set)
dotnet user-secrets set "AzureBlob:Container" "images" --project ./KunstButikken.ArtService/KunstButikken.ArtService.csproj

# To list secrets
dotnet user-secrets list --project ./KunstButikken.ArtService/KunstButikken.ArtService.csproj

# To remove a secret
dotnet user-secrets remove "AzureBlob:ConnectionString" --project ./KunstButikken.ArtService/KunstButikken.ArtService.csproj
```

When running the app in the Development environment, these secrets will be loaded into the configuration and will be
available via `configuration["AzureBlob:ConnectionString"]`.

## Using environment variables

You can also set environment variables. For nested configuration keys use double underscores:

```bash
# zsh/bash
export AzureBlob__ConnectionString="<your-azure-blob-connection-string>"
export AzureBlob__Container="images"

# then run the service
dotnet run --project ./KunstButikken.ArtService/KunstButikken.ArtService.csproj
```

## Behavior notes

- If `AzureBlob:ConnectionString` is not set, `BlobStorage` constructor will throw when created. During seeding the
  startup code attempts to resolve `IBlobStorage` and catches any exception, so demo data will still be created without
  images.
- The code uses the existing `IBlobStorage` service to upload demo images during seeding. If you want to seed actual
  images instead of the tiny placeholder, replace the base64 string in `Program.cs` or extend the seeding logic.

## Troubleshooting

- If uploads fail due to permissions, check that the storage account connection string is correct and that the
  configured container allows access or that your account has permission to create containers/blobs.
- To verify seeded data, start the service and request `GET /api/art` (example: `curl http://localhost:5000/api/art`).
