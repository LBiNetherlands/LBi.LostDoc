
$p1 = Join-Path -Path (Get-Location).Path -ChildPath "..\..\..\Company.Project.Library\Properties\AssemblyInfo.cs" -Resolve
$p2 = Join-Path -Path (Get-Location).Path -ChildPath "..\..\..\Company.Project.AnotherLibrary\Properties\AssemblyInfo.cs" -Resolve
$out = Join-Path -Path (Get-Location).Path -ChildPath "..\..\..\Company.Project.AnotherLibrary\bin\debug\" -Resolve
$t1 = Join-Path -Path (Get-Location).Path -ChildPath "tmp\v1\"
$t2 = Join-Path -Path (Get-Location).Path -ChildPath "tmp\v2\"

if (Test-Path -Path $out) {
    Remove-Item -Path $out -Recurse
}

if (Test-Path -Path $t1) {
    Remove-Item -Path $t1 -Recurse
}
if (Test-Path -Path $t2) {
    Remove-Item -Path $t2 -Recurse
}

$asmInfo = [System.IO.File]::ReadAllText($p1);

$asmInfo = ($asmInfo -replace "Version\(`"[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+`"\)","Version(`"1.0.0.130`")")

[System.IO.File]::WriteAllText($p1, $asmInfo, [System.Text.Encoding]::UTF8);

$asmInfo = [System.IO.File]::ReadAllText($p2);

$asmInfo = ($asmInfo -replace "Version\(`"[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+`"\)","Version(`"1.0.0.130`")")

[System.IO.File]::WriteAllText($p2, $asmInfo, [System.Text.Encoding]::UTF8);

& C:\Windows\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe ..\..\..\Company.Project.AnotherLibrary\Company.Project.AnotherLibrary.csproj /t:Rebuild

Copy-Item -Path $out -Destination $t1 -Recurse -Force -Container

$asmInfo = [System.IO.File]::ReadAllText($p1);

$asmInfo = $asmInfo -replace "Version\(`"[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+`"\)","Version(`"2.3.0.9130`")"

[System.IO.File]::WriteAllText($p1, $asmInfo, [System.Text.Encoding]::UTF8);

$asmInfo = [System.IO.File]::ReadAllText($p2);

$asmInfo = $asmInfo -replace "Version\(`"[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+`"\)","Version(`"2.1.3.5670`")"

[System.IO.File]::WriteAllText($p2, $asmInfo, [System.Text.Encoding]::UTF8);

& C:\Windows\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe ..\..\..\Company.Project.AnotherLibrary\Company.Project.AnotherLibrary.csproj /t:Rebuild

Copy-Item -Path $out -Destination $t2 -Recurse  -Force -Container