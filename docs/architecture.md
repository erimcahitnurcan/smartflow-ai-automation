# SmartFlow – Architecture

## Goal
SmartFlow receives customer support requests, classifies them with an LLM,
validates the structured result, and stores it for later retrieval.

## Components
| Component | Responsibility |
|---|---|
| n8n | Receives requests via webhook, calls the LLM, forwards the result to the API |
| LLM (Ollama or Claude API) | Returns category, priority, summary and suggested action as JSON |
| SmartFlow.Api (ASP.NET Core) | Validates input, persists data, exposes query endpoints |
| SQLite | Lightweight persistent storage (Docker volume) |

## Request flow
1. Client sends `POST` with the request text to the n8n webhook.
2. n8n builds a prompt and calls the configured LLM.
3. n8n parses the LLM response as JSON (retries once on failure).
4. n8n calls `POST /api/requests` with the `X-Api-Key` header.
5. The API validates the payload; invalid data returns `400 Bad Request`.
6. Valid data is saved to SQLite; the API returns `201 Created`.
7. n8n returns the saved record to the client.

## Classification contract (LLM output)
```json
{
  "category": "Billing | Technical | Account | General",
  "priority": "Low | Medium | High",
  "summary": "string, max 300 chars",
  "suggestedAction": "string, max 500 chars"
}
```

### Priority rules
- **High**: service unavailable, security issue, blocking payment failure, data loss.
- **Medium**: broken feature with workaround, non-urgent billing error.
- **Low**: general questions, information requests, feedback.

## Data model: `SupportRequest`
| Field | Type | Rule |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `OriginalText` | `string` | Required, 1–5000 chars |
| `SenderEmail` | `string?` | Optional, valid email format |
| `Category` | enum | One of the four categories |
| `Priority` | enum | One of the three priorities |
| `Summary` | `string` | Required, 1–300 chars |
| `SuggestedAction` | `string` | Required, 1–500 chars |
| `ModelName` | `string` | LLM that produced the classification |
| `CreatedAtUtc` | `DateTime` | Set by the server |

## API endpoints
| Method | Route | Description |
|---|---|---|
| POST | `/api/requests` | Store a classified request (requires `X-Api-Key`) |
| GET | `/api/requests` | List requests (filters: `category`, `priority`; paging: `page`, `pageSize`) |
| GET | `/api/requests/{id}` | Get a single request |
| GET | `/health` | Health check |

## Key decisions
- **n8n orchestrates, the API validates.** LLM output is untrusted; the API is the final gate before the database.
- **Ollama by default.** Runs locally without API keys; Claude API is an optional provider.
- **Single API project.** A layered architecture would be unnecessary complexity at this size.
- **Secrets via environment variables.** Only `.env.example` is committed.

## Out of scope (MVP)
User authentication, frontend, update/delete endpoints, notifications, cloud deployment.
