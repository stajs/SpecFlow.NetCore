$testProjectFolders = Get-ChildItem -Filter "Sample.Website.Tests.*"

foreach ($folder in $testProjectFolders) {

    $files = $folder | Get-ChildItem -Recurse | where { $_.Name -eq "app.config" -or $_.Name.EndsWith(".feature.cs") }
    Write-Host "`nFound $($files.Count) generated file(s) in $folder"-ForegroundColor Green

    foreach ($file in $files) {
        "Removing: $($file.FullName)"
        Remove-Item -Path $file.FullName
    }    
}