SERVER_PROJECT = src/ChatApp.Server
CLIENT_PROJECT = src/ChatApp.Client
DATA_PROJECT   = src/ChatApp.Data
PID_DIR        = .pids

# ── Spustenie ──────────────────────────────────────────

.PHONY: start start-server start-client stop restart upgrade clean migrate build

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
	@echo "Aplikacia restartovana."

# ── Upgrade ────────────────────────────────────────────

upgrade: stop
	@echo "Stahujem najnovsie zmeny..."
	git pull
	@echo "Instalujem zavislosti..."
	dotnet restore
	@echo "Spustam migracie..."
	dotnet ef database update --project $(DATA_PROJECT) --startup-project $(SERVER_PROJECT)
	@$(MAKE) start
	@echo "Upgrade dokonceny."

# ── Pomocne prikazy ────────────────────────────────────

build:
	@echo "Kompilujem projekty..."
	@dotnet build --verbosity quiet

migrate:
	dotnet ef database update --project $(DATA_PROJECT) --startup-project $(SERVER_PROJECT)

clean:
	dotnet clean --verbosity quiet
	rm -rf $(PID_DIR)
	@echo "Vycistene."
