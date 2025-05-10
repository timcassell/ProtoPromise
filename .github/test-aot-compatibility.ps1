param([string]$tfm, [string]$config)

$rootDirectory = Get-Location

$publishOutput = dotnet publish $rootDirectory/Tests/ProtoPromise.Tests.AotCompatibility/ProtoPromise.Tests.AotCompatibility.csproj -c $config -f $tfm -nodeReuse:false /p:UseSharedCompilation=false

$actualWarningCount = 0

foreach ($line in $($publishOutput -split "`r`n"))
{
    if (($line -like "*analysis warning IL*") -or ($line -like "*analysis error IL*"))
    {
        Write-Host $line
        $actualWarningCount += 1
    }
}

Write-Host "Actual warning count is:", $actualWarningCount
$expectedWarningCount = 0

if ($LastExitCode -ne 0)
{
    Write-Host "There was an error while publishing AotCompatibility Test App. LastExitCode is:", $LastExitCode
    Write-Host $publishOutput
}

$runtime = $IsWindows ? "win-x64" : ($IsMacOS ? "macos-x64" : "linux-x64")
$app = $IsWindows ? "./ProtoPromise.Tests.AotCompatibility.exe" : "./ProtoPromise.Tests.AotCompatibility"

Push-Location $rootDirectory/Tests/ProtoPromise.Tests.AotCompatibility/bin/$config/$tfm/$runtime

Write-Host "Executing test App..."
$app
Write-Host "Finished executing test App"

if ($LastExitCode -ne 0)
{
  Write-Host "There was an error while executing AotCompatibility Test App. LastExitCode is:", $LastExitCode
}

Pop-Location

$testPassed = 0
if ($actualWarningCount -ne $expectedWarningCount)
{
    $testPassed = 1
    Write-Host "Actual warning count:", $actualWarningCount, "is not as expected. Expected warning count is:", $expectedWarningCount
}

Exit $testPassed