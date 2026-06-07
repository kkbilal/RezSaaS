# RezSaaS Web

Frontend application boundary for RezSaaS. The initial committed surface only contains the OpenAPI-driven API client gate; UI routes are intentionally not scaffolded before the backend/frontend contracts are verified.

Generate the API types after installing the web dependencies:

```powershell
pnpm install
pnpm generate:api
```

If global `pnpm`/`npx` is unavailable, run the repository script with `-NodePath`
after installing dependencies.

The generated file is derived from `../../../artifacts/openapi/rezsaas-api-v1.json` and must not be edited manually.
