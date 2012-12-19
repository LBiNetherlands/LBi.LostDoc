param($ldPath = $(throw "ldpath required"));
trap {
    Write-Error $_
    Read-Host
}
$choices = @( 
    (New-Object -TypeName PSObject @{
                                        C = "Extract Company.Project.Library"; 
                                        A = @("Extract -Path ..\..\..\Company.Project.Library\bin\Debug\Company.Project.Library.dll -Verbose -Output .\tmp\")
                                    }),
    (New-Object -TypeName PSObject @{
                                        C = "Extract Company.Project.AnotherLibrary";
                                        A = @("Extract -Path ..\..\..\Company.Project.AnotherLibrary\bin\Debug\Company.Project.AnotherLibrary.dll -Verbose -Output .\tmp\")
                                    }),
    (New-Object -TypeName PSObject @{
                                        C = "Extract Company.Project.Library and Extract Company.Project.AnotherLibrary"; 
                                        A = @("Extract -Path ..\..\..\Company.Project.AnotherLibrary\bin\Debug\Company.Project.AnotherLibrary.dll -Verbose  -Output .\tmp\", 
                                              "Extract -Path ..\..\..\Company.Project.Library\bin\Debug\Company.Project.Library.dll -Verbose  -Output .\tmp\")
                                    }),
    (New-Object -TypeName PSObject @{
                                        C = "Template Both Libraries"; 
                                        A = @("Template -Path .\Tmp -Template Library -Verbose -Force -Output .\Html")
                                    }),
    (New-Object -TypeName PSObject @{
                                        C = "Help"; 
                                        A = @("-Help -Full")
                                    })
);

[int]$i = 1;
Write-Host
$map = @{};
foreach ($kvp in $choices) {
    $map.Add($i, $kvp.A);
    "  [{0}] {1}" -f $i++,$kvp.C
}
Write-Host
$choice = Read-Host -Prompt "Choice"

$args = $map[[int]$choice];

[void][System.Reflection.Assembly]::LoadFrom($ldPath);

foreach ($arg in $args) {
    Write-Host
    Write-Host
    "Launching with arguments: " + $arg;
    Write-Host
    [LBi.LostDoc.ConsoleApplication.Program]::Main($arg);
    
    Write-Host "================================================="
}

Read-Host