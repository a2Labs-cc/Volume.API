using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menu;
using SwiftlyS2.Shared.Menu.Components;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;
using System.Collections.Concurrent;
using System.ComponentModel;
using Volume.API.Services;
using Volume.Contract;

namespace Volume.API;

public partial class API
{
    private readonly ConcurrentDictionary<int, IMenu> _openMenus = new();

    private void OnClientDisconnected(SwiftlyS2.Shared.Events.IOnClientDisconnectedEvent @event)
    {
        if (!_openMenus.TryRemove(@event.PlayerId, out var menu)) return;
        try
        {
            var p = Core.PlayerManager.GetPlayer(@event.PlayerId);
            if (p != null) Core.Menu.CloseFor(p);
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "[Volume.API] Could not close volume menu for player {PlayerId}", @event.PlayerId);
        }
    }

    private void OpenVolumeMenu(IPlayer player)
    {
        var steamId = (long)player.SteamID;
        var builder = Core.Menu.CreateMenu("volume.api.main")
            .WithTitle(Localize("menus.volume.title"))
            .WithAutoClose(null);

        var featureSliders = new List<(string Key, SliderComponent Slider)>();
        var updatingInherited = false;

        var globalSlider = new SliderComponent(
            text: Localize("menus.volume.global"),
            min: 0f,
            max: 10f,
            defaultValue: MathF.Round(GetGlobalVolume(steamId) * 10f),
            step: 1f,
            onChanged: ctx =>
            {
                try
                {
                    SetGlobalVolume((long)ctx.Player.SteamID, ctx.NewValue / 10f);
                    updatingInherited = true;
                    var session = Core.Menu.GetSession(ctx.Player);
                    foreach (var (key, slider) in featureSliders)
                    {
                        if (!TryReadVolume((long)ctx.Player.SteamID, FeatureKey(key), out _) && session != null)
                        {
                            slider.SetValue(session, ctx.NewValue);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Core.Logger.LogError(ex, "[Volume.API] Could not save global volume for {SteamId}", ctx.Player.SteamID);
                    ctx.Player.SendChat(Localize("commands.volume.save_failed"));
                }
                finally
                {
                    updatingInherited = false;
                }
            }
        )
        {
            MaxWidth = 0,
            Overflow = MenuTextOverflow.None,
            TotalBars = 4
        };
        builder.Add(globalSlider);

        foreach (var feature in GetFeatures())
        {
            var key = feature.FeatureKey;
            var effectiveVol = MathF.Round(GetEffectiveVolume(steamId, key) * 10f);

            var slider = new SliderComponent(
                text: Localize("menus.volume.feature",
                    ("feature", feature.DisplayName),
                    ("featureKey", key),
                    ("volume", effectiveVol.ToString()),
                    ("max", "10")),
                min: 0f,
                max: 10f,
                defaultValue: effectiveVol,
                step: 1f,
                onChanged: ctx =>
                {
                    if (updatingInherited) return;
                    try
                    {
                        SetFeatureVolume((long)ctx.Player.SteamID, key, ctx.NewValue / 10f);
                    }
                    catch (Exception ex)
                    {
                        Core.Logger.LogError(ex, "[Volume.API] Could not save {Feature} volume for {SteamId}", key, ctx.Player.SteamID);
                        ctx.Player.SendChat(Localize("commands.volume.save_failed"));
                    }
                }
            )
            {
                MaxWidth = 0,
                Overflow = MenuTextOverflow.None,
                TotalBars = 4
            };
            featureSliders.Add((key, slider));
            builder.Add(slider);
        }

        if (_openMenus.TryRemove(player.PlayerID, out var previous))
            Core.Menu.CloseFor(player);

        var menu = builder.Build();
        _openMenus[player.PlayerID] = menu;
        menu.Open(player);
    }
}
