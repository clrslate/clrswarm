Write-Host "Starting CLR Swarm sample..."

Write-Host "Building and starting containers..." -ForegroundColor Yellow
Write-Host "Swarm will be available at http://localhost:8080 once started" -ForegroundColor Green
Write-Host "Press Ctrl+C to stop the containers" -ForegroundColor Yellow
Write-Host ""

try {
    docker-compose up --build
} finally {
    Write-Host ""
    Write-Host "Cleaning up containers..." -ForegroundColor Yellow
    docker-compose down
    Write-Host "Containers stopped and removed." -ForegroundColor Green
}