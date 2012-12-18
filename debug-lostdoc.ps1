param($ldPath = $(throw "ldpath required"));
trap {
    Write-Error -Exception $_.Exception
    Read-Host
}
$choices = @( 
    (New-Object -TypeName PSObject @{C = "Extract 1"; A = "Extract -Path ..\..\..\Company.Project.Library\bin\Debug\Company.Project.Library.dll"}),
    (New-Object -TypeName PSObject @{C = "Extract 2"; A = "Extract -Path ..\..\..\Company.Project.AnotherLibrary\bin\Debug\Company.Project.AnotherLibrary.dll"}),
    (New-Object -TypeName PSObject @{C = "Template"; A = "Template -Path `"`" -Template Library -Verbose"}),
    (New-Object -TypeName PSObject @{C = "Help"; A = "-HElp -Full"})
);

[int]$i = 1;

$map = @{};
foreach ($kvp in $choices) {
    $map.Add($i, $kvp.A);
    "[{0}] {1}" -f $i++,$kvp.C
}

$choice = Read-Host -Prompt "Choice"

$args = $map[[int]$choice];

"Launching with arguments: " + $args;

[System.Reflection.Assembly]::LoadFrom($ldPath);

[LBi.LostDoc.ConsoleApplication.Program]::Main($args);

#& $ldPath $args;

