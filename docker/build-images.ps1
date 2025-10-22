#!/usr/bin/env pwsh
# Build Docker images for FRNQ application

param(
	[switch]$Api,
	[switch]$Ui,
	[string]$Tag = "latest",
	[string]$ApiUrl = "http://localhost:8080"
)

$ErrorActionPreference = "Stop"

Write-Host "Building FRNQ Docker Images" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan
Write-Host ""

# Change to docker directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $scriptPath

try {
	# If no specific service specified, build both
	$buildAll = -not ($Api -or $Ui)

	if ($Api -or $buildAll) {
		Write-Host "Building API image (frnq-api:$Tag)..." -ForegroundColor Yellow
		docker build -t "frnq-api:$Tag" -f Dockerfile.api ..
				
		if ($LASTEXITCODE -ne 0) {
			throw "Failed to build API image"
		}
				
		Write-Host "✓ API image built successfully" -ForegroundColor Green
		Write-Host ""
	}

	if ($Ui -or $buildAll) {
		Write-Host "Building UI image (frnq-ui:$Tag)..." -ForegroundColor Yellow
		Write-Host "API URL: $ApiUrl" -ForegroundColor Cyan
		docker build -t "frnq-ui:$Tag" --build-arg VITE_API_BASE_URL="$ApiUrl" -f Dockerfile.ui ..
				
		if ($LASTEXITCODE -ne 0) {
			throw "Failed to build UI image"
		}
				
		Write-Host "✓ UI image built successfully" -ForegroundColor Green
		Write-Host ""
	}

	Write-Host "=============================" -ForegroundColor Cyan
	Write-Host "All images built successfully!" -ForegroundColor Green
	Write-Host ""
	Write-Host "Available images:" -ForegroundColor Cyan
	docker images | Select-String -Pattern "frnq-(api|ui)"
	Write-Host ""
	Write-Host "UI was built with API URL: $ApiUrl" -ForegroundColor Cyan
	Write-Host "To start services, run: docker-compose up -d" -ForegroundColor Yellow
}
catch {
	Write-Host "Error: $_" -ForegroundColor Red
	exit 1
}
finally {
	Pop-Location
}
