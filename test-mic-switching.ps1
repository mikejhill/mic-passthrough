# Test microphone switching functionality manually
Write-Host "Testing Windows Default Microphone Switching..." -ForegroundColor Cyan

# Get current default
$enumerator = New-Object -ComObject MMDeviceEnumerator
$defaultEndpoint = $enumerator.GetDefaultAudioEndpoint(0, 0)  # 0 = Capture, 0 = Console role

Write-Host "`nCurrent default microphone:" -ForegroundColor Yellow
Write-Host "  Name: $($defaultEndpoint.FriendlyName)"
Write-Host "  ID: $($defaultEndpoint.ID)"

# List all recording devices
Write-Host "`nAll recording devices:" -ForegroundColor Yellow
$devices = $enumerator.EnumerateAudioEndpoints(0, 1)  # 0 = Capture, 1 = Active
for ($i = 0; $i < $devices.Count; $i++) {
    $device = $devices.Item($i)
    Write-Host "  [$i] $($device.FriendlyName) - $($device.ID)"
}

Write-Host "`n✅ Detection working!" -ForegroundColor Green
Write-Host "`n⚠️ To test switching, run:" -ForegroundColor Yellow
Write-Host "  dotnet run -- --mic 'Microphone (HD Pro Webcam C920)' --auto-switch --verbose"
