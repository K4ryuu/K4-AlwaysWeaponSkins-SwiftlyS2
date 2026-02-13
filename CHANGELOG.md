# Changelog

All notable changes to K4-AlwaysWeaponSkins will be documented in this file.

## [1.0.4] - 2026-02-13

### Fixed

- **CRITICAL**: Fixed entity validity craissuesh in weapon replacement system
  - Added entity validation check before `AddEntityIOEvent()` call in scheduled callbacks
  - Prevents `System.InvalidOperationException: The entity instance is no longer valid`
  - **Root Cause**: Weapon entities could be destroyed between pickup detection and scheduled replacement
  - **Impact**: Plugin would give an error when weapons were dropped/destroyed before replacement callback executed
  - Affects scenarios: rapid weapon pickups, round transitions, weapon drops during replacement

## [1.0.3] - 2026-02-11

### Fixed

- **CRITICAL**: Fixed configuration binding bug in `Load()` method
  - Changed `.BindConfiguration(ConfigFileName)` to `.BindConfiguration(ConfigSection)`
  - This bug caused all config values to use hardcoded defaults instead of reading from `k4-alwaysweaponskins.jsonc`
  - **Impact**: Plugin configuration was completely non-functional

- Fixed USP-S/P2000 weapon substitution issue
  - Use `ItemDefinitionIndex` instead of `DesignerName` for weapon identification
  - Resolves issue where CT players with USP-S loadout received P2000 instead
  - `Core.Helpers.GetClassnameByDefinitionIndex()` returns correct weapon classname based on item ID
  - Also fixes potential issues with M4A4/M4A1-S and other alternative weapons

## [1.0.2] - 2025-12-12

-- 2025.12.12 - 1.0.2

- refactor: Migrate config system to IOptionsMonitor pattern for hot-reload support
- refactor: Use static Config property with CurrentValue accessor
- refactor: Rename config file from config.jsonc to k4-alwaysweaponskins.jsonc
- chore: Add Microsoft.Extensions.DependencyInjection and Options dependencies

-- 2025.11.28 - 1.0.1

- fix: Simplify null check for player validity in team change logic
- chore: Changed fix version to use always the lastest
- chore: Remove log information for plugin loading
- chore: Add CODEOWNERS file to define repository ownership
