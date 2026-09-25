namespace Volume.Contract;

public interface IPlayerVolumeAPI
{
    void RegisterFeature(string featureKey, string displayName);
    void UnregisterFeature(string featureKey);

    float GetGlobalVolume(long steamId);
    void SetGlobalVolume(long steamId, float volume);

    float GetFeatureVolume(long steamId, string featureKey);
    void SetFeatureVolume(long steamId, string featureKey, float volume);

    float GetEffectiveVolume(long steamId, string featureKey);
    IReadOnlyList<VolumeFeatureInfo> GetFeatures();
}

public sealed class VolumeFeatureInfo
{
    public string FeatureKey { get; }
    public string DisplayName { get; }

    public VolumeFeatureInfo(string featureKey, string displayName)
    {
        FeatureKey = featureKey;
        DisplayName = displayName;
    }
}
