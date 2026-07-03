param(
    [Parameter(Position=0)]
    [ValidateSet("start", "stop", "restart", "upgrade", "migrate", "build", "clean", "check")]
    [string]$Command = "start"
)

$ServerProject = "src/ChatApp.Server"
$ClientProject = "src/ChatApp.Client"
$DataProject   = "src/ChatApp.Data"

# ── Kontrola zavislosti ───────────────────────────────────────

function Test-Dependencies {
    Write-Host "Kontrolujem zavislosti..." -ForegroundColor Cyan
    $ok = $true

    # dotnet SDK
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        Write-Host "  CHYBA: dotnet SDK nie je nainstalovany." -ForegroundColor Red
        Write-Host "         https://dotnet.microsoft.com/download"
        $ok = $false
    } else {
        $ver = dotnet --version
        Write-Host "  dotnet SDK: $ver" -ForegroundColor Green
        if (-not $ver.StartsWith("10.")) {
            Write-Host "  UPOZORNENIE: Ocakavany .NET 10 SDK" -ForegroundColor Yellow
        }
    }

    # dotnet-ef
    $efInstalled = dotnet tool list --global 2>$null | Select-String "dotnet-ef"
    if (-not $efInstalled) {
        Write-Host "  CHYBA: dotnet-ef nie je nainstalovany." -ForegroundColor Red
        Write-Host "         Spustite: dotnet tool install --global dotnet-ef"
        $ok = $false
    } else {
        Write-Host "  dotnet-ef: nainstalovany" -ForegroundColor Green
    }

    # git
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        Write-Host "  CHYBA: git nie je nainstalovany." -ForegroundColor Red
        $ok = $false
    } else {
        Write-Host "  git: $(git --version)" -ForegroundColor Green
    }

    # mysql
    if (-not (Get-Command mysql -ErrorAction SilentlyContinue)) {
        Write-Host "  UPOZORNENIE: mysql klient nenajdeny. Uistite sa, ze MySQL server bezi." -ForegroundColor Yellow
    } else {
        Write-Host "  mysql: nainstalovany" -ForegroundColor Green
    }

    if (-not $ok) {
        Write-Host "`nNiektore zavislosti chybaju. Nainštalujte ich a skuste znova." -ForegroundColor Red
        exit 1
    }

    Write-Host "Vsetky zavislosti OK.`n" -ForegroundColor Green
}

# ── Restore + Build ──────────────────────────────────────────

function Restore-Packages {
    Test-Dependencies
    Write-Host "Instalujem NuGet balicky..." -ForegroundColor Cyan
    dotnet restore --verbosity quiet
    if ($LASTEXITCODE -ne 0) { Write-Host "CHYBA: dotnet restore zlyhalo." -ForegroundColor Red; exit 1 }
}

function Build-App {
    Restore-Packages
    New-Item -ItemType Directory -Path .pids -Force | Out-Null
    Write-Host "Kompilujem projekty..." -ForegroundColor Cyan
    dotnet build --no-restore --verbosity quiet
    if ($LASTEXITCODE -ne 0) { Write-Host "CHYBA: Build zlyhal." -ForegroundColor Red; exit 1 }
}

# ── Spustenie ──────────────────────────────────────────

function Start-App {
    Build-App

    Write-Host "Spustam server..." -ForegroundColor Cyan
    $server = Start-Process dotnet -ArgumentList "run", "--project", $ServerProject, "--no-build" -PassThru -WindowStyle Minimized
    $server.Id | Out-File .pids/server.pid -Force
    Write-Host "Server spusteny (PID: $($server.Id))"

    Start-Sleep -Seconds 2

    Write-Host "Spustam klienta..." -ForegroundColor Cyan
    $client = Start-Process dotnet -ArgumentList "run", "--project", $ClientProject, "--no-build" -PassThru
    $client.Id | Out-File .pids/client.pid -Force
    Write-Host "Klient spusteny (PID: $($client.Id))"

    Write-Host "`nAplikacia bezi." -ForegroundColor Green
}

# ── Zastavenie ─────────────────────────────────────────

function Stop-App {
    if (Test-Path .pids/client.pid) {
        $clientPid = Get-Content .pids/client.pid
        try {
            # "dotnet run" spusti skutocny proces ako child - treba zabit cely strom (/T)
            taskkill /PID $clientPid /T /F 2>$null | Out-Null
            Write-Host "Klient zastaveny."
        } catch {
            Write-Host "Klient uz nebezi."
        }
        Remove-Item .pids/client.pid -Force
    }

    if (Test-Path .pids/server.pid) {
        $serverPid = Get-Content .pids/server.pid
        try {
            taskkill /PID $serverPid /T /F 2>$null | Out-Null
            Write-Host "Server zastaveny."
        } catch {
            Write-Host "Server uz nebezi."
        }
        Remove-Item .pids/server.pid -Force
    }

    Write-Host "Aplikacia zastavena." -ForegroundColor Yellow
}

# ── Restart ────────────────────────────────────────────

function Restart-App {
    Stop-App
    Start-App
}

# ── Upgrade ────────────────────────────────────────────

function Update-App {
    Stop-App
    Test-Dependencies

    Write-Host "Stahujem najnovsie zmeny..." -ForegroundColor Cyan
    git pull

    Restore-Packages

    Write-Host "Spustam migracie..." -ForegroundColor Cyan
    dotnet ef database update --project $DataProject --startup-project $ServerProject

    Start-App
    Write-Host "Upgrade dokonceny." -ForegroundColor Green
}

# ── Pomocne prikazy ────────────────────────────────────

function Invoke-Migrate {
    Test-Dependencies
    dotnet ef database update --project $DataProject --startup-project $ServerProject
}

function Clean-App {
    dotnet clean --verbosity quiet
    if (Test-Path .pids) { Remove-Item .pids -Recurse -Force }
    Write-Host "Vycistene." -ForegroundColor Yellow
}

# ── Hlavny switch ──────────────────────────────────────

switch ($Command) {
    "start"   { Start-App }
    "stop"    { Stop-App }
    "restart" { Restart-App }
    "upgrade" { Update-App }
    "migrate" { Invoke-Migrate }
    "build"   { Build-App }
    "clean"   { Clean-App }
    "check"   { Test-Dependencies }
}
