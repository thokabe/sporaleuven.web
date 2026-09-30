param(
    [ValidateSet("h2", "d2a")]
    [string]$competition
)

$url = "https://vlmadmin.herokuapp.com/public/reeksen/$competition/wedstrijden"
$response = Invoke-RestMethod -Method Get -Uri $url

$tempFileName = $competition + "_" + [DateTime]::Now.ToString("yyyyMMdd_HHmmss") + ".json"
$fullPathLatest = Join-Path -Path $PSScriptRoot -ChildPath $tempFileName
$response | ConvertTo-Json -depth 10 | Out-File -FilePath $fullPathLatest

$fullPathOriginal = Join-Path -Path $PSScriptRoot -ChildPath "../calendar/2627/vlm/$competition/items.json"

$fileOriginal = get-FileHash -path $fullPathOriginal
$fileLatest = get-FileHash -path $fullPathLatest

if ($fileOriginal.Hash -ne $fileLatest.Hash) {
    Write-Output "The files are different."
    Copy-Item -Path $fullPathLatest -Destination $fullPathOriginal -Force
} else {
    Write-Output "The files are the same."
}

get-item -Path $fullPathLatest | remove-item -force
