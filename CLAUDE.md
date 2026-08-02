# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

BTD6Unlocker is a [MelonLoader](https://melonwiki.xyz/) mod for the Il2Cpp build of *Bloons TD 6*, built on top of [BTD-Mod-Helper](https://github.com/gurrenm3/BTD-Mod-Helper). It's a single-purpose "cheat" mod: on hotkey press it grants insta-monkeys, unlocks monkey knowledge, adds monkey money/XP, adds trophies, and unlocks trophy store items.

The entire mod logic lives in one file, `Main.cs`, in the `BTD6Unlocker.BTD6UnlockerMain` class (extends `BloonsTD6Mod` from BTD-Mod-Helper).

## Build

This is a **Windows-only, .NET Framework 4.8 class library** built with MSBuild/Visual Studio. It cannot be built on macOS/Linux without the referenced assemblies.

- `BTD6Unlocker.csproj` references game/mod-loader DLLs (MelonLoader, Il2CppInterop, Il2Cpp-interop'd `Assembly-CSharp`, `Btd6ModHelper.dll`, etc.) via **hardcoded absolute `HintPath`s** pointing into a local Steam install: `D:\SteamLibrary\steamapps\common\BloonsTD6\...`. Building requires BTD6 + MelonLoader + BTD-Mod-Helper installed at that exact path (or the `HintPath`s edited to match the local machine).
- The post-build event (`BTD6Unlocker.csproj`) copies the built DLL straight into `D:\SteamLibrary\steamapps\common\BloonsTD6\Mods` so it's picked up by MelonLoader on next game launch. This will fail (harmlessly, for MSBuild's purposes) if that path doesn't exist.
- Build via Visual Studio (open `BTD6Unlocker.sln`) or `msbuild BTD6Unlocker.sln` from a Developer Command Prompt.
- No test project, no lint/format config, no CI.

## Architecture notes

- `OnUpdate()` polls hotkeys every frame via `ModSettingHotkey(...).JustPressed()` — there's no event-driven input handling. Each feature is a straight `if (hotkey.JustPressed()) { ... }` block:
  - F1 — insta-monkeys (`GetAllInstaMonkes`, iterates every valid base tower name and every upgrade-path combination)
  - F2 — unlock all monkey knowledge
  - F3 — +1,000,000 monkey money and +1,000,000 XP per tower
  - F4 — +10,000 trophies
  - F5 — unlock all trophy store items
- Game state is reached through `Game.instance` and the `GameExt` / `Btd6Player` helpers from BTD-Mod-Helper (e.g. `GameExt.GetBtd6Player(Game.instance)`), not by touching Il2Cpp types directly where a helper exists.
- `Helpers.ValidBaseTowerNames()` (from BTD-Mod-Helper) is the source of truth for tower name strings used across features.
- Logging goes through `MelonLogger.Msg(...)`, matching MelonLoader mod conventions.
- Because this targets Il2Cpp-interop'd game types, collections are frequently `Il2CppSystem.Collections.Generic.List<T>` rather than the BCL `System.Collections.Generic.List<T>` — watch for `.ToList()`/`.ToArray()` conversions when adding new code that needs LINQ or BCL collection APIs.
