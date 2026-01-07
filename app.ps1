# ==========================
# Simple PowerShell App
# ==========================

# App variables
$appName = "User Registration App"
$logFile = "app.log"

# Function to write logs
function Write-Log {
    param (
        [string]$message
    )
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    "$timestamp - $message" | Out-File -Append $logFile
}

# Welcome screen
Clear-Host
Write-Host "==============================="
Write-Host " Welcome to $appName "
Write-Host "==============================="

Write-Log "Application started"

# Menu
Write-Host "1. Register User"
Write-Host "2. View Users"
Write-Host "3. Exit"
$choice = Read-Host "Select an option"

switch ($choice) {
    1 {
        $name = Read-Host "Enter username"
        $email = Read-Host "Enter email"

        "$name, $email" | Out-File -Append users.txt
        Write-Host "User registered successfully!"

        Write-Log "User registered: $name"
    }
    2 {
        if (Test-Path "users.txt") {
            Write-Host "Registered Users:"
            Get-Content users.txt
        } else {
            Write-Host "No users found."
        }
    }
    3 {
        Write-Host "Exiting application..."
        Write-Log "Application exited"
        exit
    }
    default {
        Write-Host "Invalid choice"
        Write-Log "Invalid menu selection"
    }
}

