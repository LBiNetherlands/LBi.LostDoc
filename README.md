LBi.LostDoc
===========

Debugging Console Application
-----------------------------
The source tree contains a "debug-lostdoc.ps1" file that can be used
to start various test scenarios.

Since these project settings are per-user, it will require a few manual steps to setup correctly:

1. In the "Debug" tab of the "LBi.LostDoc.ConsoleApplication" project properties, set the "Start external program" value to "C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe" (without the quotes).
2. In the same screen, set "Command line arguments" to "-File ..\..\..\debug-lostdoc.ps1 -ldPath .\lostdoc.exe" (without the quotes).


