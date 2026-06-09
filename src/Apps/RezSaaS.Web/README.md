# RezSaaS Web

Frontend application boundary for RezSaaS. The app starts in-repo under
`src/Apps/RezSaaS.Web` and currently contains the first authenticated business
panel slice.

## Current Routes

- `/` landing handoff into the panel
- `/giris` cookie login
- `/kayit` account registration
- `/sifremi-unuttum` forgot password
- `/sifre-sifirla` reset password
- `/panel` authenticated business operations panel

The panel combines the RezSaaS-domain reference (`rezsaas-merkez`) with the
modern studio visual reference (`viktor-oddy-studio`):

- verified tenant context card
- live appointment request inbox from `GET /api/business/appointment-requests`
- typed OpenAPI response contracts generated from the backend artifact
- masked customer PII
- PendingApproval / Approved / Declined / Expired / Superseded states
- conflict decision dialog
- explicit branch timezone display
- internal resource labels only on business surface
- session/bootstrap guard
- honest backend-unavailable state for session, context, and inbox

`/panel` requires `GET /api/session/bootstrap`, uses `GET /api/business/context`
for the verified tenant context, and forwards that context through the central
API client with `X-RezSaaS-Tenant`. Preview appointment data is not used.

## Scripts

```powershell
pnpm install
pnpm dev
pnpm typecheck
pnpm lint
pnpm build
```

Generate the API types after the backend OpenAPI artifact changes:

```powershell
pnpm generate:api
```

## Backend Integration

Browser requests use same-origin `/api/*` calls and Next rewrites them to the
backend. Server components call the backend directly and forward the incoming
cookie header.

Default local backend:

```text
REZSAAS_API_BASE_URL=http://localhost:5252
```

The generated file is derived from
`../../../artifacts/openapi/rezsaas-api-v1.json` and must not be edited
manually.
