<#
End-to-end smoke test against a running migration sample via the configured Azure
Bot's DirectLine channel. Sends a conversationUpdate and a few user messages, then
prints whatever the bot replies. Use to verify either sample (before or after) is
reachable and producing the expected activities.

Usage: $env:DL_SECRET = '<directline-secret>'; .\scripts\dl-smoketest.ps1
#>
[CmdletBinding()]
param(
    [string] $Secret = $env:DL_SECRET,
    [string[]] $UserSays = @('hello', 'Chris', 'yes'),
    [int] $WaitSeconds = 4
)

if (-not $Secret) {
    throw 'Provide DirectLine secret via -Secret or $env:DL_SECRET.'
}

$ErrorActionPreference = 'Stop'

$conv = Invoke-RestMethod `
    -Uri 'https://directline.botframework.com/v3/directline/conversations' `
    -Method POST `
    -Headers @{ Authorization = "Bearer $Secret" }

Write-Host "Conversation: $($conv.conversationId)"

foreach ($msg in $UserSays) {
    Invoke-RestMethod `
        -Uri "https://directline.botframework.com/v3/directline/conversations/$($conv.conversationId)/activities" `
        -Method POST `
        -Headers @{ Authorization = "Bearer $Secret" } `
        -ContentType 'application/json' `
        -Body (@{
            type    = 'message'
            from    = @{ id = 'user1'; name = 'Tester' }
            text    = $msg
        } | ConvertTo-Json -Depth 5 -Compress) | Out-Null
    Write-Host ">> $msg"
    Start-Sleep -Seconds $WaitSeconds
}

$reply = Invoke-RestMethod `
    -Uri "https://directline.botframework.com/v3/directline/conversations/$($conv.conversationId)/activities" `
    -Method GET `
    -Headers @{ Authorization = "Bearer $Secret" }

Write-Host ''
Write-Host '----- Bot activities -----'
$reply.activities | ForEach-Object {
    $who = if ($_.from.id -eq 'user1') { 'USER' } else { 'BOT ' }
    $payload = if ($_.text) { $_.text }
               elseif ($_.attachments) { "[attachment: $($_.attachments[0].contentType)]" }
               else { "[$($_.type)]" }
    "{0} {1}: {2}" -f $_.timestamp, $who, $payload
}
