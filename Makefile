SERVER_PROJECT = src/ChatApp.Server
CLIENT_PROJECT = src/ChatApp.Client
DATA_PROJECT   = src/ChatApp.Data
PID_DIR        = .pids

.PHONY: start start-server start-client stop restart upgrade clean migrate build check-deps restore

# ── Kontrola zavislosti ───────────────────────────────────────

check-deps:
	@echo "Kontrolujem zavislosti..."
	@command -v dotnet >/dev/null 2>&1 || { echo "CHYBA: dotnet SDK nie je nainstalovany. https://dotnet.microsoft.com/download"; exit 1; }
	@dotnet --version | grep -q "^10\." || { echo "UPOZORNENIE: Ocakavany .NET 10 SDK, najdeny: $$(dotnet --version)"; }
	@dotnet tool list --global 2>/dev/null | grep -q "dotnet-ef" || { echo "CHYBA: dotnet-ef nie je nainstalovany. Spustite: dotnet tool install --global dotnet-ef"; exit 1; }
	@command -v mysql >/dev/null 2>&1 || echo "UPOZORNENIE: mysql klient nenajdeny. Uistite sa, ze MySQL server bezi."
	@command -v git >/dev/null 2>&1 || { echo "CHYBA: git nie je nainstalovany."; exit 1; }
	@echo "Vsetky zavislosti OK."

# ── Restore + Build ──────────────────────────────────────────

restore: check-deps
	@echo "Instalujem NuGet balicky..."
	@dotnet restore --verbosity quiet

build: restore
	@echo "Kompilujem projekty..."
	@dotnet build --no-restore --verbosity quiet

# ── Spustenie ──────────────────────────────────────────

start: build start-server start-client
	@echo "Aplikacia bezi. Server + klient spustene."

start-server:
	@mkdir -p $(PID_DIR)
	@dotnet run --project $(SERVER_PROJECT) --no-build & echo $$! > $(PID_DIR)/server.pid
	@echo "Server spusteny (PID: $$(cat $(PID_DIR)/server.pid))"
	@sleep 2

start-client:
	@mkdir -p $(PID_DIR)
	@dotnet run --project $(CLIENT_PROJECT) --no-build & echo $$! > $(PID_DIR)/client.pid
	@echo "Klient spusteny (PID: $$(cat $(PID_DIR)/client.pid))"

# ── Zastavenie ─────────────────────────────────────────

stop:
	@if [ -f $(PID_DIR)/client.pid ]; then \
		kill $$(cat $(PID_DIR)/client.pid) 2>/dev/null && echo "Klient zastaveny." || echo "Klient uz nebezi."; \
		rm -f $(PID_DIR)/client.pid; \
	fi
	@if [ -f $(PID_DIR)/server.pid ]; then \
		kill $$(cat $(PID_DIR)/server.pid) 2>/dev/null && echo "Server zastaveny." || echo "Server uz nebezi."; \
		rm -f $(PID_DIR)/server.pid; \
	fi
	@echo "Aplikacia zastavena."

# ── Restart ────────────────────────────────────────────

restart: stop start

# ── Upgrade ────────────────────────────────────────────

upgrade: stop check-deps
	@echo "Stahujem najnovsie zmeny..."
	git pull
	@$(MAKE) restore
	@echo "Spustam migracie..."
	dotnet ef database update --project $(DATA_PROJECT) --startup-project $(SERVER_PROJECT)
	@$(MAKE) start
	@echo "Upgrade dokonceny."

# ── Pomocne prikazy ────────────────────────────────────

migrate: check-deps
	dotnet ef database update --project $(DATA_PROJECT) --startup-project $(SERVER_PROJECT)

clean:
	dotnet clean --verbosity quiet
	rm -rf $(PID_DIR)
	@echo "Vycistene."
