namespace CephasOps.Application.Integration;

/// <summary>
/// Optional signing for outbound requests (e.g. HMAC in header). Per-connector config.
/// </summary>
public interface IOutboundSigner
{
    /// <summary>Whether this signer handles the given connector type/key.</summary>
    bool CanSign(string connectorType, string? connectorKey = null);

    /// <summary>Add signature (and optional timestamp) to headers. Secret from endpoint config.</summary>
    void Sign(string payloadJson, string? signingConfigJson, IDictionary<string, string> headers);
}
