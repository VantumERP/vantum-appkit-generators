# Build and Test Script for Vantum.AppKit.Generators

Write-Host "=== Building Vantum.AppKit.Generators ===" -ForegroundColor Cyan
dotnet build Vantum.AppKit.Generators.csproj -c Debug

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Generator build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ Generator built successfully!" -ForegroundColor Green

Write-Host "`n=== Building Test Consumer ===" -ForegroundColor Cyan
dotnet build TestConsumer\TestConsumer.csproj -c Debug

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Test consumer build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ Test consumer built successfully!" -ForegroundColor Green

Write-Host "`n=== Running Test Consumer ===" -ForegroundColor Cyan
dotnet run --project TestConsumer\TestConsumer.csproj --no-build

Write-Host "`n=== Build and Test Complete ===" -ForegroundColor Cyan
