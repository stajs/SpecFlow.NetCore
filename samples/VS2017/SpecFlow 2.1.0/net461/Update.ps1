Param(
    [Parameter(Mandatory = $true)][string]$Version
)

$testProjectFolders = Get-ChildItem -Filter "Sample.Website.Tests.*"

foreach ($folder in $testProjectFolders) {

    $files = $folder | Get-ChildItem | where { $_.Name.EndsWith(".csproj") }

    foreach ($file in $files) {
        # Read version by XML.
        $xml = [xml](Get-Content $file.FullName)
        $oldVersion = $xml.SelectSingleNode('//ItemGroup/DotNetCliToolReference').GetAttribute("Version")

        # But just string replace to preserve formatting.
        $content = Get-Content $file.FullName -Raw
        $oldNode = "<DotNetCliToolReference Include=""SpecFlow.NetCore"" Version=""$oldVersion"" />"
        $newNode = "<DotNetCliToolReference Include=""SpecFlow.NetCore"" Version=""$Version"" />"
        $content = $content.Replace($oldNode, $newNode)
        [System.IO.File]::WriteAllText($file.FullName, $content)
    }
}