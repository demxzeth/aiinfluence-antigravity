# AIInfluence - Antigravity

[b][size=5]AIInfluence - Antigravity[/size][/b]
[i]Direct, lightweight and high-performance Google Antigravity CLI bridge for AIInfluence.[/i]

[url=https://www.nexusmods.com/games/mountandblade2bannerlord/mods/13979][img]https://img.shields.io/badge/NexusMods-13979-blue.svg[/img][/url]

---

[b][size=4]⚡ Overview[/size][/b]

[b]AIInfluence - Antigravity[/b] connects the popular [b]AIInfluence[/b] mod directly to your local [b]Google Antigravity CLI (agy.exe)[/b].

Say goodbye to complex multi-provider configuration menus and API rate limits. All in-game dialogues, diplomacy events, lord decisions, and battle tactics are seamlessly routed through Antigravity with zero overhead, intelligent context piping, and built-in response healing.

---

[b][size=4]✨ Key Features[/size][/b]

[list]
[*] [b]Pure Antigravity Integration[/b]: Replaces clutter with a single clean, intuitive MCM settings page.
[*] [b]Two-Stage Memory Pipeline[/b]: Automatically splits requests into static world lore/rules (Stage 1) and dynamic conversation history (Stage 2 with [font=Courier New]--continue[/font]). Prevents context loss and dialogue cutting off mid-sentence.
[*] [b]Async Stdin Streaming[/b]: Prompts are passed asynchronously via standard input ([font=Courier New]--input-format text[/font]), completely bypassing the Windows 32KB command-line limit.
[*] [b]Live In-Game Response Timer[/b]: Displays real-time progress in the game chat (e.g., [color=#a855f7][Antigravity][/color] - [i]Timer: 12s[/i]) so you always know when the AI is thinking.
[*] [b]Automatic JSON & Code Fence Healing[/b]: Automatically strips markdown backticks and heals broken or truncated JSON tokens before passing them back to Bannerlord, preventing game freezes or crashes.
[*] [b]Live Model Discovery[/b]: Click [b]Fetch Models[/b] directly inside the MCM menu to query available models from your CLI and choose from the dropdown without restarting the game.
[*] [b]Fast Default Model[/b]: Pre-configured out of the box with [b]gemini-3.8-flash-low[/b] for ultra-fast, snappy dialogue responses.
[/list]

---

[b][size=4]📋 Requirements[/size][/b]

[list=1]
[*] [b]Mount & Blade II: Bannerlord[/b] (v1.2.x or newer)
[*] [url=https://www.nexusmods.com/mountandblade2bannerlord/mods/2006][b]Harmony[/b][/url] ([font=Courier New]Bannerlord.Harmony[/font])
[*] [url=https://www.nexusmods.com/mountandblade2bannerlord/mods/612][b]MCM v5[/b][/url] ([font=Courier New]Bannerlord.MBOptionScreen[/font])
[*] [url=https://www.nexusmods.com/mountandblade2bannerlord/mods/5300][b]AIInfluence[/b][/url] mod
[*] [b]Google Antigravity CLI[/b] ([font=Courier New]agy.exe[/font]) installed on your system
[/list]

---

[b][size=4]📦 Installation[/size][/b]

[b]Vortex / Mod Manager:[/b]
Install the downloaded archive using Vortex or your preferred mod manager and enable it in the load order.

[b]Manual Installation:[/b]
[list=1]
[*] Download the archive [font=Courier New]AIInfluenceAntigravity-1.1.0.zip[/font].
[*] Extract the contents into your Bannerlord [font=Courier New]Modules[/font] folder:
[font=Courier New]...\Mount & Blade II Bannerlord\Modules\AIInfluenceAntigravity[/font]
[*] In the Bannerlord Launcher, enable [b]AIInfluence - Antigravity[/b].
[*] [b]Load Order[/b]: Place it after [font=Courier New]Harmony[/font], [font=Courier New]MCM[/font], and [font=Courier New]AIInfluence[/font].
[/list]

---

[b][size=4]⚙️ In-Game Configuration (MCM)[/size][/b]

In the main menu or pause menu, open [b]Options -> Mod Options -> AIInfluence - Antigravity[/b]:

[list]
[*] [b]Enable Antigravity Interception[/b]: Toggle on/off to route requests through Antigravity.
[*] [b]Response Timer in Chat[/b]: Shows live timer notifications in game chat.
[*] [b]Antigravity CLI Path[/b]: Path to [font=Courier New]agy.exe[/font]. Leave blank to auto-detect from [font=Courier New]%LOCALAPPDATA%\agy\bin\agy.exe[/font] or system [font=Courier New]PATH[/font].
[*] [b]Model / Select Model[/b]: Choose your active model (default: [font=Courier New]gemini-3.8-flash-low[/font]).
[*] [b]Refresh Models from Endpoint[/b]: Fetches available models from your local Antigravity CLI.
[*] [b]Timeout (Seconds)[/b]: Maximum waiting time per request (60 to 600s, default 360s).
[*] [b]Test CLI Connection / Test Model[/b]: Instant diagnostic buttons to verify your CLI setup.
[/list]

---

[b][size=4]❓ Troubleshooting[/size][/b]

[list]
[*] [b]CLI not found?[/b] Ensure [font=Courier New]agy.exe[/font] is in your Windows PATH or manually paste the full path into the MCM setting field.
[*] [b]No response in chat?[/b] Click [b]Test CLI Connection[/b] and [b]Test Connection to Model[/b] in MCM to verify your authentication and endpoint status.
[*] [b]Logs:[/b] Check [font=Courier New]Modules\AIInfluenceAntigravity\logs\antigravity_log.txt[/font] for detailed diagnostics.
[/list]
