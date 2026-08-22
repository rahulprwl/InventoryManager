# AI-Augmentation Report (AI-USAGE.md)

## 1. AI Strategy & Tooling
* **Primary Tool:** GitHub Copilot & Gemini Code Assistant.
* **Context Management Strategy:**
  * Supplied the assignment requirements, including the .NET 10 / React stack, DDD boundaries, MongoDB atomic inventory updates, Redis caching, RabbitMQ events, Docker Compose, and mandatory tests.
  * Used incremental prompts: first established the Compose infrastructure, then narrowed implementation to Infrastructure interfaces, repository operations, and Web API dependency registration.
  * Inspected the existing workspace and current files before editing. The repository-layer prompt was narrowed to the existing Hold and Item Mongo implementations, then routed through Domain-owned abstractions so Domain code does not depend on Infrastructure.
  * Used targeted file reads, git history, patch-based edits, and Docker Compose image builds. Prompts kept item creation and stock changes behind `IItemService`, with the Web API limited to HTTP translation and Contracts owning request DTOs.
  * Framed the item API around batch creation, offset/limit reads, explicit stock replacement, and database-owned UUID generation. The UUID serialization failure was traced from the Mongo stack trace to the driver configuration rather than patched in the controller.
  * Extended the same DDD approach to holds: Contracts owns status and response DTOs, Domain owns repositories, services, and adapter interfaces, Infrastructure owns MongoDB, Redis, and RabbitMQ calls, and Web API owns controllers and the hosted expiry worker.
  * Introduced a formal **Domain Adapter Layer** (`IRedisLockAdapter`, `IRedisHoldCacheAdapter`, `IRabbitMqHoldDelayAdapter`, `IRabbitMqEventPubAdapter`) to strictly isolate third-party protocol calls and decouple the service layer from concrete Redis/RabbitMQ infrastructure drivers.
  * Formulated the complete asynchronous **Hold Expiration Pipeline** using RabbitMQ Topic Exchanges with queue-level TTL (`900s` / 15m) and dead-letter routing (`inventory.hold.dlx.topic`) to avoid Head-of-Line (HoL) blocking.
  * Established an explicit **Redis TTL Buffer Strategy** (30-minute / `1800s` cache lifetime) to prevent cache eviction race conditions when holds are completed or cancelled early before the delayed RabbitMQ DLQ message arrives.
  * Traced and resolved local developer environment issues (Docker socket permission issues, Docker Compose invocation context, RabbitMQ health check startup delays, and MongoDB `_id` serialization mappings).
  * Used the user's concrete runtime stack traces to route fixes. MongoDB `_id` deserialization was addressed in the repository mapping, and the hold identifier was separated into Mongo `ObjectId` (`holdId`) versus `TransactionId` (Redis/RabbitMQ correlation ID).
  * Implemented the hold-list read as Redis cache discovery followed by a Mongo source-of-truth lookup, returning both cached and database status values for consistency inspection.

---

## 2. Human Audit & Engineering Interventions
This section records decisions made across planning, infrastructure setup, core domain modeling, and messaging/caching architecture.

### Accepted Suggestions
* **Infrastructure-first setup:** Added MongoDB, Redis, and RabbitMQ as separate Compose services so the future API container can connect using stable Compose service names.
* **Persistent development data:** Added named volumes for each dependency so container restarts do not discard local data.
* **Operational checks:** Added health checks for all three services and configurable ports and credentials through environment-variable interpolation.
* **RabbitMQ management image:** Kept `rabbitmq:4.1-management-alpine` for development visibility into exchanges and queues, even though the non-management Alpine image would be smaller.
* **Interface-based Infrastructure:** Added Redis, inventory Mongo, and hold Mongo interfaces, with concrete repositories registered behind those interfaces.
* **Domain repository & adapter boundary:** Added `IHoldRepository` and `IItemRepository` under Domain, made the existing Mongo contracts implement those abstractions, and registered the Domain interfaces to the Mongo implementations through Web API DI. Introduced domain-owned Adapter abstractions (`IRedisLockAdapter`, `IRedisHoldCacheAdapter`, `IRabbitMqHoldDelayAdapter`, `IRabbitMqEventPubAdapter`) so `HoldService` delegates to Adapters/Repositories without leaking infrastructure concerns.
* **Topic Exchange Topology for Delays & Events:** Configured durable topic exchanges (`inventory.hold.topic`, `inventory.hold.dlx.topic`, and `inventory.events.topic`) and bound the waiting queue (`inventory.hold.waiting.queue`) and expired queue (`inventory.hold.expired.queue`) via routing keys.
* **Queue-Level TTL Expiration:** Enforced a uniform 15-minute (`900000ms`) TTL at the queue level rather than per-message TTL, completely eliminating head-of-line blocking in RabbitMQ.
* **Redis Buffer Strategy for Early Completions:** Extended Redis cache TTL for both `ACTIVE` and `COMPLETED`/`RELEASED` states to 30 minutes (`1800s`). This ensures that if a hold completes in the first few seconds, the cache record is guaranteed to still be present when the 15-minute DLQ message reaches the worker, executing an O(1) fast-path discard.
* **Distributed Mutex Locking:** Implemented a 5-second Redis lock (`lock:hold:{holdId}`) with token verification on release, ensuring strict mutual exclusion between API checkout transactions and DLQ expiration worker execution.
* **Domain Hold Status & Event Publishing:** Added `HoldStatus` enum (`ACTIVE`, `COMPLETED`, `RELEASED`, `EXPIRED`, `CANCELLED`) under `InventoryHold.Contracts.Enums` and mandated outbound event publishing (`HoldStatusChangedEvent`) to `inventory.events.topic` on every state transition.
* **Atomic inventory updates:** Used MongoDB `$set` for absolute stock replacement and `$inc` for atomic increment/decrement operations.
* **Redis TTL behavior:** Implemented create-versus-update results for keys with TTL and an explicit missing-key message for reads.
* **Item service boundary:** Added `IItemService` and `ItemService` so validation and repository delegation remain outside the controller.
* **Item API:** Added service-backed batch creation, offset/limit retrieval, and explicit stock update endpoints. Empty pages return `404`, populated pages return `200`, and successful batch creation returns `201`.
* **Shared create contract:** Moved `CreateItemDto` into Contracts and excluded UUID from the request so MongoDB generates it and the response returns the assigned value.
* **UUID persistence fix:** Registered MongoDB's standard `GuidSerializer` so generated UUIDs can be serialized and queried consistently.
* **Cache and database reconciliation:** Kept hold state in Redis for a 30-minute buffer and used MongoDB as the authoritative status source when reading holds or when the Redis state is missing.
* **Hold lifecycle API:** Added create, complete, release, and expiry orchestration, status event publishing, and `GET /api/holds` aggregation behind controller-to-service-to-adapter/repository boundaries.

### Rejected / Deferred Suggestions
* **Smallest possible RabbitMQ image:** Did not switch to `rabbitmq:4.1-alpine` because the management UI is useful while developing and inspecting event topology. This can be changed for a production-oriented Compose profile later.
* **Unsupported MongoDB Alpine substitution:** Did not replace the official MongoDB image with an unofficial Alpine image solely to reduce size. MongoDB remains on the official `mongo:4.4` image currently present in Compose.
* **Cancelling in-flight RabbitMQ messages:** Rejected complex workarounds for attempting to cancel/delete in-flight messages inside RabbitMQ upon early checkout. Accepted the standard State Check / Idempotent Validation pattern where messages expire naturally to DLQ and short-circuit via Redis.
* **Short Redis cache TTL on completion:** Rejected setting short TTLs (e.g., 5–10 minutes) on completed hold cache entries. If an order completes at second 10, a short TTL would expire before the 15-minute DLQ message triggers, forcing an unnecessary database query fallback.
* **Premature application scaffolding:** Deferred .NET, React, repository, caching, messaging, and test implementation until the infrastructure baseline is agreed.
* **DI location correction:** The initial dependency-management class was placed in Infrastructure. Human review corrected this so `DependencyManagement` and its `Configurations` folder remain in `InventoryHold.WebApi`, while Infrastructure exposes only repository contracts and implementations.
* **Host build workflow:** Human review required restore, build, and publish validation to happen in Docker images rather than on the development machine.
* **Dependency direction:** Kept repository contracts in Domain and database-specific behavior in Infrastructure; no Domain-to-Infrastructure reference was introduced.
* **Validation workflow correction:** A host `dotnet build` was attempted during diagnosis, but human review rejected that as the project validation workflow. Docker Compose remains authoritative.
* **Controller-local DTO:** The initial create request model was corrected and moved to the Contracts project as `CreateItemDto`.
* **Client-owned UUID:** The controller was corrected so create requests do not require or accept a UUID; UUID assignment stays with the database repository.
* **Incorrect hold identifier mapping:** The initial implementation exposed `TransactionId` as `holdId`, which made delete fail because Mongo deletes by `ObjectId`. Human review corrected the mapping so `holdId` is Mongo `_id`, while `TransactionId` remains the messaging and Redis key.
* **Redis-only hold listing:** A Redis status cache alone was not accepted as the source of truth. The list API must discover cached IDs in Redis and then read current hold records and statuses from MongoDB before responding.
* **Direct host validation:** The repository instruction requires Docker-based build validation; direct host `dotnet build` was identified as incorrect and should not be repeated.

---

## 3. Verification & Testing Strategy
* **Tests and mocking:** `ItemServiceTests` uses Moq for `IItemRepository` and FluentAssertions for delegation and validation checks, including paging, stock operations, invalid input, and repository call counts.
* **Hold aggregation & adapter test:** Added `HoldServiceTests` with Moq mocks for `IHoldRepository`, `IRedisLockAdapter`, and `IRedisHoldCacheAdapter`. The tests verify stock delegation, distributed lock acquisition, status event publishing, and that a cached Redis status and a different current Mongo status are both returned accurately in `HoldSummaryDto`.
* **Idempotency & Race Condition Verification:** Verified that concurrent checkouts and background expiration worker runs cannot cause double stock rollbacks due to Redis distributed locks and atomic conditional MongoDB updates (`WHERE status = 'ACTIVE'`).
* **Static checks:** Editor diagnostics reported no errors for the touched Contracts, Domain, Infrastructure, Web API, and test files. `git diff --check` passed.
* **Docker validation:** Docker Compose build/recreate commands (`docker compose up -d --no-deps <service>`) were used as the required validation path. Verified isolated service recovery workflows and foreground diagnostic logging.
* **Runtime diagnosis:** MongoDB errors exposed two mapping issues: `Item` needed `_id` exclusion/extra-field tolerance, and public hold IDs needed to map to Mongo `ObjectId` rather than `TransactionId`. Both were corrected in Infrastructure and the API/service flow.

---

## 4. Key Takeaways & Velocity Impact
* AI was used to turn the assignment into an incremental execution plan and to establish a reproducible local dependency environment before application code is introduced.
* Pairing AI code generation with human architectural review accelerated the design of clean DDD layering (introducing the Domain Adapter layer), eliminating infrastructure leakage into the core business service.
* Designing the RabbitMQ DLQ and Redis caching topology through iterative discussions prevented subtle edge cases (head-of-line blocking, cache expiration before DLQ delivery, and race conditions between checkout and auto-expiry).
* Human review prioritized truthful repository state, configurable service connections, persistent development data, and an explicit tradeoff between image size and RabbitMQ observability.