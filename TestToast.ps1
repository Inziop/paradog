# Test script to check if Toast is working
Write-Host "Starting app with debug output..." -ForegroundColor Green

# Run dotnet in background and capture all output
$process = Start-Process -FilePath "dotnet" -ArgumentList "run" -WorkingDirectory "C:\Users\HOANG\ParadoxTranslator" -PassThru -RedirectStandardOutput "toast_output.log" -RedirectStandardError "toast_error.log" -NoNewWindow

Write-Host "App started (PID: $($process.Id))" -ForegroundColor Yellow
Write-Host "Waiting for app to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

Write-Host "`nPress ENTER after you've triggered AI Translation in the app..." -ForegroundColor Cyan
Read-Host

Write-Host "`nChecking logs..." -ForegroundColor Green

if (Test-Path "toast_output.log") {
    Write-Host "`n=== STDOUT ===" -ForegroundColor Magenta
    Get-Content "toast_output.log" | Select-String -Pattern "TOAST|ShowToast|Translation" -Context 2
}

if (Test-Path "toast_error.log") {
    Write-Host "`n=== STDERR (Debug) ===" -ForegroundColor Magenta
    Get-Content "toast_error.log" | Select-String -Pattern "TOAST|ShowToast|Translation" -Context 2
}

Write-Host "`nPress ENTER to stop app..." -ForegroundColor Yellow
Read-Host

Stop-Process -Id $process.Id -Force
Remove-Item "toast_output.log" -ErrorAction SilentlyContinue
Remove-Item "toast_error.log" -ErrorAction SilentlyContinue
Write-Host "Done!" -ForegroundColor Green
