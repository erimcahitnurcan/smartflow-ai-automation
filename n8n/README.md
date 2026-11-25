# n8n workflows

`workflows/` contains the SmartFlow n8n workflows as portable JSON.
They contain no secrets: credentials are referenced by name only.

| File | Purpose |
|---|---|
| `workflows/smartflow-classify-support-request.json` | `POST /webhook/support-request` → classify → save via SmartFlow API → respond with the stored record |

## Webhook contract

```http
POST http://localhost:5678/webhook/support-request
Content-Type: application/json

{ "text": "I forgot my password and cannot log in.", "senderEmail": "customer@example.com" }
```

`senderEmail` is optional. The response is the stored request (`201 Created`).

## Import a workflow into a fresh n8n

With the stack running (`docker compose up -d`), from the repository root:

```powershell
docker compose cp n8n/workflows/smartflow-classify-support-request.json n8n:/tmp/workflow.json
docker compose exec n8n n8n import:workflow --input=/tmp/workflow.json
```

Then in the n8n UI (http://localhost:5678):

1. **Credentials → Create credential → Header Auth**, name it `SmartFlow API key`.
   - Name: `X-Api-Key`
   - Value: the `SMARTFLOW_API_KEY` value from your `.env`
2. Open the workflow, select that credential in the **Save to SmartFlow API** node.
3. Click **Publish**.

## Export after changing a workflow

```powershell
.\scripts\export-n8n-workflows.ps1
```

The script removes instance data from the raw n8n export (owner name and email, project IDs)
so the files are safe to commit. Review the diff before committing.
