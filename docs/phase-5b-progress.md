# Phase 5b Implementation Progress

## Status: IN PROGRESS (Foundation Laid)

This document tracks the implementation progress of Phase 5b (Analytics Module).

## Completed ✓

### 1. Module Structure
- ✅ Created `RezSaaS.Modules.Analytics.csproj` with EF Core dependencies
- ✅ Created `AnalyticsModule.cs` implementing `IModule` interface
- ✅ Module follows modular monolith architecture (AGENTS.md §3.2)

### 2. Domain Read Models
- ✅ **DailyBusinessMetrics**:
  - Request metrics (total, approved, declined, expired)
  - Appointment metrics (total, completed, cancelled, no-show)
  - Capacity metrics (slots, occupancy rate, utilization rate)
  - Conversion metrics (request-to-approval rate)
  - No-show rate calculation
  - Tenant + branch scope support
  - Daily aggregation granularity

- ✅ **TopServiceMetrics**:
  - Service/variant identification
  - Booking counts by status
  - Revenue tracking (for future payment integration)
  - Average service duration
  - Ranking support (for top N lists)
  - Period-based aggregation

- ✅ **ResourceCapacityMetrics**:
  - Resource identification (name, type)
  - Slot-based capacity utilization
  - Time-based utilization (minutes)
  - Staff assignment tracking
  - Period-based aggregation

### 3. Persistence Layer
- ✅ **AnalyticsDbContext**:
  - Separate DbContext following module isolation
  - EF Core entity configurations with proper constraints
  - Precision types for decimal fields (rates, percentages, money)
  - Comprehensive indexes for performance:
    - DailyMetrics: Unique index on tenant+branch+date
    - TopServiceMetrics: Index on tenant+branch+period, ranking
    - ResourceCapacity: Index on tenant+branch+period, resource
  - Proper length constraints on string fields
  - Ready for migration generation

## Pending Implementation ⏳

### 1. Application Services
- ❌ **AnalyticsQueryService**:
  - Query daily business metrics by date range
  - Query top N services by booking count or revenue
  - Query resource capacity metrics by period
  - Calculate trends (day-over-day, week-over-week)
  - Aggregate metrics across branches (tenant-wide view)
  - Apply tenant filtering (mandatory, per AGENTS.md §4.2)

- ❌ **MetricsCalculationService**:
  - Calculate occupancy rate from bookings and capacity
  - Calculate no-show rate from appointments
  - Calculate conversion rate from requests to approvals
  - Calculate utilization rates
  - Determine top services by bookings/revenue
  - Calculate resource capacity utilization

- ❌ **MetricsProjectionService** (Background Job):
  - Project from Booking/AppointmentRequest entities to analytics read models
  - Cannot directly access write-side tables (AGENTS.md §3.2)
  - Strategy options:
    - Event-based projection via domain events
    - Scheduled batch projection with controlled access
  - Incremental updates vs full recalculation
  - Idempotency for retries

### 2. API Endpoints
- ❌ **Business Analytics Endpoints** (Tenant-scoped):
  - `GET /api/business/analytics/daily-metrics` - Daily metrics by date range
    - Auth: `BusinessOwner` (tenant-wide) or `BranchManager` (branch-scoped)
    - Query params: startDate, endDate, branchId (optional)
  - `GET /api/business/analytics/top-services` - Top N services
    - Auth: `BusinessOwner` or `BranchManager`
    - Query params: limit, periodStart, periodEnd, branchId (optional), sortBy (bookings/revenue)
  - `GET /api/business/analytics/resource-capacity` - Resource utilization
    - Auth: `BusinessOwner` or `BranchManager`
    - Query params: periodStart, periodEnd, branchId (optional), resourceId (optional)
  - `GET /api/business/analytics/trends` - Trend analysis
    - Auth: `BusinessOwner` or `BranchManager`
    - Query params: metric, period, comparisonType

- ❌ **Platform Analytics Endpoints** (Platform-admin only):
  - `GET /api/platform/analytics/overview` - Platform-wide metrics
    - Auth: `PlatformAdminWithStepUp`
    - Aggregates across all tenants (no tenant filter, approved by ADR)
  - `GET /api/platform/analytics/top-tenants` - Top performing tenants
    - Auth: `PlatformAdminWithStepUp`
  - `GET /api/platform/analytics/growth-metrics` - Platform growth trends
    - Auth: `PlatformAdminWithStepUp`

### 3. Repositories
- ❌ **IDailyBusinessMetricsRepository**:
  - Get by tenant, branch, and date range
  - Upsert (create or update) for idempotent projection
  - Delete old metrics for retention policy

- ❌ **ITopServiceMetricsRepository**:
  - Get by tenant, branch, and period
  - Get top N by ranking
  - Upsert for idempotent projection

- ❌ **IResourceCapacityMetricsRepository**:
  - Get by tenant, branch, and period
  - Get by resource
  - Upsert for idempotent projection

### 4. Infrastructure
- ❌ **DbContext Registration**:
  - Register AnalyticsDbContext in AnalyticsModule
  - Configure connection string (shared DB, analytics schema)
  - Set up separate schema if needed

- ❌ **Migration**:
  - Generate initial migration
  - Create analytics tables in separate schema (recommended: `analytics`)
  - Add indexes and constraints

- ❌ **Background Job Registration**:
  - Register metrics projection worker
  - Configure schedule (hourly/daily)
  - Configure retention policy (old metrics cleanup)

### 5. Frontend Implementation
- ❌ **Business Analytics Dashboard** (`/panel/analiz`):
  - Daily metrics cards (occupancy, no-show, conversion)
  - Time series charts (bookings over time)
  - Comparison with previous period
  - Branch selector (for multi-branch businesses)
  - Date range picker (UTC dates)

- ❌ **Top Services View** (`/panel/analiz/hizmetler`):
  - Top services table/list
  - Sortable by bookings, revenue, no-show rate
  - Service details drill-down
  - Performance trend indicators

- ❌ **Resource Capacity View** (`/panel/analiz/kaynaklar`):
  - Resource utilization cards
  - Capacity utilization charts
  - Staff assignment breakdown
  - Time-based utilization visualization

- ❌ **Platform Analytics Dashboard** (`/platform/analiz`):
  - Platform-wide metrics (step-up authz required)
  - Tenant ranking table
  - Growth trend charts
  - Only accessible to `PlatformAdminWithStepUp`

### 6. Configuration
- ❌ **AnalyticsOptions**:
  - Projection schedule (cron or interval)
  - Retention period (days/months to keep metrics)
  - Maximum top N services default
  - Background job enabled flag

- ❌ **appsettings.json**:
  - Analytics section with configuration
  - Connection string configuration

### 7. Documentation
- ❌ **ADR for Read Model Projection Strategy**:
  - Decision on event-based vs batch projection
  - Cross-module data access rules
  - Idempotency and retry strategy
  - Performance considerations

- ❌ **Data Retention Policy**:
  - Update `docs/11-veri-envanteri-taslagi.md`
  - Analytics data retention period
  - PII considerations (minimal PII in analytics)

- ❌ **API Documentation**:
  - Update OpenAPI docs with analytics endpoints
  - Authz requirements documented
  - Query parameter specifications

## Security Requirements (To Verify)

- ❌ All analytics queries are tenant-filtered by default
- ❌ Platform analytics endpoints require `PlatformAdminWithStepUp`
- ❌ Business analytics endpoints require `BusinessOwner` or `BranchManager`
- ❌ Read models do NOT contain PII (no customer names, emails, phones)
- ❌ Query filter bypass is prevented via architecture tests
- ❌ Platform analytics aggregate data only (no raw `UserAccountId`)
- ❌ Rate limiting on heavy aggregation queries

## Dependencies

- Phase 3 (booking operations) ✅ - required for meaningful data
- Phase 5a (business settings) ✅ - provides branch/service data
- Phase 4a/4b (payments) ⏳ - optional for revenue metrics
- Background job framework ⏳ - needs integration with existing job system

## Acceptance Criteria Status

| Criterion | Status |
|-----------|--------|
| Analytics module passes architecture tests | ⏳ (IModule implemented, tests pending) |
| Business metrics scoped to tenant membership | ⏳ (read models support scope, queries pending) |
| Platform metrics only accessible with step-up admin | ⏳ (endpoints pending) |
| Read models don't access write-side tables directly | ⏳ (projection strategy pending ADR) |
| Metric calculations are deterministic and testable | ⏳ (calculation services pending) |
| Time range and timezone rules consistent | ✅ (all dates UTC in read models) |
| No PII in analytics read models | ✅ (read models contain aggregated data only) |
| No mock/fake metrics (real data only) | ⏳ (projection needed) |

## Architecture Compliance

### Modular Monolith Rules (AGENTS.md §3.2, §11.2)
- ✅ Separate module project
- ✅ Only references BuildingBlocks
- ✅ Separate DbContext for analytics
- ⏳ No direct assembly reference to write modules (Booking, Catalog)
- ⏳ Cross-module communication via explicit contract/event (pending ADR)

### Tenant Isolation (AGENTS.md §4.2)
- ✅ All read models have `TenantId` field
- ✅ Unique indexes include `TenantId`
- ⏳ Global query filter on DbContext (pending)
- ⏳ All queries enforce tenant filter (pending implementation)

### Data Security (AGENTS.md §6.2)
- ✅ No PII in read model design
- ✅ Aggregated data only (counts, rates, percentages)
- ⏳ Audit logging for analytics queries (pending)

## Next Steps

1. Create ADR for read model projection strategy
2. Implement repositories with tenant filtering
3. Implement metrics calculation services
4. Create analytics query service
5. Register DbContext and configure schema
6. Generate and apply migration
7. Implement business analytics API endpoints
8. Implement platform analytics API endpoints (with step-up authz)
9. Implement projection background job
10. Create frontend analytics dashboard
11. Add integration tests
12. Update data inventory documentation
13. Update OpenAPI documentation

## Blockers & Open Questions

1. **Projection Strategy**: Event-based vs batch projection?
   - Event-based: Real-time, more complex, requires domain event integration
   - Batch: Simpler, eventual consistency, scheduled background job
   - Decision needed: Which approach for Phase 5b?

2. **Cross-Module Access**: How to read Booking/AppointmentRequest data for projection?
   - Option 1: Domain events published by write modules
   - Option 2: Explicit read-only contracts with write modules
   - Option 3: Controlled direct access with ADR approval
   - Must comply with AGENTS.md §3.2 (no direct write-side table access)

3. **Retention Policy**: How long to keep daily metrics?
   - Raw daily metrics: 90 days? 1 year?
   - Aggregated monthly metrics: longer?
   - Update required in `docs/11-veri-envanteri-taslagi.md`

4. **Background Job Framework**: Which framework to use?
   - Hangfire (if already used)
   - Quartz.NET
   - Custom hosted service
   - Decision needed for projection job scheduling