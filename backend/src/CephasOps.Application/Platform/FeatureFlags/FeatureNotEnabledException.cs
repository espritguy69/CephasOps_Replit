namespace CephasOps.Application.Platform.FeatureFlags;

/// <summary>Thrown when a feature is required but not enabled for the tenant.</summary>
public class FeatureNotEnabledException : InvalidOperationException
{
    public FeatureNotEnabledException(string featureKey)
        : base($"Feature '{featureKey}' is not enabled for this tenant.")
    {
        FeatureKey = featureKey;
    }

    public string FeatureKey { get; }
}
