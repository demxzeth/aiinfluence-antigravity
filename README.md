# AIInfluence - Antigravity

Lightweight, high-performance bridge between **Mount & Blade II: Bannerlord** (via **AIInfluence**) and **Google Antigravity CLI** (`agy.exe`).

---

## Highlights

- **Pure Antigravity**: Zero multi-provider clutter. No need to manage 7 separate scopes. All dialogues, diplomacy events, decisions, and battle tactics route through your local Antigravity installation.
- **Universal Response Timer**:
  - Optional toggle in MCM (`Response Timer in Chat`).
  - Emits in-game chat notifications formatted as `[Antigravity] - Timer: **s` (with violet prefix and clean white timer text) for all events.
- **Built-in Two-Stage Request Pipeline**:
  - Automatically breaks prompts into static game rules/lore and dialogue history with `--continue`.
  - Fixes dialogue cutoffs and memory loss.
- **Async Standard Input Streaming**:
  - Bypasses Windows command-line limits (32KB) by feeding prompt text directly to stdin (`--input-format text`).
- **JSON & Code Fence Healing**:
  - Strips markdown code fences (```json ... ```) and repairs unclosed braces/brackets so Bannerlord never receives corrupted JSON.
- **Model Discovery & Live Refresh**:
  - Fetches models directly via `agy.exe models`.
  - Live MCM dropdown updates without game restarts.
  - Auto-normalizes common model name typos.

---

## Requirements

1. **Mount & Blade II: Bannerlord** (v1.2+)
2. **Harmony** (`Bannerlord.Harmony`)
3. **MBOptionScreen v5** (`Bannerlord.MBOptionScreen`)
4. **AIInfluence** mod
5. **Google Antigravity CLI** installed locally (`agy.exe`).

---

## Installation

1. Copy the `AIInfluenceAntigravity` folder into your Bannerlord `Modules/` directory:
   ```
   Mount & Blade II Bannerlord\Modules\AIInfluenceAntigravity
   ```
2. Enable `AIInfluence - Antigravity` in the Bannerlord launcher (load after `AIInfluence`, `Harmony`, and `MCM`).
3. Launch the game and configure your settings in **Options -> Mod Options -> AIInfluence - Antigravity**.

---

## Configuration (MCM)

- **Master Switch**: Enable / disable Antigravity interception globally.
- **Response Timer in Chat**: Shows live timers (`[Antigravity] - Timer: 10s`) in the game chat.
- **Antigravity CLI Path**: Custom path to `agy.exe` (leave empty for auto-detection).
- **Model**: Model name to use (default: `gemini-3.8-flash-low`).
- **Select Model**: Dropdown of models discovered via `Fetch Models`.
- **Fetch Models**: Queries your local `agy.exe models` list.
- **Clear Fetched Models**: Clears cached models and resets dropdown.
- **Timeout (seconds)**: Execution timeout (60 to 600 seconds).
- **Test CLI / Test Model**: In-game diagnosis buttons to verify your CLI setup.

---

## Building from Source

Build the project using .NET SDK:
```cmd
dotnet build src\AIInfluenceAntigravity.csproj -c Release
```

Run test suite:
```cmd
dotnet test tests\AIInfluenceAntigravity.Tests.csproj -c Release
```
