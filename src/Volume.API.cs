using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;
using System.Collections.Concurrent;
using Volume.API.Services;
using Volume.Contract;

namespace Volume.API;

[PluginMetadata(Id = "Volume.API", Version = "1.0.0", Name = "Volume.API", Author = "aga", Description = "Shared player sound volume")]
public partial class API : BasePlugin, IPlayerVolumeAPI
{
    private readonly ConcurrentDictionary<string, VolumeFeatureInfo> _features = new(StringComparer.OrdinalIgnoreCase);
    private PlayerCookiesApiWrapper? _cookies;

    public API(ISwiftlyCore core) : base(core)
    {
    }

    public override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
    {
        interfaceManager.AddSharedInterface<IPlayerVolumeAPI, API>("Volume.Player.v1", this);
    }

    public override void UseSharedInterface(IInterfaceManager interfaceManager)
    {
        _cookies = null;
        try
        {
            if (interfaceManager.HasSharedInterface("Cookies.Player.v2"))
            {
                var v2 = interfaceManager.GetSharedInterface<Cookies.Contract.IPlayerCookiesAPIv2>("Cookies.Player.v2");
                if (v2 != null) _cookies = new PlayerCookiesApiWrapper(v2);
            }
            if (_cookies == null && interfaceManager.HasSharedInterface("Cookies.Player.v1"))
            {
                var v1 = interfaceManager.GetSharedInterface<Cookies.Contract.IPlayerCookiesAPIv1>("Cookies.Player.v1");
                if (v1 != null) _cookies = new PlayerCookiesApiWrapper(v1);
            }

            if (_cookies == null)
                Core.Logger.LogWarning("[Volume.API] Cookies plugin not found; volume changes are unavailable.");
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "[Volume.API] Failed to initialize Cookies API.");
            _cookies = null;
        }
    }

    public override void Load(bool hotReload)
    {
        Core.MenusAPI.MenuClosed += OnMenuClosed;
        Core.Event.OnClientDisconnected += OnClientDisconnected;
        Core.Logger.LogInformation("[Volume.API] Plugin loaded.");
    }

    public override void Unload()
    {
        Core.MenusAPI.MenuClosed -= OnMenuClosed;
        Core.Event.OnClientDisconnected -= OnClientDisconnected;
        foreach (var menu in _openMenus.Values)
            Core.MenusAPI.CloseMenu(menu);
        _openMenus.Clear();

        foreach (var name in new[] { "vol", "volume" })
        {
            try
            {
                Core.Command.UnregisterCommand(name);
            }
            catch (Exception ex)
            {
                Core.Logger.LogError(ex, "[Volume.API] Failed to unregister {Command} command.", name);
            }
        }
        _features.Clear();
        _cookies = null;
    }

    public void RegisterFeature(string featureKey, string displayName)
    {
        ValidateFeatureKey(featureKey);
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("A display name is required.", nameof(displayName));
        _features[featureKey] = new VolumeFeatureInfo(featureKey, displayName);
    }

    public void UnregisterFeature(string featureKey)
    {
        ValidateFeatureKey(featureKey);
        _features.TryRemove(featureKey, out _);
    }

    public IReadOnlyList<VolumeFeatureInfo> GetFeatures() =>
        Array.AsReadOnly(_features.Values.OrderBy(feature => feature.DisplayName, StringComparer.OrdinalIgnoreCase).ToArray());

    public float GetGlobalVolume(long steamId) => TryReadVolume(steamId, "Volume.Global", out var volume) ? volume : 1f;

    public void SetGlobalVolume(long steamId, float volume) => WriteVolume(steamId, "Volume.Global", volume);

    public float GetFeatureVolume(long steamId, string featureKey) =>
        TryReadVolume(steamId, FeatureKey(featureKey), out var volume) ? volume : 1f;

    public void SetFeatureVolume(long steamId, string featureKey, float volume)
    {
        ValidateFeatureKey(featureKey);
        if (!_features.TryGetValue(featureKey, out var feature))
            throw new ArgumentException("Feature is not registered.", nameof(featureKey));
        WriteVolume(steamId, FeatureKey(feature.FeatureKey), volume);
    }

    public float GetEffectiveVolume(long steamId, string featureKey) =>
        TryReadVolume(steamId, FeatureKey(featureKey), out var volume) ? volume : GetGlobalVolume(steamId);

    private string FeatureKey(string featureKey)
    {
        ValidateFeatureKey(featureKey);
        return $"Volume.Feature.{(_features.TryGetValue(featureKey, out var feature) ? feature.FeatureKey : featureKey)}";
    }

    private static void ValidateFeatureKey(string featureKey)
    {
        if (string.IsNullOrWhiteSpace(featureKey) || featureKey.Length > 64 ||
            featureKey.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('_' or '-' or '.')))
            throw new ArgumentException("Feature keys must contain only ASCII letters, digits, underscores, hyphens or dots (max 64 characters).", nameof(featureKey));
    }

    private bool TryReadVolume(long steamId, string key, out float volume)
    {
        volume = 1f;
        if (_cookies == null) return false;

        try
        {
            if (!_cookies.Has(steamId, key)) return false;
            var stored = _cookies.Get<float>(steamId, key);
            if (!float.IsFinite(stored) || stored < 0f || stored > 1f) return false;
            volume = stored;
            return true;
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "[Volume.API] Failed to read volume cookie {Key} for {SteamId}", key, steamId);
            return false;
        }
    }

    private void WriteVolume(long steamId, string key, float volume)
    {
        if (!float.IsFinite(volume) || volume < 0f || volume > 1f)
            throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0 and 1.");
        if (_cookies == null) throw new InvalidOperationException("Cookies plugin is required to save volume.");

        try
        {
            _cookies.Set(steamId, key, volume);
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "[Volume.API] Failed to save volume cookie {Key} for {SteamId}", key, steamId);
            throw;
        }
    }

}
