# SmartFlow

AI-powered support request classification and workflow automation.

SmartFlow receives support requests through an n8n webhook, classifies them with an LLM
(category, priority, summary, suggested action), validates the result, and stores it
via an ASP.NET Core REST API in SQLite.

> **Status:** 🚧 Work in progress – Phase 1 (planning) completed.

## Tech stack
ASP.NET Core (.NET 10, C#) · EF Core · SQLite · n8n · Docker Compose · Ollama / Claude API

## Documentation
- [Architecture](docs/architecture.md)
- [n8n workflows](n8n/README.md)

## Roadmap
- [x] Phase 1 – Planning
- [ ] Phase 2 – Backend API
- [ ] Phase 3 – Database (EF Core + SQLite)
- [ ] Phase 4 – n8n workflow + Docker Compose
- [ ] Phase 5 – AI integration
- [ ] Phase 6 – Testing
- [ ] Phase 7 – GitHub release
