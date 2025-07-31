# Test Redis connectivity
Write-Host "Testing Redis connectivity..." -ForegroundColor Yellow

# Test with telnet (if available)
Write-Host "`nTesting port 6379..." -ForegroundColor Cyan
Test-NetConnection -ComputerName localhost -Port 6379

# Test with Redis CLI
Write-Host "`nTesting with Redis CLI..." -ForegroundColor Cyan
docker exec redis-dev redis-cli ping

# Check which process is using port 6379
Write-Host "`nChecking what's using port 6379..." -ForegroundColor Cyan
netstat -ano | findstr :6379