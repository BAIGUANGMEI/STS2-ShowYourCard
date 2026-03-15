using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace ShowYourCard;

internal static class ShowYourCardHotkey
{
    public static readonly StringName ToggleOverlayAction = "show_your_card.toggle_overlay";

    private const Key DefaultToggleKey = Key.H;
    private const string PlaceholderInputTitleKey = "viewDeck";
    private const string HotkeyLabelKey = "HOTKEY_TOGGLE";

    private static Dictionary<string, string>? _locStrings;

    public static void Initialize()
    {
        EnsureInputActionExists();
        RegisterRemappableKeyboardInput();
        RegisterSettingsEntryPlaceholder();
    }

    public static void EnsureKeyboardBinding(NInputManager inputManager, bool emitRebound = false)
    {
        if (GetKeyboardInputMap(inputManager) is not Dictionary<StringName, Key> keyboardInputMap)
        {
            return;
        }

        if (!keyboardInputMap.ContainsKey(ToggleOverlayAction))
        {
            keyboardInputMap[ToggleOverlayAction] = DefaultToggleKey;
            if (emitRebound)
            {
                inputManager.EmitSignal(NInputManager.SignalName.InputRebound);
            }
        }
    }

    public static void OverrideSettingsEntryLabel(NInputSettingsEntry entry)
    {
        if (entry.InputName != ToggleOverlayAction)
        {
            return;
        }

        MegaRichTextLabel? inputLabel = entry.GetNodeOrNull<MegaRichTextLabel>("%InputLabel");
        if (inputLabel != null)
        {
            inputLabel.Text = L(HotkeyLabelKey);
        }
    }

    private static void EnsureInputActionExists()
    {
        if (!InputMap.HasAction(ToggleOverlayAction))
        {
            InputMap.AddAction(ToggleOverlayAction);
        }
    }

    private static void RegisterRemappableKeyboardInput()
    {
        if (typeof(NInputManager).GetField(nameof(NInputManager.remappableKeyboardInputs), BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null) is not IList<StringName> keyboardInputs)
        {
            return;
        }

        if (!keyboardInputs.Contains(ToggleOverlayAction))
        {
            keyboardInputs.Add(ToggleOverlayAction);
        }
    }

    private static void RegisterSettingsEntryPlaceholder()
    {
        FieldInfo? field = typeof(NInputSettingsEntry).GetField("_commandToLocTitle", BindingFlags.NonPublic | BindingFlags.Static);
        if (field?.GetValue(null) is not Dictionary<StringName, string> commandTitles)
        {
            return;
        }

        if (!commandTitles.ContainsKey(ToggleOverlayAction))
        {
            commandTitles[ToggleOverlayAction] = PlaceholderInputTitleKey;
        }
    }

    private static Dictionary<StringName, Key>? GetKeyboardInputMap(NInputManager inputManager)
    {
        FieldInfo? field = typeof(NInputManager).GetField("_keyboardInputMap", BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(inputManager) as Dictionary<StringName, Key>;
    }

    private static string L(string key)
    {
        return LoadLocStrings().TryGetValue(key, out string? value) ? value : key;
    }

    private static Dictionary<string, string> LoadLocStrings()
    {
        if (_locStrings != null)
        {
            return _locStrings;
        }

        string lang = ResolveGameLanguage();
        string path = $"res://localization/{lang}/show_your_card.json";
        if (!Godot.FileAccess.FileExists(path) && lang != "eng")
        {
            path = "res://localization/eng/show_your_card.json";
        }

        if (!Godot.FileAccess.FileExists(path))
        {
            _locStrings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return _locStrings;
        }

        using Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        string json = file.GetAsText();
        _locStrings = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        return _locStrings;
    }

    private static string ResolveGameLanguage()
    {
        try
        {
            Type? locMgr = Type.GetType("MegaCrit.Sts2.Core.Localization.LocManager, sts2", throwOnError: false);
            object? inst = locMgr?.GetProperty("Instance",
                BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            string? lang = inst?.GetType().GetProperty("Language")?.GetValue(inst) as string;
            if (!string.IsNullOrEmpty(lang))
            {
                return lang;
            }
        }
        catch
        {
            // Fall back to OS language.
        }

        return OS.GetLocaleLanguage().StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? "zhs" : "eng";
    }
}
