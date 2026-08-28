# Publishes the local SKINR thumbnail cache to the community preview shelf.
#
# INCREMENTAL by default: asks the shelf what it already has (GET /admin/thumbs)
# and uploads only the missing files -- the first run is the big one, every run
# after costs one index request plus whatever is new. -Force re-uploads all
# (after render-pipeline improvements).
#
# Server mode (the Railway shelf behind hub.evelens.dev):
#   .\tools\publish-hub-thumbs.ps1 -Server https://hub.evelens.dev -Token <UPLOAD_TOKEN>
#   (token: `railway variables` in D:\evelens-hub-server)
#
# Directory mode (any static hosting):
#   .\tools\publish-hub-thumbs.ps1 -Destination D:\deploy\hub\thumbs
param(
    [string]$Destination,
    [string]$Server,
    [string]$Token,
    [switch]$Force,

    # Debug builds cache under "EveLens Debug"; releases under "EveLens".
    [string]$Source = "$env:APPDATA\EveLens Debug\cache\skinr\thumbs"
)

if (-not (Test-Path $Source)) {
    Write-Error "No thumbnail cache at $Source -- browse the Hub (or run the pre-renderer) first."
    exit 1
}
$files = Get-ChildItem -Path $Source -Filter *.png

if ($Server) {
    if (-not $Token) { Write-Error "-Server needs -Token (the shelf's UPLOAD_TOKEN)"; exit 1 }
    $auth = @{ Authorization = "Bearer $Token" }

    # name -> size; a size mismatch means the local render changed and the shelf
    # copy is stale (name-only diffing once kept dark thumbnails alive forever).
    $have = @{}
    if (-not $Force) {
        try {
            $index = Invoke-RestMethod -Method Get -Uri "$Server/admin/thumbs" -Headers $auth
            foreach ($e in $index.thumbs) {
                if ($e -is [string]) { $have[$e.ToUpperInvariant()] = -1 }
                else { $have[$e.n.ToUpperInvariant()] = [long]$e.s }
            }
            Write-Host "Shelf has $($have.Count); local cache has $($files.Count)."
        } catch {
            Write-Warning "Could not read shelf index ($($_.Exception.Message)) -- uploading all."
        }
    }

    $ok = 0; $skipped = 0
    foreach ($f in $files) {
        $key = $f.Name.ToUpperInvariant()
        if (-not $Force -and $have.ContainsKey($key) -and
            ($have[$key] -eq -1 -or $have[$key] -eq $f.Length)) { $skipped++; continue }
        try {
            Invoke-RestMethod -Method Put -Uri "$Server/admin/thumbs/$($f.Name)" `
                -Headers $auth -InFile $f.FullName -ContentType "image/png" | Out-Null
            $ok++
        } catch {
            Write-Warning "$($f.Name): $($_.Exception.Message)"
        }
    }
    Write-Host "Published $ok new, skipped $skipped already on the shelf."
}
elseif ($Destination) {
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    robocopy $Source $Destination *.png /NDL /NJH /NJS | Out-Null
    Write-Host "Published $($files.Count) thumbnails to $Destination"
}
else {
    Write-Error "Give either -Server (Railway shelf) or -Destination (static dir)."
    exit 1
}
