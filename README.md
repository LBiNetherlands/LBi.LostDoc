![Logo](LBi.LostDoc.Templates/Library/images/lostdoc.svg)

LBi.LostDoc
===========
LostDoc is a .Net documentation generator based on XML Documentation Comments, written in C# & XSLT. 

Usage
-----

To show help
```
lostdoc.exe -Help
```

To create help from one or more .net assemblies:
```
lostdoc.exe -Path "a.dll","b.dll","c.dll" -Template Library -IgnoreVersionComponent Patch -IncludeBclDocComments
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
There is currently no support for async/await an co/contravariance in the XSLTs.
See todo.txt for more details and information.

Contribute
----------
All pull requests must now include the following line in the pull request comments (using your full name and email address), which indicates your contribution complies to the LostDoc Developer's Certificate of Origin v1.0:

```LostDoc-DCO-1.0-Signed-off-by: Joe Smith <joe@example.com>```

Below is a layman's description of the five points in the LostDoc DCO (be sure to read and agree to the full text in ```lostdoc-dco-v1.0.txt```):

* I created this contribution/change and have the right to submit it to the Project; or
* I created this contribution/change based on a previous work with a compatible open source license; or
* This contribution/change has been provided to me by someone who did (a) or (b) and I am submitting the contribution unchanged.
* I understand this contribution is public and may be redistributed as open source software.
* I understand that I retain copyright ownership in this contribution and I am granting the Project a copyright license to use, modify and distribute my contribution. The Project may relicense my contribution under other OSI-approved licenses.

The LostDoc DCO and signoff process was heavily influenced by the EnyoJS contribution process.


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
