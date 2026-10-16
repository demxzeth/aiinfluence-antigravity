# Changelog

All notable changes to AIInfluence - Antigravity are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
versioning follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-10-17

### Changed
- Bumped mod version to v1.1.0 across all submodule definitions and assemblies.
- Upgraded project metadata, AssemblyInfo attributes, and build automation.
- Verified test suite compatibility and packaging structure for Bannerlord modules.

## [1.0.0] - 2026-10-13

### Initial Release

- **Pure Antigravity Bridge**: dedicated, lightweight sub-mod intercepting `AIInfluence.API.AIClient` exclusively for Google Antigravity CLI (`agy.exe`).
- **No Scope Bloat**: removed all 7 per-scope configuration tabs and complex multi-provider routing in favor of a single unified MCM panel and clean configuration (`Config/antigravity_settings.json`).
- **Universal Event Response Timer**:
  - Optional in-game chat ticker toggle in MCM (`Response Timer in Chat`).
  - Active across **all** game events (Dialogue, Diplomacy, Decisions, Battle Tactics, etc.).
  - Clear signature format:
    - Violet prefix: `[Antigravity]`
    - White message body: `- Request sent`, `- Timer: 12s`, `- Response received in 18s`.
- **Built-in Two-Stage Request Pipeline**:
  - Automatically splits the prompt into static world lore/rules (Stage 1) and dynamic dialogue turns/user messages (Stage 2 with `--continue`).
  - Solves context amnesia and dialogue cutting off mid-sentence ("Вайзер... Значит, с...").
- **Streaming over Standard Input**:
  - Large prompts are piped asynchronously via stdin with `--input-format text` rather than Win32 command line arguments `-p`.
  - Eliminates the 32,767-character Windows command line limit and prevents CLI crashes on large contextual prompts.
- **Robust Model Handling & Discovery**:
  - `Fetch Models` button executes `agy.exe models` and dynamically populates the model dropdown without restarting the game.
  - `Clear Fetched Models` button clears fetched cache and resets the selection.
  - Automatic typo normalization (e.g. `gemini-3.8-low` -> `gemini-3.8-flash-low`).
- **Resilient CLI Path Resolution**:
  - Supports custom paths (strips quotes, auto-resolves `.exe`).
  - Auto-discovery falls back to `%LOCALAPPDATA%\agy\bin\agy.exe` and system PATH.
  - Test CLI and Test Model buttons built into MCM for instant verification.
- **JSON & Code Fence Healing**:
  - Automatically strips markdown code blocks (````json ... ````).
  - Built-in `JsonHealer` automatically fixes unmatched braces, missing commas, and truncated response tokens.
