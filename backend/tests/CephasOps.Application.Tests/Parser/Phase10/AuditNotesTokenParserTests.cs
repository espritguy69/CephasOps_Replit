using CephasOps.Application.Parser.Utilities;
using FluentAssertions;
using Xunit;

namespace CephasOps.Application.Tests.Parser.Phase10;

public class AuditNotesTokenParserTests
{
    [Fact]
    public void Parse_returns_null_when_notes_null_or_empty()
    {
        AuditNotesTokenParser.Parse(null).Should().BeNull();
        AuditNotesTokenParser.Parse("").Should().BeNull();
    }

    [Fact]
    public void Parse_returns_null_when_no_audit_segment()
    {
        AuditNotesTokenParser.Parse("Some text without [Audit]").Should().BeNull();
        AuditNotesTokenParser.Parse("Prefix | Other=value").Should().BeNull();
    }

    [Fact]
    public void Parse_returns_null_when_audit_has_neither_ProfileId_nor_Category()
    {
        // Parser returns null if no ProfileId and no Category (per spec)
        var notes = " | [Audit] DriftDetected=true; HeaderScore=80";
        AuditNotesTokenParser.Parse(notes).Should().BeNull();
    }

    [Fact]
    public void Parse_extracts_ProfileId_and_ProfileName_from_audit_segment()
    {
        var profileId = Guid.NewGuid();
        var notes = "x | [Audit] Profile=" + profileId + "; ProfileName=MyProfile; Category=LAYOUT_DRIFT";
        var t = AuditNotesTokenParser.Parse(notes);
        t.Should().NotBeNull();
        t!.ProfileId.Should().Be(profileId);
        t.ProfileName.Should().Be("MyProfile");
        t.Category.Should().Be("LAYOUT_DRIFT");
    }

    [Fact]
    public void Parse_extracts_Category_without_ProfileId()
    {
        var notes = " | [Audit] Category=DATA_MISSING; Missing=ServiceId,CustomerName";
        var t = AuditNotesTokenParser.Parse(notes);
        t.Should().NotBeNull();
        t!.ProfileId.Should().BeNull();
        t.Category.Should().Be("DATA_MISSING");
        t.Missing.Should().NotBeNull();
        t.Missing!.Should().BeEquivalentTo(new[] { "ServiceId", "CustomerName" });
    }

    [Fact]
    public void Parse_handles_missing_tokens_gracefully()
    {
        var notes = " | [Audit] Category=VALIDATION_FAIL";
        var t = AuditNotesTokenParser.Parse(notes);
        t.Should().NotBeNull();
        t!.Category.Should().Be("VALIDATION_FAIL");
        t.ProfileId.Should().BeNull();
        t.DriftDetected.Should().BeNull();
        t.DriftSignature.Should().BeNull();
        t.Missing.Should().BeNull();
        t.HeaderScore.Should().BeNull();
        t.BestSheetScore.Should().BeNull();
        t.TemplateAction.Should().BeNull();
    }

    [Fact]
    public void Parse_extracts_DriftDetected_and_DriftSignature()
    {
        var notes = " | [Audit] Category=LAYOUT_DRIFT; Profile=" + Guid.NewGuid() + "; DriftDetected=true; DriftSignature=SheetChanged:Orders->Sheet1";
        var t = AuditNotesTokenParser.Parse(notes);
        t.Should().NotBeNull();
        t!.DriftDetected.Should().BeTrue();
        t.DriftSignature.Should().Be("SheetChanged:Orders->Sheet1");
    }

    [Fact]
    public void Parse_extracts_HeaderScore_and_BestSheetScore()
    {
        var notes = " | [Audit] Category=LAYOUT_DRIFT; Profile=" + Guid.NewGuid() + "; HeaderScore=85; BestSheetScore=90";
        var t = AuditNotesTokenParser.Parse(notes);
        t.Should().NotBeNull();
        t!.HeaderScore.Should().Be(85);
        t.BestSheetScore.Should().Be(90);
    }

    [Fact]
    public void Parse_extracts_TemplateAction()
    {
        var notes = " | [Audit] Category=LAYOUT_DRIFT; Profile=" + Guid.NewGuid() + "; TemplateAction=UpdateProfile";
        var t = AuditNotesTokenParser.Parse(notes);
        t.Should().NotBeNull();
        t!.TemplateAction.Should().Be("UpdateProfile");
    }

    [Fact]
    public void Parse_Missing_splits_on_comma()
    {
        var notes = " | [Audit] Category=DATA_MISSING; Missing=ServiceId, CustomerName , Address";
        var t = AuditNotesTokenParser.Parse(notes);
        t.Should().NotBeNull();
        t!.Missing.Should().NotBeNull();
        t.Missing!.Should().BeEquivalentTo(new[] { "ServiceId", "CustomerName", "Address" });
    }

    [Fact]
    public void Parse_uses_audit_prefix_case_insensitively()
    {
        var notes = " | [AUDIT] Category=PARSE_ERROR; Profile=" + Guid.NewGuid();
        var t = AuditNotesTokenParser.Parse(notes);
        t.Should().NotBeNull();
        t!.Category.Should().Be("PARSE_ERROR");
    }
}
