.DEFAULT_GOAL := up

COMPOSE := docker compose

.PHONY: up down build rebuild restart logs status clean

# Start all services in the background.
up:
	$(COMPOSE) up --detach

# Stop and remove the running containers.
down:
	$(COMPOSE) down

# Build all service images.
build:
	$(COMPOSE) build

# Recreate all services using freshly built images.
rebuild:
	$(COMPOSE) down
	$(COMPOSE) up --detach --build

# Restart running services without rebuilding them.
restart:
	$(COMPOSE) restart

# Follow logs from all services.
logs:
	$(COMPOSE) logs --follow

# Show the current service and container status.
status:
	$(COMPOSE) ps

# Remove containers, networks, volumes, and orphaned services.
clean:
	$(COMPOSE) down --volumes --remove-orphans
