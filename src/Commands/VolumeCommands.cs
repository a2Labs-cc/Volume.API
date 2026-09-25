using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared.Commands;
using System.Globalization;

namespace Volume.API;

public partial class API
{
    private string Localize(string key, params (string Name, string Value)[] tokens)
    {
        var text = Core.Localizer[key] ?? string.Empty;
        foreach (var (name, value) in tokens)
            text = text.Replace($"{{{name}}}", value, StringComparison.Ordinal);
        return text;
    }

    [Command("volume")]
    [CommandAlias("vol")]
    public void VolumeCommand(ICommandContext context)
    {
        if (context.Sender == null) return;
        var steamId = (long)context.Sender.SteamID;
        var args = context.Args;

        if (args.Length == 0)
        {
            try
            {
                OpenVolumeMenu(context.Sender);
            }
            catch (Exception ex)
            {
                Core.Logger.LogError(ex, "[Volume.API] Could not open volume menu for {SteamId}", steamId);
                context.Reply(Localize("commands.volume.open_failed"));
            }
            return;
        }

        if (args.Length == 1 && TryParseVolume(args[0], out var global))
        {
            try
            {
                SetGlobalVolume(steamId, global);
                context.Reply(Localize("commands.volume.set_global", ("volume", (global * 10f).ToString("0.#", CultureInfo.InvariantCulture))));
            }
            catch (Exception ex)
            {
                Core.Logger.LogError(ex, "[Volume.API] Could not save global volume for {SteamId}", steamId);
                context.Reply(Localize("commands.volume.save_failed"));
            }
            return;
        }

        if (args.Length == 2 && _features.TryGetValue(args[0], out var feature) && TryParseVolume(args[1], out var selected))
        {
            try
            {
                SetFeatureVolume(steamId, feature.FeatureKey, selected);
                context.Reply(Localize("commands.volume.set_feature", ("feature", feature.DisplayName),
                    ("volume", (selected * 10f).ToString("0.#", CultureInfo.InvariantCulture))));
            }
            catch (Exception ex)
            {
                Core.Logger.LogError(ex, "[Volume.API] Could not save feature volume for {SteamId}", steamId);
                context.Reply(Localize("commands.volume.save_failed"));
            }
            return;
        }

        context.Reply(Localize("commands.volume.usage"));
    }

    private static bool TryParseVolume(string value, out float volume)
    {
        volume = 0f;
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var level) || level < 0 || level > 10)
            return false;
        volume = level / 10f;
        return true;
    }
}
