param($Configuration = "debug")

#cleanup
Remove-Item -Path (".\Company.Project.Library\bin\{0}\*.ldoc" -f $Configuration)
Remove-Item -Path (".\Company.Project.AnotherLibrary\bin\{0}\*.ldoc" -f $Configuration)
#regen
& ".\LBi.LostDoc.ConsoleApplication\bin\$Configuration\lostdoc.exe" extract -Verbose -Path .\Company.Project.Library\bin\$Configuration\Company.Project.Library.dll -IncludeBclDocComments -NamespaceDocPath "nsdoc.xml"

if (!$?) {
    exit $LastExitCode
}

& ".\LBi.LostDoc.ConsoleApplication\bin\$Configuration\lostdoc.exe" extract -Verbose -Path .\Company.Project.AnotherLibrary\bin\$Configuration\Company.Project.AnotherLibrary.dll -IncludeBclDocComments -NamespaceDocPath "nsdoc.xml"

if (!$?) {
    exit $LastExitCode
}

#modify
# Get-Content ".\Company.Project.AnotherLibrary\bin\$Configuration\Company.Project.AnotherLibrary.dll_$Version.ldoc" -Encoding UTF8 | Foreach-Object {$_ -replace $Version, "2.3.4.5"} | Set-Content ".\Company.Project.AnotherLibrary\bin\$Configuration\Company.Project.AnotherLibrary.dll_2.3.4.5.ldoc" -Encoding UTF8
#copy

if (-not (Test-Path -Path .\LBi.LostDoc.Core.Test\Data\)) {
    New-Item -Path .\LBi.LostDoc.Core.Test\Data\ -ItemType Directory
}

$srcOne = ".\Company.Project.Library\bin\$Configuration\*.ldoc"
$srcTwo = ".\Company.Project.AnotherLibrary\bin\$Configuration\*.ldoc"

Write-Host -Object "Copying from: $srcOne"
Copy-Item -Path $srcOne -Destination .\LBi.LostDoc.Core.Test\Data\ -Force
Write-Host -Object "Copying from: $srcTwo"
Copy-Item -Path $srcTwo -Destination .\LBi.LostDoc.Core.Test\Data\ -Force