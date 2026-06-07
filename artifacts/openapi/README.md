# RezSaaS OpenAPI Artifacts

This directory is reserved for versioned API contract artifacts that feed the frontend type-safe client.

Generate the current Development contract from the API project:

```powershell
.\scripts\Export-OpenApi.ps1
```

After `src/Apps/RezSaaS.Web` exists, generate TypeScript API types from the artifact:

```powershell
.\scripts\Generate-OpenApiTypes.ps1
```
