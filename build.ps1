Push-Location $PSScriptRoot
dotnet publish .\Source\KernelFun.csproj

Write-Host "========================================================================================================================"
Write-Host "You should probably make a new pwsh instance before importing to test:"
Write-Host Import-Module $PSScriptRoot\bin\Debug\net5.0\publish\KernelFun.dll