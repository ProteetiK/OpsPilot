# OpsPilot

AI-assisted incident management and root-cause analysis platform.

## Repository layout

- `src/api-dotnet/` — ASP.NET Core API and application/domain/infrastructure layers.
- `src/ai-service/` — Python FastAPI service for retrieval and AI investigations.
- `src/web/` — Web interface.
- `infra/` — Docker, Azure, messaging and monitoring configuration.
- `tests/` — Cross-service, end-to-end and performance tests.
- `docs/` — Architecture, decisions, API, data model and evaluation notes.

## Getting started

This repository is a scaffold. Add implementation and setup instructions as the services are created.

1. Copy relevant `.env.example` files to local environment files.
2. Start infrastructure using the Docker Compose file once it is implemented.
3. Apply database migrations.
4. Start the .NET API, Python AI service and web application.

## Engineering principles

- The .NET API owns core business entities and incident lifecycle rules.
- The Python service owns AI investigation and retrieval workflows.
- PostgreSQL is the source of truth for transactional application data.
- AI findings are advisory, evidence-linked and subject to human review.
- Never commit secrets, credentials or production data.
