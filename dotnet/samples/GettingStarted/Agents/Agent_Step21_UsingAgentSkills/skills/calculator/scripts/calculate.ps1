#!/usr/bin/env pwsh
# Simple calculator script that simulates mathematical calculations

param(
    [Parameter(Mandatory=$true, Position=0)]
    [string]$Expression
)

# Mock implementation - in a real scenario, this would perform actual calculations
# For demo purposes, we'll return simulated results based on common patterns

Write-Host "Calculating: $Expression"
Write-Host ""

# Simulate calculation result
if ($Expression -match "^\(?\d+\s*[\+\-\*/]\s*\d+\)?$") {
    # Basic arithmetic
    $result = Invoke-Expression $Expression
    Write-Host "Result: $result"
} elseif ($Expression -match "miles? to km") {
    Write-Host "Result: 160.93 kilometers"
    Write-Host "(Conversion: 100 miles = 160.93 km)"
} elseif ($Expression -match "(\d+)%\s+of\s+(\d+)") {
    $percent = [int]$matches[1]
    $number = [int]$matches[2]
    $result = ($percent / 100) * $number
    Write-Host "Result: $result"
} else {
    Write-Host "Result: 42"
    Write-Host "(Mock result - in a real implementation, this would evaluate: $Expression)"
}

exit 0
