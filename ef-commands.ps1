# EF Core Command Helper Script
# Place this in your solution root folder

# Add Migration
function Add-Mig {
    param(
        [Parameter(Mandatory=$true)]
        [string]$name,
        
        [string]$project = "TechScriptAid.Infrastructure",
        [string]$startupProject = "TechScriptAid.API"
    )
    
    Write-Host "Adding migration: $name" -ForegroundColor Green
    Add-Migration $name -Project $project -StartupProject $startupProject
}

# Update Database
function Update-Db {
    param(
        [string]$project = "TechScriptAid.Infrastructure",
        [string]$startupProject = "TechScriptAid.API",
        [string]$targetMigration
    )
    
    Write-Host "Updating database..." -ForegroundColor Green
    if ($targetMigration) {
        Update-Database -Project $project -StartupProject $startupProject -Migration $targetMigration
    } else {
        Update-Database -Project $project -StartupProject $startupProject
    }
}

# Remove Last Migration
function Remove-LastMig {
    param(
        [string]$project = "TechScriptAid.Infrastructure",
        [string]$startupProject = "TechScriptAid.API"
    )
    
    Write-Host "Removing last migration..." -ForegroundColor Yellow
    Remove-Migration -Project $project -StartupProject $startupProject
}

# List Migrations
function List-Mig {
    param(
        [string]$project = "TechScriptAid.Infrastructure",
        [string]$startupProject = "TechScriptAid.API"
    )
    
    Write-Host "Listing all migrations..." -ForegroundColor Cyan
    Get-Migration -Project $project -StartupProject $startupProject
}

# Generate SQL Script
function Script-Mig {
    param(
        [string]$from,
        [string]$to,
        [string]$output = "Migrations.sql",
        [string]$project = "TechScriptAid.Infrastructure",
        [string]$startupProject = "TechScriptAid.API"
    )
    
    Write-Host "Generating SQL script..." -ForegroundColor Magenta
    if ($from -and $to) {
        Script-Migration -From $from -To $to -Output $output -Project $project -StartupProject $startupProject
    } elseif ($to) {
        Script-Migration -To $to -Output $output -Project $project -StartupProject $startupProject
    } else {
        Script-Migration -Output $output -Project $project -StartupProject $startupProject
    }
    Write-Host "SQL script generated: $output" -ForegroundColor Green
}

# Drop Database (Development Only!)
function Drop-Db {
    param(
        [string]$project = "TechScriptAid.Infrastructure",
        [string]$startupProject = "TechScriptAid.API"
    )
    
    Write-Host "WARNING: This will drop the database!" -ForegroundColor Red
    $confirm = Read-Host "Are you sure? (yes/no)"
    
    if ($confirm -eq "yes") {
        Drop-Database -Project $project -StartupProject $startupProject
        Write-Host "Database dropped." -ForegroundColor Yellow
    } else {
        Write-Host "Operation cancelled." -ForegroundColor Green
    }
}

# Reset Database (Drop, Create, Migrate, Seed)
function Reset-Db {
    Write-Host "Resetting database..." -ForegroundColor Yellow
    Drop-Db
    Update-Db
    Write-Host "Database reset complete!" -ForegroundColor Green
}

# Show available commands
function Show-EfCommands {
    Write-Host "`nAvailable EF Core Commands:" -ForegroundColor Cyan
    Write-Host "  Add-Mig <name>       - Add a new migration" -ForegroundColor White
    Write-Host "  Update-Db            - Update database to latest migration" -ForegroundColor White
    Write-Host "  Remove-LastMig       - Remove the last migration" -ForegroundColor White
    Write-Host "  List-Mig             - List all migrations" -ForegroundColor White
    Write-Host "  Script-Mig           - Generate SQL script" -ForegroundColor White
    Write-Host "  Drop-Db              - Drop the database (DEV ONLY!)" -ForegroundColor White
    Write-Host "  Reset-Db             - Reset database (drop, create, migrate)" -ForegroundColor White
    Write-Host "`n"
}

# Display available commands on load
Show-EfCommands