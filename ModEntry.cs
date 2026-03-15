using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace ShowYourCard;

[ModInitializer("Initialize")]
public static class ModEntry
{
    private static Harmony? _harmony;

    public static void Initialize()
    {
        if (_harmony != null)
        {
            return;
        }

        ShowYourCardHotkey.Initialize();
        _harmony = new Harmony("com.example.sts2.show_your_card");
        PatchHook(nameof(Hook.BeforeCombatStart), nameof(HookPatches.BeforeCombatStartPostfix));
        PatchHook(nameof(Hook.AfterCombatEnd), nameof(HookPatches.AfterCombatEndPostfix));
        PatchHook(nameof(Hook.AfterPlayerTurnStart), nameof(HookPatches.AfterPlayerTurnStartPostfix));
        PatchHook(nameof(Hook.AfterCardDrawn), nameof(HookPatches.RefreshCombatPostfix));
        PatchHook(nameof(Hook.AfterCardChangedPiles), nameof(HookPatches.AfterCardChangedPilesPostfix));
        PatchHook(nameof(Hook.AfterCardDiscarded), nameof(HookPatches.RefreshCombatPostfix));
        PatchHook(nameof(Hook.AfterCardExhausted), nameof(HookPatches.RefreshCombatPostfix));
        PatchHook(nameof(Hook.AfterCardPlayed), nameof(HookPatches.RefreshCombatPostfix));
        PatchHook(nameof(Hook.AfterCardRetained), nameof(HookPatches.RefreshCombatPostfix));
        PatchHook(nameof(Hook.AfterHandEmptied), nameof(HookPatches.RefreshCombatPostfix));
        PatchMethod(typeof(NInputManager), "Init", nameof(GamePatches.InputManagerInitPostfix));
        PatchMethod(typeof(NInputManager), nameof(NInputManager.ResetToDefaults), nameof(GamePatches.InputManagerResetToDefaultsPostfix));
        PatchMethod(typeof(NInputSettingsEntry), nameof(NInputSettingsEntry._Ready), nameof(GamePatches.InputSettingsEntryReadyPostfix));

        ShowYourCardOverlay.EnsureCreated();
        Log.Info("ShowYourCard initialized");
    }

    private static void PatchHook(string hookName, string postfixName)
    {
        PatchMethod(typeof(Hook), hookName, postfixName, typeof(HookPatches));
    }

    private static void PatchMethod(Type originalType, string methodName, string postfixName, Type? patchType = null)
    {
        MethodInfo original = AccessTools.Method(originalType, methodName)
            ?? throw new MissingMethodException(originalType.FullName, methodName);
        Type resolvedPatchType = patchType ?? typeof(GamePatches);
        MethodInfo postfix = AccessTools.Method(resolvedPatchType, postfixName)
            ?? throw new MissingMethodException(resolvedPatchType.FullName, postfixName);

        _harmony!.Patch(original, postfix: new HarmonyMethod(postfix));
    }
}

internal static class HookPatches
{
    public static void BeforeCombatStartPostfix(object[] __args)
    {
        object? combatState = __args.Length > 1 ? __args[1] : null;
        TeammateHandService.BeginCombat(combatState);
        ShowYourCardOverlay.EnsureCreated();
    }

    public static void AfterCombatEndPostfix(object[] __args)
    {
        TeammateHandService.EndCombat();
    }

    public static void AfterPlayerTurnStartPostfix(object[] __args)
    {
        object? combatState = __args.Length > 0 ? __args[0] : null;
        object? player = __args.Length > 2 ? __args[2] : null;
        TeammateHandService.NoteActivePlayer(combatState, player);
        ShowYourCardOverlay.EnsureCreated();
    }

    public static void RefreshCombatPostfix(object[] __args)
    {
        object? combatState = __args.Length > 0 ? __args[0] : null;
        TeammateHandService.RefreshFromCombat(combatState);
    }

    public static void AfterCardChangedPilesPostfix(object[] __args)
    {
        object? combatState = __args.Length > 1 ? __args[1] : null;
        TeammateHandService.RefreshFromCombat(combatState);
    }
}

internal static class GamePatches
{
    public static void InputManagerInitPostfix(NInputManager __instance)
    {
        ShowYourCardHotkey.EnsureKeyboardBinding(__instance);
    }

    public static void InputManagerResetToDefaultsPostfix(NInputManager __instance)
    {
        ShowYourCardHotkey.EnsureKeyboardBinding(__instance, emitRebound: true);
    }

    public static void InputSettingsEntryReadyPostfix(NInputSettingsEntry __instance)
    {
        ShowYourCardHotkey.OverrideSettingsEntryLabel(__instance);
    }
}
