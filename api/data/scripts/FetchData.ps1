param(
    [ValidateSet("h2", "d2a")]
    [string]$competition
)

function Retrieve-Latest(
    # param(
        [ValidateSet("h2", "d2a")]
        [string]$competition,
        [ValidateSet("wedstrijden", "ranking")]
        [string]$dataType
    # )
){
    $url = "https://vlmadmin.herokuapp.com/public/reeksen/$competition/$dataType"

    write-warning "url: $url"

    $tempFileName = $competition + "_" + [DateTime]::Now.ToString("yyyyMMdd_HHmmss") + ".json"
    $fullPathLatest = Join-Path -Path $PSScriptRoot -ChildPath $tempFileName
    $response = Invoke-RestMethod -Uri $url -Method Get
    $response | ConvertTo-Json -depth 10 | Out-File -FilePath $fullPathLatest

    $folderMapping = @{
        "wedstrijden" = "calendar"
        "ranking" = "ranking"
    }

    $fullPathOriginal = Join-Path -Path $PSScriptRoot -ChildPath "../$($folderMapping[$dataType])/2627/vlm/$competition/items.json"

 
    $fileOriginal = get-FileHash -path $fullPathOriginal
    $fileLatest = get-FileHash -path $fullPathLatest

    if ($fileOriginal.Hash -ne $fileLatest.Hash) {
        Write-Output "The files are different."
        Copy-Item -Path $fullPathLatest -Destination $fullPathOriginal -Force
    } else {
        Write-Output "The files are the same."
    }

    get-item -Path $fullPathLatest | remove-item -force
}

Retrieve-Latest -competition $competition -dataType "wedstrijden"
Retrieve-Latest -competition $competition -dataType "ranking"






