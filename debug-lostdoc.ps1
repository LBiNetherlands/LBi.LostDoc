param($ldPath = $(throw "ldpath required"));



[string]$script:verbose = "";
$choices = @(
    (New-Object -TypeName PSObject @{
                                        C = "Extract System.Web.*.dll from Web.Host bin folder"; 
                                        A = @((Get-ChildItem -Path ..\..\..\LBi.LostDoc.Repository.Web.Host\bin\System.Web.*.dll).FullName | %{ "Extract -IncludeBclDocComments -Path {0} -Output .\tmp\" -f $_ })
                                    }),
    (New-Object -TypeName PSObject @{
                                        C = "Extract *.dll from Web.Host bin folder"; 
                                        A = @((Get-ChildItem -Path ..\..\..\LBi.LostDoc.Repository.Web.Host\bin\*.dll).FullName | %{ "Extract -IncludeBclDocComments -Path {0} -Output .\tmp\" -f $_ })
                                    }),
    (New-Object -TypeName PSObject @{
                                        C = "Extract Company.Project.Library"; 
                                        A = @("Extract -IncludeBclDocComments -Path ..\..\..\Company.Project.Library\bin\Debug\Company.Project.Library.dll -Output .\tmp\")
                                    }),
    (New-Object -TypeName PSObject @{
                                        C = "Extract Company.Project.AnotherLibrary";
                                        A = @("Extract -IncludeBclDocComments -Path ..\..\..\Company.Project.AnotherLibrary\bin\Debug\Company.Project.AnotherLibrary.dll -Output .\tmp\")
                                    }),
    (New-Object -TypeName PSObject @{
                                        C = "Extract All"; 
                                        A = @("Extract -IncludeBclDocComments -Path ..\..\..\Company.Project.AnotherLibrary\bin\Debug\Company.Project.AnotherLibrary.dll  -Output .\tmp\", 
                                              "Extract -IncludeBclDocComments -Path ..\..\..\Company.Project.Library\bin\Debug\Company.Project.Library.dll  -Output .\tmp\")
                                    }),
    (New-Object -TypeName PSObject @{
                                        C = "Extract with multiple versions"; 
                                        A = @(
                                                { ..\..\..\build-test-dlls.ps1 },
                                                "Extract -IncludeBclDocComments -Path .\tmp\v1\Company.Project.AnotherLibrary.dll  -Output .\tmp\", 
                                                "Extract -IncludeBclDocComments -Path .\tmp\v1\Company.Project.Library.dll  -Output .\tmp\", 
                                                "Extract -IncludeBclDocComments -Path .\tmp\v2\Company.Project.AnotherLibrary.dll  -Output .\tmp\", 
                                                "Extract -IncludeBclDocComments -Path .\tmp\v2\Company.Project.Library.dll  -Output .\tmp\"
                                              )
                                    }),
    (New-Object -TypeName PSObject @{
                                        C = "Template"; 
                                        A = @("Template -Path .\Tmp -Template Library -Force -Output .\Html -IgnoreVersionComponent Patch")
                                    }),
    (New-Object -TypeName PSObject @{
                                        C = "Template with Custom logo"; 
                                        A = @("Template -Path .\Tmp -Template Library -Force -Output .\Html -Arguments @{LogoUrl = 'duck.jpg'}")
                                    }),
    (New-Object -TypeName PSObject @{
                                        C = "Open output"; 
                                        A = {
                                            Invoke-Item -Path .\Html\Library.html
                                        }
                                    }),
    (New-Object -TypeName PSObject @{
                                        C = "Help"; 
                                        A = @("-Help -Full")
                                    }),
    (New-Object -TypeName PSObject @{
                                        C = "Clear output"; 
                                        A ={
                                            if (Test-Path -Path .\tmp -PathType Container) {
                                                Remove-Item -Path .\tmp -Recurse -Force
                                            }
                                            if (Test-Path -Path .\Html -PathType Container) {
                                                Remove-Item -Path .\Html -Recurse -Force
                                            }
                                        }
                                    }), 
    (New-Object -TypeName PSObject @{
                                        C = "Toggle verobisty"; 
                                        A ={
                                            if ($script:verbose -eq "") {
                                                $script:verbose = "-Verbose";
                                            } elseif ($script:verbose -eq "-Verbose") {
                                                $script:verbose = "-Quiet";
                                            } else {
                                                $script:verbose = "";
                                            }
                                            Write-Host "Flag: $verbose"
                                        }
                                    }),
    (New-Object -TypeName PSObject @{
                                        C = "Exit"; 
                                        A ={ exit 0;}
                                    })
);

$ErrorActionPreference = "Continue"

while ($true) {
    [int]$i = 1;
    $map = @{};
    Write-Host
    foreach ($kvp in $choices) {
        $map.Add($i, $kvp.A);
        "  [{0}] {1}" -f $i++,$kvp.C
    }
    Write-Host
    $choice = Read-Host -Prompt "Choice"
    Write-Host
    $args = $map[[int]$choice];

    if ($args -is [ScriptBlock]) {
        & $args;
    } else {
        foreach ($arg in $args) {
            if ($arg -is [ScriptBlock]) {
                & $arg;
            } else {
                [System.AppDomainSetup]$setup = New-Object -TypeName System.AppDomainSetup -Property @{ApplicationBase = [System.IO.Path]::GetDirectoryName($ldPath)}
                [System.AppDomain]$app = [System.AppDomain]::CreateDomain("lostdoc", [System.AppDomain]::CurrentDomain.Evidence, $setup)
                if ($script:verbose -ne "") {
                    $arg += ' ' + $script:verbose;
                }
                "Launching with arguments: " + $arg;
                $app.ExecuteAssembly($ldPath, $arg)
                [System.AppDomain]::Unload($app);
            }
        }
    }
}

