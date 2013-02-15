param($Configuration = "debug")

#cleanup
if (Test-Path .\LBi.LostDoc.Test\Data\Output\) {
    Remove-Item -Path .\LBi.LostDoc.Test\Data\Output\ -Recurse
}
#template
& ".\LBi.LostDoc.ConsoleApplication\bin\$Configuration\lostdoc.exe" template -Template Library -IgnoreVersionComponent Patch -Path .\LBi.LostDoc.Test\Data\ -Output .\LBi.LostDoc.Test\Data\Output\

exit $LastExitCode
