param(
    [Parameter(Position = 0)]
    [ValidateSet("up", "down", "build", "rebuild", "restart", "logs", "status", "clean")]
    [string]$Command = "up"
)

$ErrorActionPreference = "Stop"

switch ($Command) {
    "up" {
        docker compose up -d
    }

    "down" {
        docker compose down
    }

    "build" {
        docker compose build
    }

    "rebuild" {
        docker compose down
        docker compose up -d --build
    }

    "restart" {
        docker compose restart
    }

    "logs" {
        docker compose logs --follow
    }

    "status" {
        docker compose ps
    }

    "clean" {
        docker compose down --volumes --remove-orphans
    }
}

if ($LASTEXITCODE -ne 0) {
    Write-Error "Docker Compose Failed."
    exit $LASTEXITCODE
}