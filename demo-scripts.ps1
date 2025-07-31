# TechScriptAid AI Demo Scripts

# Function to test provider comparison
function Test-ProviderComparison {
    $body = @{
        text = "The artificial intelligence revolution is transforming how we work, communicate, and solve complex problems. Machine learning algorithms can now process vast amounts of data, identify patterns, and make predictions with remarkable accuracy."
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "https://localhost:7001/api/v1/ai-test/compare-providers" `
        -Method Post `
        -Body $body `
        -ContentType "application/json"
    
    Write-Host "Provider Comparison Results:" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 10
}

# Function to test circuit breaker
function Test-CircuitBreaker {
    param(
        [int]$Failures = 6
    )

    $body = @{
        numberOfRequests = 10
        simulateFailure = $true
        failuresCount = $Failures
        delayBetweenRequests = 100
    } | ConvertTo-Json

    Write-Host "Testing Circuit Breaker with $Failures failures..." -ForegroundColor Yellow
    
    $response = Invoke-RestMethod -Uri "https://localhost:7001/api/v1/ai-test/test-circuit-breaker" `
        -Method Post `
        -Body $body `
        -ContentType "application/json"
    
    $response | ConvertTo-Json -Depth 10
}

# Function to monitor health
function Watch-Health {
    while($true) {
        Clear-Host
        Write-Host "=== TechScriptAid AI Health Monitor ===" -ForegroundColor Cyan
        Write-Host "Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor White
        
        try {
            $health = Invoke-RestMethod -Uri "https://localhost:7001/health"
            Write-Host "`nOverall Status: $($health.status)" -ForegroundColor $(if($health.status -eq 'Healthy'){'Green'}else{'Red'})
            
            foreach($check in $health.entries.PSObject.Properties) {
                $status = $check.Value.status
                $color = switch($status) {
                    'Healthy' { 'Green' }
                    'Degraded' { 'Yellow' }
                    default { 'Red' }
                }
                Write-Host "$($check.Name): $status" -ForegroundColor $color
            }
        }
        catch {
            Write-Host "Failed to get health status" -ForegroundColor Red
        }
        
        Start-Sleep -Seconds 5
    }
}

# Function to simulate load
function Start-LoadTest {
    param(
        [int]$Threads = 5,
        [int]$RequestsPerThread = 10
    )

    Write-Host "Starting load test with $Threads threads..." -ForegroundColor Yellow
    
    $jobs = @()
    
    for($i = 0; $i -lt $Threads; $i++) {
        $job = Start-Job -ScriptBlock {
            param($ThreadId, $Requests)
            
            for($j = 0; $j -lt $Requests; $j++) {
                try {
                    $body = '"Load test from thread ' + $ThreadId + ', request ' + $j + '"'
                    Invoke-RestMethod -Uri "https://localhost:7001/api/v1/ai-test/demo-all-patterns" `
                        -Method Post `
                        -Body $body `
                        -ContentType "application/json" | Out-Null
                    
                    Write-Output "Thread $ThreadId - Request $j completed"
                }
                catch {
                    Write-Output "Thread $ThreadId - Request $j failed: $_"
                }
                
                Start-Sleep -Milliseconds 500
            }
        } -ArgumentList $i, $RequestsPerThread
        
        $jobs += $job
    }
    
    Write-Host "Waiting for load test to complete..." -ForegroundColor Yellow
    $jobs | Wait-Job | Receive-Job
    $jobs | Remove-Job
    
    Write-Host "Load test completed!" -ForegroundColor Green
}

# Menu
function Show-Menu {
    Clear-Host
    Write-Host "=== TechScriptAid AI Demo Menu ===" -ForegroundColor Cyan
    Write-Host "1. Test Provider Comparison"
    Write-Host "2. Test Circuit Breaker"
    Write-Host "3. Demo All Patterns"
    Write-Host "4. Test Rate Limiting"
    Write-Host "5. Watch Health Status"
    Write-Host "6. Run Load Test"
    Write-Host "7. Get Optimization Recommendations"
    Write-Host "Q. Quit"
    Write-Host ""
}

# Main loop
do {
    Show-Menu
    $choice = Read-Host "Select an option"
    
    switch($choice) {
        '1' { Test-ProviderComparison }
        '2' { Test-CircuitBreaker }
        '3' { 
            $text = Read-Host "Enter text to process"
            Invoke-RestMethod -Uri "https://localhost:7001/api/v1/ai-test/demo-all-patterns" `
                -Method Post `
                -Body """$text""" `
                -ContentType "application/json" | ConvertTo-Json -Depth 10
        }
        '4' {
            Invoke-RestMethod -Uri "https://localhost:7001/api/v1/ai-test/test-rate-limit" | 
                ConvertTo-Json -Depth 10
        }
        '5' { Watch-Health }
        '6' { Start-LoadTest }
        '7' {
            Invoke-RestMethod -Uri "https://localhost:7001/api/v1/ai-test/optimization-recommendations" |
                ConvertTo-Json -Depth 10
        }
        'Q' { break }
        default { Write-Host "Invalid option" -ForegroundColor Red }
    }
    
    if($choice -ne 'Q' -and $choice -ne '5') {
        Read-Host "`nPress Enter to continue"
    }
} while($choice -ne 'Q')