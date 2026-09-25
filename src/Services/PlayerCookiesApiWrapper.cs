using Cookies.Contract;

namespace Volume.API.Services;

public sealed class PlayerCookiesApiWrapper
{
    private readonly IPlayerCookiesAPIv1 _api;

    public PlayerCookiesApiWrapper(IPlayerCookiesAPIv1 api)
    {
        _api = api;
    }

    public bool Has(long steamId, string key) => _api.Has(steamId, key);

    public T? Get<T>(long steamId, string key) => _api.Get<T>(steamId, key);

    public void Set<T>(long steamId, string key, T value) => _api.Set(steamId, key, value);
}
