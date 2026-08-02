global using BTD_Mod_Helper.Extensions;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using System.Collections.Generic;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Api.Components;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Knowledge;
using MelonLoader;
using UnityEngine;
using Il2CppAssets;
using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Data.TrophyStore;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Player;
using Il2CppAssets.Scripts.Unity.UI_New.InGame; // HotkeyModifier - unchanged, was previously fully-qualified inline instead of `using`
using Il2CppAssets.Scripts.Utils;

// MelonInfo/MelonGame now pull from ModHelperData instead of being hardcoded, so version bumps only
// need to happen in one place, and Mod Helper's btd6.targets can keep a generated GitHub Actions
// workflow / Thunderstore listing in sync with these values.
[assembly: MelonInfo(typeof(BTD6Unlocker.BTD6UnlockerMain), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6-Epic")] // current Mod Helper mods declare the Epic Games build as a second MelonGame target

namespace BTD6Unlocker
{
    public class BTD6UnlockerMain : BloonsTD6Mod
    {
        // ModSettingHotkey fields are picked up by Mod Helper's settings system via reflection over
        // fields on the mod class, which wires them into the in-game hotkey rebind UI and persists
        // player rebinds. The original code `new`'d a fresh ModSettingHotkey every single frame
        // inside OnUpdate() - JustPressed() still worked, but a throwaway local never registers with
        // that system, so the hotkeys wouldn't show up as rebindable and any rebind would be lost the
        // next frame. Declaring them as fields fixes that; it isn't a new-API requirement, just a
        // latent bug in the original polling code that's worth fixing while touching this file.
        public static readonly ModSettingHotkey InstaHotkey = new(KeyCode.F1, HotkeyModifier.None);
        public static readonly ModSettingHotkey KnowledgeHotkey = new(KeyCode.F2, HotkeyModifier.None);
        public static readonly ModSettingHotkey MoneyAndXpHotkey = new(KeyCode.F3, HotkeyModifier.None);
        public static readonly ModSettingHotkey TrophiesHotkey = new(KeyCode.F4, HotkeyModifier.None);
        public static readonly ModSettingHotkey UnlockTrophyItemsHotkey = new(KeyCode.F5, HotkeyModifier.None);

        public override void OnApplicationStart()
        {
            // ModHelper.Msg<T>(...) is the current idiomatic logging call in Mod Helper - it tags the
            // log line with the mod type automatically. Raw MelonLogger.Msg(...) still compiles and
            // works fine, this is a style update, not a required fix.
            ModHelper.Msg<BTD6UnlockerMain>("BTD6Unlocker was loaded.");
        }

        // BloonsTD6Mod : BloonsMod : MelonMod (MelonLoader), and MelonMod.OnUpdate() is still called
        // every frame exactly as before - this override point itself is unchanged by the API bump.
        public override void OnUpdate()
        {
            // --- IMPORTANT CAVEAT, applies to every call below marked "UNVERIFIED" ---
            // Helpers.ValidBaseTowerNames() and every Btd6Player member this file calls
            // (AddInstaTower, AcquireKnowledge, HasKnowledge, AddTowerXP, GainTrophies,
            // AddTrophyStoreItem) are NOT part of BTD-Mod-Helper. They're methods on Ninja Kiwi's own
            // compiled, Il2Cpp-interop'd Assembly-CSharp - proprietary game code that isn't in this
            // repo, isn't in the open-source Mod Helper repo, and isn't something I have access to
            // decompile. I checked the current (2026) Mod Helper source directly: the convenience
            // wrappers this mod originally relied on (GameExt.AddInstaTower-style helpers) have been
            // removed from Mod Helper's public API surface entirely - there's no trace of
            // ValidBaseTowerNames, AddInstaTower, AcquireKnowledge, AddTowerXP, GainTrophies, or
            // AddTrophyStoreItem anywhere in it anymore. That confirms these calls need attention, but
            // I can't confirm the *replacement* signatures (or whether they moved to `Player`,
            // changed argument order, got renamed, etc.) without inspecting the actual current
            // Assembly-CSharp.dll from a live v50+ install - guessing at new names/signatures here
            // would be fabricated, not verified, and risks silently corrupting a real save file.
            // Below, every such call is left as the last known-working shape (pre-migration) with a
            // TODO. Before relying on this: open the current Assembly-CSharp.dll (or
            // Il2CppAssets.Scripts.Data.Player.Btd6Player, wherever it now lives) in ILSpy/dnSpy and
            // confirm each signature, or check the Mod Helper Discord for an up-to-date example mod
            // that still does inventory/currency/trophy grants.

            // f1 gives all insta monkeys
            if (InstaHotkey.JustPressed())
            {
                // TODO UNVERIFIED: Helpers.ValidBaseTowerNames() no longer exists in current Mod Helper.
                List<string> monkes = Helpers.ValidBaseTowerNames().ToList();
                foreach (string monke in monkes)
                {
                    GetAllInstaMonkes(monke);
                }
            }

            // f2 unlocks all monkey knowledge
            if (KnowledgeHotkey.JustPressed())
            {
                GameModel gameModel = Game.instance.model;
                KnowledgeModel[] models = gameModel.allKnowledge.ToArray();
                foreach (KnowledgeModel model in models)
                {
                    string m = model.name.Remove(0, 15);
                    // Game.instance.GetBtd6Player() (fluent extension-method call) is confirmed
                    // current - GameExt.GetBtd6Player(this Game game) still exists unchanged in
                    // today's Mod Helper. Only .HasKnowledge(...)/.AcquireKnowledge(...) on the
                    // returned Btd6Player are TODO UNVERIFIED raw game-side calls (see caveat above).
                    if (Game.instance.GetBtd6Player().HasKnowledge(m))
                        ModHelper.Msg<BTD6UnlockerMain>($"{m} is already unlocked");
                    else
                    {
                        ModHelper.Msg<BTD6UnlockerMain>($"unlocked {m}");
                        Game.instance.GetBtd6Player().AcquireKnowledge(m); // TODO UNVERIFIED
                    }
                }
            }

            // f3 gives monkey money and monkey xp
            if (MoneyAndXpHotkey.JustPressed())
            {
                // Game.instance.AddMonkeyMoney(...) is confirmed current -
                // GameExt.AddMonkeyMoney(this Game game, double amount) still exists unchanged.
                Game.instance.AddMonkeyMoney(1000000);

                // TODO UNVERIFIED: Helpers.ValidBaseTowerNames(), see caveat above.
                Il2CppSystem.Collections.Generic.List<string> monkes = Helpers.ValidBaseTowerNames();
                foreach (string monke in monkes)
                {
                    Game.instance.GetBtd6Player().AddTowerXP(monke, 1000000); // TODO UNVERIFIED
                    ModHelper.Msg<BTD6UnlockerMain>("Added 1000000 xp to all monkeys");
                }
            }

            // f4 gives trophies
            if (TrophiesHotkey.JustPressed())
            {
                Game.instance.GetBtd6Player().GainTrophies(10000, "event", null); // TODO UNVERIFIED
                ModHelper.Msg<BTD6UnlockerMain>("added 10000 trophies");
            }

            // f5 unlocks almost every trophy item
            if (UnlockTrophyItemsHotkey.JustPressed())
            {
                TrophyStoreItems items = GameData.Instance.trophyStoreItems;
                List<TrophyStoreItem> itemList = items.storeItems.ToList();
                foreach (TrophyStoreItem item in itemList)
                {
                    Game.instance.GetBtd6Player().AddTrophyStoreItem(item.name); // TODO UNVERIFIED
                    ModHelper.Msg<BTD6UnlockerMain>($"Unlocked {item.name}");
                }
            }
        }

        public static void GetAllInstaMonkes(string monke)
        {
            // Every AddInstaTower(...) call below is TODO UNVERIFIED - see the caveat in OnUpdate().
            Game.instance.GetBtd6Player().AddInstaTower(monke, new int[3], 50);
            for (int i = 1; i < 6; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    int[] array = new int[3];
                    array[0] = i;
                    array[1] = j;
                    Game.instance.GetBtd6Player().AddInstaTower(monke, array, 50);
                    Game.instance.GetBtd6Player().AddInstaTower(monke, new int[]
                    {
                        i,
                        0,
                        j
                    }, 50);
                }
            }
            for (int k = 1; k < 6; k++)
            {
                for (int l = 0; l < 3; l++)
                {
                    int[] array2 = new int[3];
                    array2[0] = l;
                    array2[1] = k;
                    Game.instance.GetBtd6Player().AddInstaTower(monke, array2, 50);
                    Game.instance.GetBtd6Player().AddInstaTower(monke, new int[]
                    {
                        0,
                        k,
                        l
                    }, 50);
                }
            }
            for (int m = 1; m < 6; m++)
            {
                for (int n = 0; n < 3; n++)
                {
                    Game.instance.GetBtd6Player().AddInstaTower(monke, new int[]
                    {
                        0,
                        n,
                        m
                    }, 50);
                    Game.instance.GetBtd6Player().AddInstaTower(monke, new int[]
                    {
                        n,
                        0,
                        m
                    }, 50);
                }
            }
        }
    }
}
