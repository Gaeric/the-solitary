# TheSolitary - cross-platform build automation
#
# Each collaborator has different local install paths for the game and Godot.
# Copy config.mk.example to config.mk and fill in your paths, then run:
#   make decomp    - decompile the game .pck into sts2_src/ for reference
#   make build     - build the mod DLL and deploy it to the game mods folder
#   make publish   - build, export the .pck via Godot headless, and deploy everything
#
# config.mk is gitignored; never commit your personal paths.
#
# How builds work:
#   TheSolitary.csproj is a generated, gitignored file. It is produced from
#   TheSolitary.csproj.template by substituting the @STS2_DIR@ / @STS2_DATA_DIR@
#   / @GODOT_EXE@ placeholders with the values from config.mk. The file is
#   generated on demand: if it does not exist, `make build` (or `make publish`)
#   creates it; if it already exists, it is left untouched. To force a
#   regeneration after changing config.mk, delete TheSolitary.csproj and rebuild.
#
#   Generating the file (instead of passing -p: on the command line) is required
#   because the Godot.NET SDK spawns nested MSBuild invocations that re-read the
#   .csproj and would ignore CLI property overrides.

# --- Load local configuration ---
# config.mk must define STS2_DIR, STS2_DATA_DIR (optional; derived below), and GODOT_EXE.
-include config.mk

# Default STS2_DATA_DIR based on the host OS if not explicitly set in config.mk.
ifndef STS2_DATA_DIR
ifeq ($(OS),Windows_NT)
STS2_DATA_DIR := $(STS2_DIR)/data_sts2_windows_x86_64
else
UNAME_S := $(shell uname -s)
ifeq ($(UNAME_S),Linux)
STS2_DATA_DIR := $(STS2_DIR)/data_sts2_linuxbsd_x86_64
else ifeq ($(UNAME_S),Darwin)
STS2_DATA_DIR := $(STS2_DIR)/data_sts2_macos_x86_64
endif
endif
endif

# --- Project constants ---
PROJECT_NAME   := TheSolitary
CSPROJ         := TheSolitary.csproj
CSPROJ_TEMPLATE := TheSolitary.csproj.template
DECOMP_DIR     := sts2_src
STS2_PCK       := $(STS2_DIR)/SlayTheSpire2.pck

# Escape characters that are special in a sed replacement (\, &, and our | delimiter).
# config.mk paths must use forward slashes (MSBuild accepts them on every OS).
esc = $(subst |,\|,$(subst &,\&,$(subst \,\\,$(1))))

# Generate TheSolitary.csproj from the template by substituting the placeholders
# with the local paths from config.mk.
gen_csproj = sed \
  -e 's|@STS2_DIR@|$(call esc,$(STS2_DIR))|g' \
  -e 's|@STS2_DATA_DIR@|$(call esc,$(STS2_DATA_DIR))|g' \
  -e 's|@GODOT_EXE@|$(call esc,$(GODOT_EXE))|g' \
  $(CSPROJ_TEMPLATE) > $(CSPROJ)

# --- Targets ---
.PHONY: all _check-config _ensure-csproj decomp build publish clean help

all: build

# Internal: generate TheSolitary.csproj from the template if it does not exist.
# If it already exists, leave it unchanged (honoring any local hand-edits).
_ensure-csproj:
	@if [ ! -f "$(CSPROJ)" ]; then \
		echo "==> [gen] Generating $(CSPROJ) from $(CSPROJ_TEMPLATE)"; \
		$(gen_csproj); \
	else \
		echo "==> [gen] $(CSPROJ) already exists, leaving it unchanged"; \
	fi

# Internal: verify that config.mk was loaded with the required variables.
_check-config:
	@test -n "$(STS2_DIR)"      || { echo "Error: STS2_DIR is not set. Copy config.mk.example to config.mk and fill in your paths." >&2; exit 1; }
	@test -n "$(STS2_DATA_DIR)" || { echo "Error: STS2_DATA_DIR is not set. Copy config.mk.example to config.mk and fill in your paths." >&2; exit 1; }
	@test -n "$(GODOT_EXE)"     || { echo "Error: GODOT_EXE is not set. Copy config.mk.example to config.mk and fill in your paths." >&2; exit 1; }

# Decompile the shipped game .pck into sts2_src/ for reference.
# Requires gdre_tools on PATH.
decomp: _check-config
	@echo "==> [decomp] Recovering game source from $(STS2_PCK)"
	gdre_tools --headless --recover="$(STS2_PCK)" --output="$(DECOMP_DIR)"
	@echo "==> [decomp] Done. Output in $(DECOMP_DIR)/"

# Build the mod and auto-deploy the DLL + manifest to the game mods folder.
build: _check-config _ensure-csproj
	@echo "==> [build] Building $(PROJECT_NAME)"
	dotnet build
	@echo "==> [build] Done. Deployed to $(STS2_DIR)/mods/$(PROJECT_NAME)/"

# Build, export the .pck via Godot headless, and deploy everything to the game mods folder.
publish: _check-config _ensure-csproj
	@echo "==> [publish] Building and exporting $(PROJECT_NAME)"
	dotnet publish -c ExportRelease
	@echo "==> [publish] Done. Deployed to $(STS2_DIR)/mods/$(PROJECT_NAME)/"

# Remove build artifacts.
clean:
	dotnet clean

# Show available targets.
help:
	@echo "TheSolitary build targets:"
	@echo "  decomp    Decompile the game .pck into $(DECOMP_DIR)/ (requires gdre_tools)"
	@echo "  build     Build the mod DLL and deploy to the game mods folder"
	@echo "  publish   Build, export .pck via Godot, and deploy to the game mods folder"
	@echo "  clean     Remove build artifacts"
	@echo "  help      Show this help message"
	@echo ""
	@echo "Configuration: copy config.mk.example to config.mk and set your local paths."
	@echo "TheSolitary.csproj is generated from TheSolitary.csproj.template; delete it to regenerate."
