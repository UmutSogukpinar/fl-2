.DEFAULT_GOAL := up

ROOT_DIR := $(abspath $(dir $(lastword $(MAKEFILE_LIST)))/../..)
COMPOSE := docker compose --project-directory "$(ROOT_DIR)" --file "$(ROOT_DIR)/docker-compose.yml"

.PHONY: up down build rebuild restart logs status clean

up:
	$(COMPOSE) up --detach

down:
	$(COMPOSE) down

build:
	$(COMPOSE) build

rebuild:
	$(COMPOSE) down
	$(COMPOSE) up --detach --build

restart:
	$(COMPOSE) restart

logs:
	$(COMPOSE) logs --follow

status:
	$(COMPOSE) ps

clean:
	$(COMPOSE) down --volumes --remove-orphans
