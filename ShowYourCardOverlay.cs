using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace ShowYourCard;

public sealed partial class ShowYourCardOverlay : CanvasLayer
{
    private static readonly Color White = new("FFFFFF");
    private static readonly Color Dim = new("98A2B3");
    private static readonly Color Border = new("314158");
    private static readonly Color Bg = new("08111DBA");
    private static readonly Color ActiveBg = new("12263FD6");
    private static readonly Color Attack = new("B84242");
    private static readonly Color Skill = new("245E9F");
    private static readonly Color Power = new("8B4CD8");
    private static readonly Color Other = new("475467");
    private static readonly Color UpgradeBorder = new("FACC15");

    private static ShowYourCardOverlay? _instance;
    private static Dictionary<string, string>? _locStrings;

    private Control? _root;
    private VBoxContainer? _players;
    private Label? _emptyLabel;
    private Button? _toggleBtn;
    private bool _expanded = true;
    private bool _dragging;
    private Vector2 _dragOffset;
    private OverlayState? _lastState;

    private static Dictionary<string, string> LocStrings => _locStrings ??= LoadLocStrings();

    public static void EnsureCreated()
    {
        if (IsInstanceValid(_instance))
        {
            return;
        }

        if (Engine.GetMainLoop() is not SceneTree tree || tree.Root == null)
        {
            return;
        }

        _instance = new ShowYourCardOverlay();
        tree.Root.CallDeferred(Node.MethodName.AddChild, _instance);
    }

    public override void _EnterTree()
    {
        Layer = 100;
        Name = nameof(ShowYourCardOverlay);
        TeammateHandService.Changed += OnChanged;
    }

    public override void _ExitTree()
    {
        TeammateHandService.Changed -= OnChanged;
        if (ReferenceEquals(_instance, this))
        {
            _instance = null;
        }
    }

    public override void _Ready()
    {
        PanelContainer root = new()
        {
            Name = "Root",
            Position = new Vector2(20, 100),
            CustomMinimumSize = new Vector2(360, 0),
            Size = new Vector2(360, 0),
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        root.GuiInput += OnGuiInput;
        root.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = Bg,
            BorderColor = Border,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8
        });
        _root = root;

        MarginContainer pad = new();
        pad.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        pad.AddThemeConstantOverride("margin_left", 10);
        pad.AddThemeConstantOverride("margin_right", 10);
        pad.AddThemeConstantOverride("margin_top", 8);
        pad.AddThemeConstantOverride("margin_bottom", 8);

        VBoxContainer content = new()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        content.AddThemeConstantOverride("separation", 6);
        content.AddChild(BuildHeader());

        _players = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _players.AddThemeConstantOverride("separation", 6);
        content.AddChild(_players);

        _emptyLabel = MakeLabel(L("EMPTY"), 12, Dim);
        _emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(_emptyLabel);

        pad.AddChild(content);
        root.AddChild(pad);
        AddChild(root);

        ApplyState(TeammateHandService.BuildOverlayState());
    }

    private static Dictionary<string, string> LoadLocStrings()
    {
        string lang = ResolveGameLanguage();
        string path = $"res://localization/{lang}/show_your_card.json";
        if (!Godot.FileAccess.FileExists(path) && lang != "eng")
        {
            path = "res://localization/eng/show_your_card.json";
        }

        if (!Godot.FileAccess.FileExists(path))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        using Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        string json = file.GetAsText();
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
               ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private static string ResolveGameLanguage()
    {
        try
        {
            Type? locMgr = Type.GetType("MegaCrit.Sts2.Core.Localization.LocManager, sts2", throwOnError: false);
            object? inst = locMgr?.GetProperty("Instance",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null);
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

        string locale = OS.GetLocaleLanguage().ToLowerInvariant();
        if (locale.StartsWith("zh"))
        {
            return "zhs";
        }

        return "eng";
    }

    private static string L(string key)
    {
        return LocStrings.TryGetValue(key, out string? value) ? value : key;
    }

    private Control BuildHeader()
    {
        HBoxContainer row = new()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        Label title = MakeLabel(L("TITLE"), 15, White);
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(title);

        _toggleBtn = new Button
        {
            Text = "\u25bc",
            CustomMinimumSize = new Vector2(24, 24),
            FocusMode = Control.FocusModeEnum.None,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _toggleBtn.AddThemeFontSizeOverride("font_size", 12);
        _toggleBtn.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color("FFFFFF12"), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
        _toggleBtn.AddThemeStyleboxOverride("hover", new StyleBoxFlat { BgColor = new Color("FFFFFF1E"), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });
        _toggleBtn.AddThemeColorOverride("font_color", White);
        _toggleBtn.Pressed += OnToggle;
        row.AddChild(_toggleBtn);

        return row;
    }

    private Control BuildPlayerSection(PlayerHandSnapshot snapshot)
    {
        PanelContainer panel = new()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = snapshot.IsActive ? ActiveBg : new Color("101828C8"),
            BorderColor = snapshot.IsActive ? new Color("60A5FA") : Border,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6
        });

        MarginContainer pad = new();
        pad.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        pad.AddThemeConstantOverride("margin_left", 8);
        pad.AddThemeConstantOverride("margin_right", 8);
        pad.AddThemeConstantOverride("margin_top", 8);
        pad.AddThemeConstantOverride("margin_bottom", 8);

        VBoxContainer body = new()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        body.AddThemeConstantOverride("separation", 4);
        body.AddChild(BuildPlayerHeader(snapshot));

        if (_expanded)
        {
            VBoxContainer cards = new()
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            cards.AddThemeConstantOverride("separation", 3);

            if (snapshot.Cards.Count == 0)
            {
                cards.AddChild(MakeLabel(L("NO_CARDS"), 11, Dim));
            }
            else
            {
                for (int i = 0; i < snapshot.Cards.Count; i++)
                {
                    cards.AddChild(BuildCardRow(snapshot.Cards[i]));
                }
            }

            body.AddChild(cards);
        }

        pad.AddChild(body);
        panel.AddChild(pad);
        return panel;
    }

    private Control BuildPlayerHeader(PlayerHandSnapshot snapshot)
    {
        HBoxContainer row = new()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 8);

        row.AddChild(BuildPortrait(snapshot));

        VBoxContainer names = new()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        names.AddThemeConstantOverride("separation", 0);

        Label displayName = MakeLabel(snapshot.DisplayName, 13, White);
        displayName.ClipText = true;
        displayName.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        names.AddChild(displayName);

        string subtitle = $"{snapshot.CharacterName}  {L("HAND")}: {snapshot.Cards.Count}";
        if (snapshot.IsActive)
        {
            subtitle += $"  {L("ACTIVE")}";
        }

        Label detail = MakeLabel(subtitle, 11, Dim);
        detail.ClipText = true;
        detail.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        names.AddChild(detail);

        row.AddChild(names);
        return row;
    }

    private static Control BuildPortrait(PlayerHandSnapshot snapshot)
    {
        if (snapshot.PortraitTexture != null)
        {
            return new TextureRect
            {
                Texture = snapshot.PortraitTexture,
                CustomMinimumSize = new Vector2(28, 28),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
            };
        }

        PanelContainer fallback = new()
        {
            CustomMinimumSize = new Vector2(28, 28)
        };
        fallback.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color("334155"),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        });

        CenterContainer center = new();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        center.AddChild(MakeLabel(snapshot.CharacterName[..Math.Min(1, snapshot.CharacterName.Length)], 12, White));
        fallback.AddChild(center);
        return fallback;
    }

    private Control BuildCardRow(CardSnapshot card)
    {
        PanelContainer row = new()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        row.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = GetCardColor(card.Type),
            BorderColor = card.IsUpgraded ? UpgradeBorder : Colors.Transparent,
            BorderWidthLeft = card.IsUpgraded ? 1 : 0,
            BorderWidthTop = card.IsUpgraded ? 1 : 0,
            BorderWidthRight = card.IsUpgraded ? 1 : 0,
            BorderWidthBottom = card.IsUpgraded ? 1 : 0,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        });

        MarginContainer pad = new();
        pad.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        pad.AddThemeConstantOverride("margin_left", 6);
        pad.AddThemeConstantOverride("margin_right", 6);
        pad.AddThemeConstantOverride("margin_top", 4);
        pad.AddThemeConstantOverride("margin_bottom", 4);

        HBoxContainer content = new()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        content.AddThemeConstantOverride("separation", 8);

        Label cost = MakeLabel(card.CostText, 11, White);
        cost.CustomMinimumSize = new Vector2(50, 0);
        content.AddChild(cost);

        Label title = MakeLabel(card.Title, 11, White);
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        title.ClipText = true;
        content.AddChild(title);

        pad.AddChild(content);
        row.AddChild(pad);
        return row;
    }

    private void OnChanged(OverlayState state)
    {
        if (!IsInsideTree())
        {
            return;
        }

        Callable.From(() => ApplyState(state)).CallDeferred();
    }

    private void ApplyState(OverlayState state)
    {
        if (_players == null || _emptyLabel == null)
        {
            return;
        }

        _lastState = state;

        foreach (Node child in _players.GetChildren())
        {
            child.QueueFree();
        }

        bool showPlayers = state.CombatActive && state.Players.Count > 0;
        _players.Visible = showPlayers;
        _emptyLabel.Visible = !showPlayers;

        if (!showPlayers)
        {
            return;
        }

        for (int i = 0; i < state.Players.Count; i++)
        {
            _players.AddChild(BuildPlayerSection(state.Players[i]));
        }
    }

    private void OnToggle()
    {
        _expanded = !_expanded;
        if (_toggleBtn != null)
        {
            _toggleBtn.Text = _expanded ? "\u25bc" : "\u25b6";
        }

        if (_root != null)
        {
            float width = _expanded ? 360 : 260;
            _root.CustomMinimumSize = new Vector2(width, 0);
            _root.Size = new Vector2(width, 0);
        }

        if (_lastState != null)
        {
            ApplyState(_lastState);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_dragging && @event is InputEventMouseButton mouseButton &&
            !mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
        {
            _dragging = false;
        }
    }

    private void OnGuiInput(InputEvent @event)
    {
        if (_root == null)
        {
            return;
        }

        switch (@event)
        {
            case InputEventMouseButton mouseButton when mouseButton.ButtonIndex == MouseButton.Left:
                if (mouseButton.Pressed)
                {
                    _dragging = true;
                    _dragOffset = GetViewport().GetMousePosition() - _root.Position;
                    GetViewport().SetInputAsHandled();
                }
                else
                {
                    _dragging = false;
                }
                break;
            case InputEventMouseMotion when _dragging:
                ClampPosition(GetViewport().GetMousePosition() - _dragOffset);
                GetViewport().SetInputAsHandled();
                break;
        }
    }

    private void ClampPosition(Vector2 position)
    {
        if (_root == null)
        {
            return;
        }

        Vector2 viewport = GetViewport().GetVisibleRect().Size;
        Vector2 size = _root.Size;
        _root.Position = new Vector2(
            Mathf.Clamp(position.X, 0, Mathf.Max(0, viewport.X - size.X)),
            Mathf.Clamp(position.Y, 0, Mathf.Max(0, viewport.Y - size.Y)));
    }

    private static Color GetCardColor(CardType type)
    {
        return type switch
        {
            CardType.Attack => new Color(Attack, 0.55f),
            CardType.Skill => new Color(Skill, 0.55f),
            CardType.Power => new Color(Power, 0.55f),
            _ => new Color(Other, 0.55f)
        };
    }

    private static Label MakeLabel(string text, int size, Color color)
    {
        Label label = new()
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }
}
