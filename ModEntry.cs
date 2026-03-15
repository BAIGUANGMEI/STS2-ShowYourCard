using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

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

        ShowYourCardOverlay.EnsureCreated();
        Log.Info("ShowYourCard initialized");
    }

    private static void PatchHook(string hookName, string postfixName)
    {
        MethodInfo original = AccessTools.Method(typeof(Hook), hookName)
            ?? throw new MissingMethodException(typeof(Hook).FullName, hookName);
        MethodInfo postfix = AccessTools.Method(typeof(HookPatches), postfixName)
            ?? throw new MissingMethodException(typeof(HookPatches).FullName, postfixName);

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
