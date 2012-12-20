LBi.LostDoc
===========
LostDoc is a .Net documentation generator based on XML Documentation Comments, written in C# & XSLT. 

Usage
-----

To show help
```
lostdoc.exe -Help
```

To extract ldoc files from a .net assembly:
```
lostdoc.exe Extract -Path path\to\assembly.dll -Output .\tmp
```

To template a collection of ldoc files:
```
lostdoc.exe Template -Path .\tmp -Template Library -Verbose -Force -Output .\out
```

Known issues
------------
There is currently no support for async/await and dynamic.
See todo.txt for more details and information.

Debugging Console Application
-----------------------------
The source tree contains a "debug-lostdoc.ps1" file that can be used
to start various test scenarios.

Since these project settings are per-user, it will require a few manual steps to setup correctly:

1. In the "Debug" tab of the "LBi.LostDoc.ConsoleApplication" project properties, set the "Start external program" value to: ```C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe```
2. In the same screen, set "Command line arguments" to ```-File ..\..\..\debug-lostdoc.ps1 -ldPath .\lostdoc.exe```

When you debug "LBi.LostDoc.ConsoleApplication" you will now greeted by this prompt:
```

  [1] Extract Company.Project.Library
  [2] Extract Company.Project.AnotherLibrary
  [3] Extract All
  [4] Extract with multiple versions
  [5] Template
  [6] Template with Search
  [7] Open output
  [8] Help
  [9] Clear output
  [10] Toggle verbose
  [11] Exit

Choice:
```
