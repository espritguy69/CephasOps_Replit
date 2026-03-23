using CephasOps.Application.Parser.DTOs;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Resolves template profiles from ParserTemplates (ProfileConfig in Description with PROFILE_JSON: prefix).
/// Returns best-matching profile context for parser hints; specificity: partnerId &gt; filenameRegex &gt; senderDomain &gt; subjectContains.
/// Phase 9: profile config by id and pack resolution for replay-profile-pack.
/// </summary>
public interface ITemplateProfileService
{
    /// <summary>
    /// Get the best matching template profile for an email/attachment. Returns null if no enabled profile matches.
    /// </summary>
    Task<TemplateProfileContext?> GetBestMatchProfileAsync(
        string? senderEmail,
        string? subject,
        string? attachmentFileName,
        Guid? partnerId,
        Guid? companyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 9: Get profile config by template/profile id. Returns config even if profile is disabled (for pack replay).
    /// </summary>
    Task<(TemplateProfileConfig Config, Guid TemplateId)?> GetProfileConfigByIdAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 9: Get all templates that have valid PROFILE_JSON config. When enabledOnly is true, only enabled and active.
    /// </summary>
    Task<IReadOnlyList<(TemplateProfileConfig Config, Guid TemplateId)>> GetAllProfileConfigsAsync(bool enabledOnly, CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 9: Resolve pack to list of attachment IDs. Uses attachmentIds first; fallback: parseSessionIds via session+drafts filename.
    /// </summary>
    Task<IReadOnlyList<Guid>> ResolvePackAttachmentIdsAsync(ProfilePackConfig pack, CancellationToken cancellationToken = default);
}
