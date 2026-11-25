# Exports all n8n workflows from the running "n8n" container into n8n/workflows/.
#
# The raw n8n export contains instance data (owner account name and email, project IDs,
# version counters). This script keeps only the portable parts of each workflow, so the
# files are safe to commit. Credentials are referenced by name only; their secrets
# are never part of an n8n export.
#
# Usage (from the repository root, with the stack running):
#   .\scripts\export-n8n-workflows.ps1

$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)

# 1. Raw export inside the container.
docker compose exec -T n8n sh -c "rm -rf /tmp/wf && mkdir -p /tmp/wf && n8n export:workflow --all --separate --output=/tmp/wf/"
if ($LASTEXITCODE -ne 0) { throw 'n8n export failed. Is the stack running (docker compose up -d)?' }

# 2. Sanitize with Node.js inside the container (the n8n image ships with Node).
$sanitize = @'
const fs = require('fs');
const source = '/tmp/wf';
const target = '/tmp/wf-clean';
fs.rmSync(target, { recursive: true, force: true });
fs.mkdirSync(target);
for (const file of fs.readdirSync(source)) {
  const wf = JSON.parse(fs.readFileSync(`${source}/${file}`, 'utf8'));
  const portable = {
    id: wf.id,
    name: wf.name,
    active: false,
    nodes: wf.nodes,
    connections: wf.connections,
    settings: wf.settings,
    pinData: {}
  };
  const slug = wf.name.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
  fs.writeFileSync(`${target}/${slug}.json`, JSON.stringify(portable, null, 2) + '\n');
  console.log(`exported ${slug}.json`);
}
'@
$sanitize | docker compose exec -T n8n node -
if ($LASTEXITCODE -ne 0) { throw 'Sanitizing the export failed.' }

# 3. Copy the sanitized files into the repository.
New-Item -ItemType Directory -Force -Path 'n8n/workflows' | Out-Null
$container = docker compose ps -q n8n
docker cp "${container}:/tmp/wf-clean/." 'n8n/workflows/'
if ($LASTEXITCODE -ne 0) { throw 'Copying the files out of the container failed.' }
