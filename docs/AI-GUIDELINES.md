# AI Contribution Guidelines

All AI agents working within this repository must respect the following ground rules before making any change:

1. **Honor `.editorconfig`** – always rely on the repo's formatting settings (indentation, line endings, charset, etc.). Run the appropriate formatter or adjust your editor to match the `.editorconfig` directives before saving files.

2. **Backend architecture guardrails**
   - **Dependency Injection Pattern** – use the existing DI containers/modules when wiring services together.
   - **Clean Architecture Pattern** – keep business logic inside the application/domain layers and depend on abstractions when crossing boundaries.
   - **Repository Pattern** – data access must go through repository interfaces; avoid leaking persistence details into higher layers.
   - **DRY** – extract shared logic instead of duplicating code.

3. **Frontend architecture guardrails**
   - **Dependency Injection via TSyringe** – reuse the TSyringe container/tokens that the project already exposes for shared services/use cases.
   - **Clean Architecture Pattern** – isolate UI, application logic, and infrastructure concerns; avoid coupling view components directly to network/persistence layers.
   - **Auth & Security** – respect existing authentication helpers, never bypass authorization checks, and avoid exposing secrets or tokens in logs.
   - **DRY** – prefer shared hooks/components/utilities over copy/paste.

Any new feature, refactor, or test must be evaluated against these principles. If a requirement forces a deviation, document the reasoning (e.g., in the PR description or inline comments) so that future contributors understand the trade-off.

