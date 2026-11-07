using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared.Menu;
using SwiftlyS2.Shared.Menu.Components;
using SwiftlyS2.Shared.Players;

namespace Volume.API;

public partial class API
{
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
                    var playerSteamId = (long)ctx.Player.SteamID;
                    SetGlobalVolume(playerSteamId, ctx.NewValue / 10f);
                    updatingInherited = true;
                    foreach (var (key, slider) in featureSliders)
                    {
                        if (!TryReadVolume(playerSteamId, FeatureKey(key), out _))
                            slider.SetValue(ctx.Session, ctx.NewValue);
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
            })
        {
            MaxWidth = 0,
            Overflow = MenuTextOverflow.None,
            TotalBars = 10,
            ValueFormat = "0"
        };
        builder.Add(globalSlider);

        foreach (var feature in GetFeatures())
        {
            var key = feature.FeatureKey;
            var slider = new SliderComponent(
                text: Localize("menus.volume.feature", ("feature", feature.DisplayName), ("featureKey", key)),
                min: 0f,
                max: 10f,
                defaultValue: MathF.Round(GetEffectiveVolume(steamId, key) * 10f),
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
                })
            {
                MaxWidth = 0,
                Overflow = MenuTextOverflow.None,
                TotalBars = 10,
                ValueFormat = "0"
            };
            featureSliders.Add((key, slider));
            builder.Add(slider);
        }

        builder.Build().Open(player);
    }
}
