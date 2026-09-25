<div align="center">

# [SwiftlyS2] Volume.API

<a href="https://github.com/a2Labs-cc/Volume.API/releases/latest">
  <img src="https://img.shields.io/github/v/release/a2Labs-cc/Volume.API?label=release&color=07f223&style=for-the-badge">
</a>
<a href="https://github.com/a2Labs-cc/Volume.API/issues">
  <img src="https://img.shields.io/github/issues/a2Labs-cc/Volume.API?label=issues&color=E63946&style=for-the-badge">
</a>
<a href="https://github.com/a2Labs-cc/Volume.API/releases">
  <img src="https://img.shields.io/github/downloads/a2Labs-cc/Volume.API/total?label=downloads&color=3A86FF&style=for-the-badge">
</a>
<a href="https://github.com/a2Labs-cc/Volume.API/stargazers">
  <img src="https://img.shields.io/github/stars/a2Labs-cc/Volume.API?label=stars&color=e3d322&style=for-the-badge">
</a>

<br/>
<sub>Made by <a href="https://github.com/agasking1337" target="_blank" rel="noopener noreferrer">aga</a></sub>

</div>

## Overview

**Volume.API** is a shared SwiftlyS2 volume system for CS2 sound plugins. Instead of each plugin registering its own `!volume` command, this plugin owns the command and exposes `IPlayerVolumeAPI` to other plugins through the `Volume.Player.v1` shared interface.

Players can set one global volume for all participating sound features or override a particular feature. For example, QuakeSounds registers the feature key `QuakeSounds` with the display name `Quake Sounds`.

> ⚠️ **Cookies is required for volume consistency.** Volume.API delegates all persistence to the [Cookies](https://github.com/SwiftlyS2-Plugins/Cookies) plugin. Without Cookies installed, loaded, and connected to its database, volume changes exist only in memory until the player disconnects or the server restarts; on reconnect or map change they will revert to the default `1.0` (100%).

## Support

Need help or have questions? Join our Discord server:

<p align="center">
  <a href="https://discord.gg/d853jMW2gh" target="_blank">
    <img src="https://img.shields.io/badge/Join%20Discord-5865F2?logo=discord&logoColor=white&style=for-the-badge" alt="Discord">
  </a>
</p>

## Download Shortcuts
<ul>
  <li>
    <code>📦</code>
    <strong>&nbsp;Download Latest Plugin Version</strong> &rarr;
    <a href="https://github.com/a2Labs-cc/Volume.API/releases/latest" target="_blank" rel="noopener noreferrer">Click Here</a>
  </li>
  <li>
    <code>📦</code>
    <strong>&nbsp;Download Latest Contract Developer Package</strong> &rarr;
    <a href="https://github.com/a2Labs-cc/Volume.API/releases" target="_blank" rel="noopener noreferrer">Click Here</a>
  </li>
  <li>
    <code>⚙️</code>
    <strong>&nbsp;Download Latest Cookies Plugin</strong> &rarr;
    <a href="https://github.com/SwiftlyS2-Plugins/Cookies/releases/latest" target="_blank" rel="noopener noreferrer">Click Here</a>
  </li>
  <li>
    <code>⚙️</code>
    <strong>&nbsp;Download Latest SwiftlyS2 Version</strong> &rarr;
    <a href="https://github.com/swiftly-solution/swiftlys2/releases/latest" target="_blank" rel="noopener noreferrer">Click Here</a>
  </li>
</ul>

## Installation

<ul>
  <li>
    <code>⚠️</code>
    <strong>&nbsp;Cookies plugin is required for persistence</strong> &rarr;
    <a href="https://github.com/SwiftlyS2-Plugins/Cookies/releases/latest" target="_blank" rel="noopener noreferrer">Click Here</a>
  </li>
</ul>

1. Download/build the plugin. The release contains two assets:
   - `Volume.API-v{VERSION}.zip` — server plugin package.
   - `Volume.Contract-v{VERSION}.zip` — developer contract package (for compiling consumer plugins).
2. Extract the plugin package and copy the `Volume.API` folder to your server:

```text
.../game/csgo/addons/swiftlys2/plugins/Volume.API/
```

3. Make sure `resources/exports/Volume.Contract.dll` remains inside the `Volume.API` folder. SwiftlyS2 loads the shared contract from that path so consumer plugins can resolve it.
4. Install and configure the [Cookies](https://github.com/SwiftlyS2-Plugins/Cookies) plugin. Without Cookies, volume reads fall back to defaults and setters fail rather than falsely claiming persistence.
5. Install sound plugins that consume `Volume.Player.v1` (such as [QuakeSounds](https://github.com/a2Labs-cc/SW2-QuakeSounds)), then start/restart the server. They must use the same `Volume.Contract` assembly and must not register a competing `!volume` command.

## Commands

| Command | Result |
| --- | --- |
| `!volume` or `!vol` | Opens a per-player volume menu with a global slider and one slider for every registered feature. |
| `!volume <0-10>` or `!vol <0-10>` | Sets your global volume (for example, `!volume 5` sets 50%). |
| `!volume <featureKey> <0-10>` or `!vol <featureKey> <0-10>` | Sets an override for one feature (for example, `!volume QuakeSounds 3` sets QuakeSounds to 30%). |

In the menu, use SwiftlyS2's menu movement/select controls to adjust each slider in whole steps from 0 to 10. The feature sliders initially show each feature's **effective** volume; moving a feature slider saves an override. If you adjust the global slider, features without an override follow it. Feature keys are case-insensitive when typed in commands; open the menu to see registered names and keys. Unregistered features cannot be changed. Setting global volume does **not** remove existing feature overrides.

Example:

```text
!volume 5
!volume QuakeSounds 3
!vol
```

The menu then shows Global at `5/10` and Quake Sounds at `3/10`. Other registered features without an override show `5/10`.

## Volume Resolution and Persistence

The effective volume for a player and feature is resolved in this order:

1. The feature override, if set.
2. The global volume, if set.
3. `1.0` (100%), if neither is set.

The API uses floats from `0.0` to `1.0`, while commands use integers from `0` to `10`. Settings are stored per Steam ID through Cookies under `Volume.Global` and `Volume.Feature.{featureKey}`. Cookies loads player values on connect, queues writes from `Set` for database saving, and flushes them on disconnect; Volume.API reads those values through Cookies whenever a menu opens or a sound plugin requests a volume. Volume.API does not start a second asynchronous load/save that could race with Cookies. On reconnect or map change the same Steam ID reads the saved values again. No separate Volume.API data store is used.

> ⚠️ **If Cookies is not installed, loaded, or connected to its database, volume changes will not survive disconnect, map change, or server restart.** In that case reads fall back to the defaults above, setters fail, and sound plugins may choose their own fallback when Volume.API itself is unavailable.

## Using the API in Another Plugin

Add a reference to `Volume.Contract.dll` from this project and do not copy the contract into the consumer plugin's output (Volume.API exports it under `resources/exports/`):

```xml
<ItemGroup>
  <Reference Include="Volume.Contract">
    <HintPath>lib\Volume.Contract.dll</HintPath>
    <Private>false</Private>
  </Reference>
</ItemGroup>
```

Resolve the interface in your plugin's `UseSharedInterface` callback, register a stable feature key, and unregister on unload. SwiftlyS2 calls `UseSharedInterface` after providers have registered their interfaces and may call it again when plugins change; registration is safe to repeat. Wrap calls to another plugin in `try/catch` and handle the API being absent:

```csharp
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using Volume.Contract;

private IPlayerVolumeAPI? _volumeApi;

public override void UseSharedInterface(IInterfaceManager interfaceManager)
{
    _volumeApi = null;
    try
    {
        if (!interfaceManager.HasSharedInterface("Volume.Player.v1")) return;
        _volumeApi = interfaceManager.GetSharedInterface<IPlayerVolumeAPI>("Volume.Player.v1");
        _volumeApi.RegisterFeature("MySounds", "My Sounds");
    }
    catch (Exception ex)
    {
        Core.Logger.LogError(ex, "Could not connect to Volume.API");
        _volumeApi = null;
    }
}

private float GetSoundVolume(ulong steamId, float configuredFallback)
{
    if (_volumeApi == null) return configuredFallback;
    try
    {
        return _volumeApi.GetEffectiveVolume((long)steamId, "MySounds");
    }
    catch (Exception ex)
    {
        Core.Logger.LogError(ex, "Could not read sound volume");
        return configuredFallback;
    }
}

public override void Unload()
{
    if (_volumeApi == null) return;
    try
    {
        _volumeApi.UnregisterFeature("MySounds");
    }
    catch (Exception ex)
    {
        Core.Logger.LogError(ex, "Could not unregister volume feature");
    }
}
```

Use the returned `GetSoundVolume` value as the **player's playback volume**, including when a sound is sent to multiple players. Choose a stable feature key so saved overrides still apply after plugin reloads; keys can contain ASCII letters, digits, underscores, hyphens, or dots (maximum 64 characters). Registration requires a nonempty display name.

### Contract Methods

| Method | Purpose |
| --- | --- |
| `RegisterFeature(key, displayName)` / `UnregisterFeature(key)` | Add or remove a feature from `!volume`. |
| `GetGlobalVolume(steamId)` / `SetGlobalVolume(steamId, volume)` | Read or save the player's global volume. |
| `GetFeatureVolume(steamId, key)` / `SetFeatureVolume(steamId, key, volume)` | Read or save the feature override; `GetFeatureVolume` returns `1.0` if no override is stored, **not** the global volume. The feature must be registered before setting it. |
| `GetEffectiveVolume(steamId, key)` | Read the feature override, falling back to the global volume and then `1.0`. Use this for playback. |
| `GetFeatures()` | Get the registered feature keys and display names. |

Setters accept only finite values between `0.0` and `1.0` and require Cookies to be available.

## Building

Requires the .NET 10 SDK. The project references `lib/Cookies.Contract.dll` at build time; install the Cookies plugin on the server for runtime persistence.

```bash
dotnet build Volume.API.csproj -c Release
dotnet publish Volume.API.csproj -c Release
```

The build output is under `build/net10.0/`; publishing creates `build/publish/Volume.API/` with `resources/exports/Volume.Contract.dll` for the server. The GitHub Actions release workflow (manual dispatch or a push with `[build]` in the commit message) also stages two separate folders under `build/package/`: `Volume.API/` for server installation and `Volume.Contract/` containing the standalone DLL for plugin developers. Each folder is published as its own versioned zip in the GitHub release. Install the **plugin** zip on the server; the contract-only zip is for compiling consumers, not a second server plugin.
