# Evaluates local Ollama models on the labeled samples in classification-samples.json.
#
# Uses exactly the same system prompt and JSON schema as the n8n workflow
# (prompts/classification-system-prompt.txt, prompts/classification-output-schema.json),
# so the numbers reflect what SmartFlow would actually do.
#
# Usage (stack running, models pulled):
#   .\eval\Invoke-ClassificationEval.ps1 -Models qwen2.5:3b, llama3.2:3b
#
# Per-sample results are written to eval/results/<model>.csv.

param(
    [string[]] $Models = @('qwen2.5:3b'),
    [string] $OllamaUrl = 'http://localhost:11434'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

# Read files as plain UTF-8 strings.
# Not Get-Content: in Windows PowerShell 5.1 its strings carry hidden PSPath/PSDrive properties,
# which ConvertTo-Json -Depth 10 tries to serialize and then hangs. UTF-8 also keeps German/Turkish characters intact.
function Read-Utf8([string] $Path) { [IO.File]::ReadAllText($Path, [Text.Encoding]::UTF8) }

$systemPrompt = Read-Utf8 (Join-Path $root 'prompts/classification-system-prompt.txt')
$schema = Read-Utf8 (Join-Path $root 'prompts/classification-output-schema.json') | ConvertFrom-Json
$samples = Read-Utf8 (Join-Path $PSScriptRoot 'classification-samples.json') | ConvertFrom-Json

$resultsDir = Join-Path $PSScriptRoot 'results'
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

function Invoke-OllamaJson([string] $Path, [hashtable] $Payload) {
    $json = $Payload | ConvertTo-Json -Depth 10
    Invoke-RestMethod -Method Post -Uri "$OllamaUrl$Path" -ContentType 'application/json; charset=utf-8' `
        -Body ([Text.Encoding]::UTF8.GetBytes($json)) -TimeoutSec 600
}

$overview = @()

foreach ($model in $Models) {
    Write-Host "`n=== $model ===" -ForegroundColor Cyan

    # Warm-up: load the model into memory so loading time is not counted for the first sample.
    Invoke-OllamaJson '/api/generate' @{ model = $model; prompt = 'ok'; stream = $false } | Out-Null

    $rows = foreach ($sample in $samples) {
        $payload = @{
            model    = $model
            stream   = $false
            format   = $schema
            options  = @{ temperature = 0 }
            messages = @(
                @{ role = 'system'; content = $systemPrompt },
                @{ role = 'user'; content = $sample.text }
            )
        }

        $parsed = $null
        $errorMessage = $null
        $watch = [Diagnostics.Stopwatch]::StartNew()
        try {
            $response = Invoke-OllamaJson '/api/chat' $payload
            $parsed = $response.message.content | ConvertFrom-Json
        }
        catch {
            $errorMessage = $_.Exception.Message
        }
        $watch.Stop()
        Write-Host ("  sample {0,2}: {1,5:N1}s {2}" -f $sample.id, $watch.Elapsed.TotalSeconds, $(if ($errorMessage) { "ERROR $errorMessage" } else { "$($parsed.category)/$($parsed.priority)" }))

        # Same rules the SmartFlow API enforces (case-sensitive enum values, length limits).
        $valid = $null -ne $parsed -and
            ($parsed.category -cin 'Billing', 'Technical', 'Account', 'General') -and
            ($parsed.priority -cin 'Low', 'Medium', 'High') -and
            -not [string]::IsNullOrWhiteSpace($parsed.summary) -and $parsed.summary.Length -le 300 -and
            -not [string]::IsNullOrWhiteSpace($parsed.suggestedAction) -and $parsed.suggestedAction.Length -le 500

        [pscustomobject]@{
            Id               = $sample.id
            ExpectedCategory = $sample.category
            ActualCategory   = $parsed.category
            ExpectedPriority = $sample.priority
            ActualPriority   = $parsed.priority
            Valid            = [bool] $valid
            Seconds          = [math]::Round($watch.Elapsed.TotalSeconds, 1)
            Summary          = $parsed.summary
            SuggestedAction  = $parsed.suggestedAction
            Error            = $errorMessage
        }
    }

    $rows | Format-Table Id, ExpectedCategory, ActualCategory, ExpectedPriority, ActualPriority, Valid, Seconds -AutoSize |
        Out-String | Write-Host

    $fileName = ($model -replace '[^a-zA-Z0-9.-]', '_') + '.csv'
    $rows | Export-Csv (Join-Path $resultsDir $fileName) -NoTypeInformation -Encoding UTF8

    $count = @($rows).Count
    $overview += [pscustomobject]@{
        Model           = $model
        ValidJson       = "$(@($rows | Where-Object Valid).Count)/$count"
        CategoryCorrect = "$(@($rows | Where-Object { $_.ActualCategory -ceq $_.ExpectedCategory }).Count)/$count"
        PriorityCorrect = "$(@($rows | Where-Object { $_.ActualPriority -ceq $_.ExpectedPriority }).Count)/$count"
        BothCorrect     = "$(@($rows | Where-Object { $_.ActualCategory -ceq $_.ExpectedCategory -and $_.ActualPriority -ceq $_.ExpectedPriority }).Count)/$count"
        AvgSeconds      = [math]::Round((@($rows) | Measure-Object Seconds -Average).Average, 1)
        MaxSeconds      = (@($rows) | Measure-Object Seconds -Maximum).Maximum
    }
}

Write-Host "`n=== Overview ===" -ForegroundColor Cyan
$overview | Format-Table -AutoSize
