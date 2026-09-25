using Microsoft.Extensions.Logging;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using System.Collections.Concurrent;

namespace Volume.API;

public partial class API
{
    private readonly ConcurrentDictionary<int, IMenuAPI> _openMenus = new();

    private void OnMenuClosed(object? sender, MenuManagerEventArgs args)
    {
        if (args.Player == null || args.Menu == null) return;
        if (_openMenus.TryGetValue(args.Player.PlayerID, out var menu) && ReferenceEquals(menu, args.Menu))
            _openMenus.TryRemove(args.Player.PlayerID, out _);
    }

    private void OnClientDisconnected(IOnClientDisconnectedEvent @event)
    {
        if (!_openMenus.TryRemove(@event.PlayerId, out var menu)) return;
        try
        {
            Core.MenusAPI.CloseMenu(menu);
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "[Volume.API] Could not close volume menu for player {PlayerId}", @event.PlayerId);
        }
    }

    private void OpenVolumeMenu(IPlayer player)
    {
        var steamId = (long)player.SteamID;
        var builder = Core.MenusAPI.CreateBuilder();
        builder.Design.SetMenuTitle(Localize("menus.volume.title"));
        builder.SetAutoCloseDelay(0f);

        var featureSliders = new List<(string Key, SliderMenuOption Slider)>();
        var updatingInherited = false;
        var globalSlider = new SliderMenuOption(Localize("menus.volume.global"), min: 0f, max: 10f,
            defaultValue: MathF.Round(GetGlobalVolume(steamId) * 10f), step: 1f, totalBars: 10);
        globalSlider.ValueChanged += (sender, args) =>
        {
            try
            {
                SetGlobalVolume((long)args.Player.SteamID, args.NewValue / 10f);
                updatingInherited = true;
                foreach (var (key, slider) in featureSliders)
                {
                    if (!TryReadVolume((long)args.Player.SteamID, FeatureKey(key), out _))
                        slider.SetValue(args.Player, args.NewValue);
                }
            }
            catch (Exception ex)
            {
                Core.Logger.LogError(ex, "[Volume.API] Could not save global volume for {SteamId}", args.Player.SteamID);
                args.Player.SendChat(Localize("commands.volume.save_failed"));
            }
            finally
            {
                updatingInherited = false;
            }
        };
        builder.AddOption(globalSlider);

        foreach (var feature in GetFeatures())
        {
            var key = feature.FeatureKey;
            var slider = new SliderMenuOption(Localize("menus.volume.feature", ("feature", feature.DisplayName), ("featureKey", key)),
                min: 0f, max: 10f,
                defaultValue: MathF.Round(GetEffectiveVolume(steamId, key) * 10f), step: 1f, totalBars: 10);
            slider.ValueChanged += (_, args) =>
            {
                if (updatingInherited) return;
                try
                {
                    SetFeatureVolume((long)args.Player.SteamID, key, args.NewValue / 10f);
                }
                catch (Exception ex)
                {
                    Core.Logger.LogError(ex, "[Volume.API] Could not save {Feature} volume for {SteamId}", key, args.Player.SteamID);
                    args.Player.SendChat(Localize("commands.volume.save_failed"));
                }
            };
            featureSliders.Add((key, slider));
            builder.AddOption(slider);
        }

        if (_openMenus.TryRemove(player.PlayerID, out var previous))
            Core.MenusAPI.CloseMenuForPlayer(player, previous);

        var menu = builder.Build();
        _openMenus[player.PlayerID] = menu;
        Core.MenusAPI.OpenMenuForPlayer(player, menu);
    }
}
