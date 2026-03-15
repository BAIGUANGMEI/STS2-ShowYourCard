using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Platform;

namespace ShowYourCard;

public static class TeammateHandService
{
    private static readonly object SyncRoot = new();

    private static OverlayState _state = new()
    {
        CombatActive = false,
        LocalPlayerNetId = null,
        ActivePlayerNetId = null,
        Players = Array.Empty<PlayerHandSnapshot>()
    };

    public static event Action<OverlayState>? Changed;

    public static void BeginCombat(object? combatState)
    {
        RefreshFromCombat(combatState);
    }

    public static void EndCombat()
    {
        Publish(new OverlayState
        {
            CombatActive = false,
            LocalPlayerNetId = LocalContext.NetId,
            ActivePlayerNetId = null,
            Players = Array.Empty<PlayerHandSnapshot>()
        });
    }

    public static void RefreshFromCombat(object? combatState)
    {
        ulong? activePlayerNetId;
        lock (SyncRoot)
        {
            activePlayerNetId = _state.ActivePlayerNetId;
        }

        RefreshFromCombat(combatState, activePlayerNetId);
    }

    public static void RefreshFromCombat(object? combatState, ulong? activePlayerNetId)
    {
        if (combatState is not CombatState typedCombatState)
        {
            return;
        }

        ulong? localPlayerNetId = LocalContext.NetId;

        PlayerHandSnapshot[] snapshots = typedCombatState.Players
            .Where(player => !LocalContext.IsMe(player))
            .Select(player => BuildPlayerSnapshot(player, activePlayerNetId))
            .OrderByDescending(snapshot => snapshot.IsActive)
            .ThenBy(snapshot => snapshot.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Publish(new OverlayState
        {
            CombatActive = true,
            LocalPlayerNetId = localPlayerNetId,
            ActivePlayerNetId = activePlayerNetId,
            Players = snapshots
        });
    }

    public static void NoteActivePlayer(object? combatState, object? player)
    {
        ulong? activePlayerNetId = player is Player typedPlayer ? typedPlayer.NetId : null;
        RefreshFromCombat(combatState, activePlayerNetId);
    }

    public static OverlayState BuildOverlayState()
    {
        lock (SyncRoot)
        {
            return _state;
        }
    }

    private static PlayerHandSnapshot BuildPlayerSnapshot(Player player, ulong? activePlayerNetId)
    {
        IReadOnlyList<CardModel> handCards = player.PlayerCombatState?.Hand.Cards ?? Array.Empty<CardModel>();

        CardSnapshot[] cards = handCards
            .Select(BuildCardSnapshot)
            .ToArray();

        return new PlayerHandSnapshot
        {
            NetId = player.NetId,
            DisplayName = ResolvePlayerName(player),
            CharacterName = ResolveCharacterName(player),
            PortraitTexture = player.Character.IconTexture,
            IsActive = activePlayerNetId.HasValue && activePlayerNetId.Value == player.NetId,
            Cards = cards
        };
    }

    private static CardSnapshot BuildCardSnapshot(CardModel card)
    {
        return new CardSnapshot
        {
            Title = card.Title,
            CostText = BuildCostText(card),
            Type = card.Type,
            IsUpgraded = card.IsUpgraded
        };
    }

    private static string ResolvePlayerName(Player player)
    {
        try
        {
            PlatformType platformType = PlatformUtil.PrimaryPlatform;
            if (platformType != PlatformType.None)
            {
                string? playerName = PlatformUtil.GetPlayerName(platformType, player.NetId);
                if (!string.IsNullOrWhiteSpace(playerName) &&
                    !string.Equals(playerName, "[unknown]", StringComparison.OrdinalIgnoreCase))
                {
                    return playerName;
                }
            }
        }
        catch
        {
            // Fall back to a stable net id label.
        }

        return $"Player {player.NetId}";
    }

    private static string ResolveCharacterName(Player player)
    {
        return player.Character.Title.GetFormattedText();
    }

    private static string BuildCostText(CardModel card)
    {
        string energyCost = card.EnergyCost.CostsX
            ? "X"
            : card.EnergyCost.GetWithModifiers(CostModifiers.All).ToString();

        bool hasStarCost = card.HasStarCostX || card.CurrentStarCost >= 0;
        if (!hasStarCost)
        {
            return energyCost + "E";
        }

        string starCost = card.HasStarCostX
            ? "X"
            : card.GetStarCostWithModifiers().ToString();

        return $"{energyCost}E {starCost}S";
    }

    private static void Publish(OverlayState state)
    {
        lock (SyncRoot)
        {
            _state = state;
        }

        Changed?.Invoke(state);
    }
}

public sealed class OverlayState
{
    public bool CombatActive { get; init; }

    public ulong? LocalPlayerNetId { get; init; }

    public ulong? ActivePlayerNetId { get; init; }

    public IReadOnlyList<PlayerHandSnapshot> Players { get; init; } = Array.Empty<PlayerHandSnapshot>();
}

public sealed class PlayerHandSnapshot
{
    public ulong NetId { get; init; }

    public string DisplayName { get; init; } = "Unknown Player";

    public string CharacterName { get; init; } = "Unknown Character";

    public Texture2D? PortraitTexture { get; init; }

    public bool IsActive { get; init; }

    public IReadOnlyList<CardSnapshot> Cards { get; init; } = Array.Empty<CardSnapshot>();
}

public sealed class CardSnapshot
{
    public string Title { get; init; } = string.Empty;

    public string CostText { get; init; } = string.Empty;

    public CardType Type { get; init; }

    public bool IsUpgraded { get; init; }
}
