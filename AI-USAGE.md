# AI-Augmentation Report (AI-USAGE.md)

## 1. AI Strategy & Tooling
* **Primary Tool:** GitHub Copilot.
* **Context Management Strategy:**
  * Supplied the assignment requirements, including the .NET 10 / React stack, DDD boundaries, MongoDB atomic inventory updates, Redis caching, RabbitMQ events, Docker Compose, and mandatory tests.
  * Used incremental prompts: first established the Compose infrastructure, then narrowed implementation to Infrastructure interfaces, repository operations, and Web API dependency registration.
  * Inspected the existing workspace and current files before editing. The repository-layer prompt was narrowed to the existing Hold and Item Mongo implementations, then routed through Domain-owned abstractions so Domain code does not depend on Infrastructure.
  * Used targeted file reads, git history, patch-based edits, and Docker Compose image builds. Prompts kept item creation and stock changes behind `IItemService`, with the Web API limited to HTTP translation and Contracts owning request DTOs.
  * Framed the item API around batch creation, offset/limit reads, explicit stock replacement, and database-owned UUID generation. The UUID serialization failure was traced from the Mongo stack trace to the driver configuration rather than patched in the controller.

---

## 2. Human Audit & Engineering Interventions
This section records decisions made during the current planning and infrastructure phase.

### Accepted Suggestions
* **Infrastructure-first setup:** Added MongoDB, Redis, and RabbitMQ as separate Compose services so the future API container can connect using stable Compose service names.
* **Persistent development data:** Added named volumes for each dependency so container restarts do not discard local data.
* **Operational checks:** Added health checks for all three services and configurable ports and credentials through environment-variable interpolation.
* **RabbitMQ management image:** Kept `rabbitmq:4.1-management-alpine` for development visibility into exchanges and queues, even though the non-management Alpine image would be smaller.
* **Interface-based Infrastructure:** Added Redis, inventory Mongo, and hold Mongo interfaces, with concrete repositories registered behind those interfaces.
* **Domain repository boundary:** Added `IHoldRepository` and `IItemRepository` under Domain, made the existing Mongo contracts implement those abstractions, and registered the Domain interfaces to the Mongo implementations through Web API DI.
* **Atomic inventory updates:** Used MongoDB `$set` for absolute stock replacement and `$inc` for atomic increment/decrement operations.
* **Redis TTL behavior:** Implemented create-versus-update results for keys with TTL and an explicit missing-key message for reads.
* **Item service boundary:** Added `IItemService` and `ItemService` so validation and repository delegation remain outside the controller.
* **Item API:** Added service-backed batch creation, offset/limit retrieval, and explicit stock update endpoints. Empty pages return `404`, populated pages return `200`, and successful batch creation returns `201`.
* **Shared create contract:** Moved `CreateItemDto` into Contracts and excluded UUID from the request so MongoDB generates it and the response returns the assigned value.
* **UUID persistence fix:** Registered MongoDB's standard `GuidSerializer` so generated UUIDs can be serialized and queried consistently.

### Rejected / Deferred Suggestions
* **Smallest possible RabbitMQ image:** Did not switch to `rabbitmq:4.1-alpine` because the management UI is useful while developing and inspecting event topology. This can be changed for a production-oriented Compose profile later.
* **Unsupported MongoDB Alpine substitution:** Did not replace the official MongoDB image with an unofficial Alpine image solely to reduce size. MongoDB remains on the official `mongo:4.4` image currently present in Compose.
* **Premature application scaffolding:** Deferred .NET, React, repository, caching, messaging, and test implementation until the infrastructure baseline is agreed.
* **DI location correction:** The initial dependency-management class was placed in Infrastructure. Human review corrected this so `DependencyManagement` and its `Configurations` folder remain in `InventoryHold.WebApi`, while Infrastructure exposes only repository contracts and implementations.
* **Host build workflow:** Human review required restore, build, and publish validation to happen in Docker images rather than on the development machine.
* **Dependency direction:** Kept repository contracts in Domain and database-specific behavior in Infrastructure; no Domain-to-Infrastructure reference was introduced.
* **Validation workflow correction:** A host `dotnet build` was attempted during diagnosis, but human review rejected that as the project validation workflow. Docker Compose remains authoritative.
* **Controller-local DTO:** The initial create request model was corrected and moved to the Contracts project as `CreateItemDto`.
* **Client-owned UUID:** The controller was corrected so create requests do not require or accept a UUID; UUID assignment stays with the database repository.

---

## 3. Verification & Testing Strategy
* **Completed validation:** `docker compose build api` succeeded after the controller and Contracts changes. Restore and `dotnet publish` ran inside the `mcr.microsoft.com/dotnet/sdk:10.0` build stage, and host `bin/obj` files are excluded through `.dockerignore`.
* **Tests and mocking:** `ItemServiceTests` uses Moq for `IItemRepository` and FluentAssertions for delegation and validation checks, including paging, stock operations, invalid input, and repository call counts. No focused controller tests were added yet.
* **Runtime diagnosis:** The first item insert exposed `GuidRepresentation.Unspecified` in MongoDB.Driver. The stack trace identified serialization before persistence; registering `GuidSerializer(GuidRepresentation.Standard)` addressed the configuration issue. A post-fix Docker rebuild was offered but declined, so runtime confirmation after that fix remains outstanding.
* **Runtime validation:** A full `docker compose up` was not recorded as successful in this session, so service connectivity and DI resolution at runtime remain to be verified.

---

## 4. Key Takeaways & Velocity Impact
* AI was used to turn the assignment into an incremental execution plan and to establish a reproducible local dependency environment before application code is introduced.
* Human review prioritized truthful repository state, configurable service connections, persistent development data, and an explicit tradeoff between image size and RabbitMQ observability.