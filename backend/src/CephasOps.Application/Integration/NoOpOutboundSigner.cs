namespace CephasOps.Application.Integration;

/// <summary>
/// No-op signer when no signing is configured. CanSign returns false so it never actually signs.
/// </summary>
public class NoOpOutboundSigner : IOutboundSigner
{
    public bool CanSign(string connectorType, string? connectorKey = null) => false;
    public void Sign(string payloadJson, string? signingConfigJson, IDictionary<string, string> headers) { }
}
