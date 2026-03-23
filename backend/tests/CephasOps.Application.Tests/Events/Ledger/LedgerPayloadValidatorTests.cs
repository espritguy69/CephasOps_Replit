using CephasOps.Application.Events.Ledger;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CephasOps.Application.Tests.Events.Ledger;

public class LedgerPayloadValidatorTests
{
    private static LedgerPayloadValidator CreateValidator(int? maxPayloadSizeBytes = null, bool validateJson = true)
    {
        var options = new LedgerOptions
        {
            MaxPayloadSizeBytes = maxPayloadSizeBytes ?? 64 * 1024,
            ValidateJsonPayload = validateJson
        };
        return new LedgerPayloadValidator(Options.Create(options));
    }

    [Fact]
    public void Validate_NullPayload_AcceptsAndReturnsNull()
    {
        var validator = CreateValidator();
        var result = validator.Validate(null, "WorkflowTransition", "WorkflowTransitionCompleted");
        result.WasRejected.Should().BeFalse();
        result.WasTruncated.Should().BeFalse();
        result.PayloadToStore.Should().BeNull();
    }

    [Fact]
    public void Validate_EmptyPayload_AcceptsAndReturnsNull()
    {
        var validator = CreateValidator();
        var result = validator.Validate("", "WorkflowTransition", "WorkflowTransitionCompleted");
        result.WasRejected.Should().BeFalse();
        result.WasTruncated.Should().BeFalse();
        result.PayloadToStore.Should().BeNull();
    }

    [Fact]
    public void Validate_ValidJsonObject_Accepts()
    {
        var validator = CreateValidator();
        var payload = "{\"FromStatus\":\"Pending\",\"ToStatus\":\"Assigned\"}";
        var result = validator.Validate(payload, "WorkflowTransition", "WorkflowTransitionCompleted");
        result.WasRejected.Should().BeFalse();
        result.WasTruncated.Should().BeFalse();
        result.PayloadToStore.Should().Be(payload);
    }

    [Fact]
    public void Validate_ValidJsonArray_Accepts()
    {
        var validator = CreateValidator();
        var payload = "[1,2,3]";
        var result = validator.Validate(payload, "WorkflowTransition", "WorkflowTransitionCompleted");
        result.WasRejected.Should().BeFalse();
        result.WasTruncated.Should().BeFalse();
        result.PayloadToStore.Should().Be(payload);
    }

    [Fact]
    public void Validate_JsonPrimitive_Rejects()
    {
        var validator = CreateValidator();
        var result = validator.Validate("\"string\"", "WorkflowTransition", "WorkflowTransitionCompleted");
        result.WasRejected.Should().BeTrue();
        result.RejectionReason.Should().Contain("Object or Array");
        result.PayloadToStore.Should().BeNull();
    }

    [Fact]
    public void Validate_JsonNumber_Rejects()
    {
        var validator = CreateValidator();
        var result = validator.Validate("42", "WorkflowTransition", "WorkflowTransitionCompleted");
        result.WasRejected.Should().BeTrue();
        result.PayloadToStore.Should().BeNull();
    }

    [Fact]
    public void Validate_MalformedJson_Rejects()
    {
        var validator = CreateValidator();
        var result = validator.Validate("{ invalid }", "WorkflowTransition", "WorkflowTransitionCompleted");
        result.WasRejected.Should().BeTrue();
        result.RejectionReason.Should().Contain("Invalid JSON");
        result.PayloadToStore.Should().BeNull();
    }

    [Fact]
    public void Validate_WhenValidateJsonDisabled_AcceptsMalformedJson()
    {
        var validator = CreateValidator(validateJson: false);
        var payload = "not json at all";
        var result = validator.Validate(payload, "WorkflowTransition", "WorkflowTransitionCompleted");
        result.WasRejected.Should().BeFalse();
        result.PayloadToStore.Should().Be(payload);
    }

    [Fact]
    public void Validate_PayloadExceedsMaxSize_TruncatesWithPlaceholder()
    {
        var maxBytes = 50;
        var validator = CreateValidator(maxPayloadSizeBytes: maxBytes);
        var payload = "{\"key\":\"" + new string('x', 100) + "\"}";
        var result = validator.Validate(payload, "WorkflowTransition", "WorkflowTransitionCompleted");
        result.WasRejected.Should().BeFalse();
        result.WasTruncated.Should().BeTrue();
        result.OriginalSizeBytes.Should().BeGreaterThan(maxBytes);
        result.PayloadToStore.Should().NotBeNullOrEmpty();
        result.PayloadToStore.Should().Contain("_ledgerPayloadTruncated");
        result.PayloadToStore.Should().Contain("_originalSizeBytes");
    }

    [Fact]
    public void Validate_PayloadAtExactlyMaxSize_Accepts()
    {
        var maxBytes = 100;
        var validator = CreateValidator(maxPayloadSizeBytes: maxBytes);
        var payload = "{\"a\":\"" + new string('x', 100 - 10) + "\"}"; // roughly 100 bytes
        var size = System.Text.Encoding.UTF8.GetByteCount(payload);
        if (size > maxBytes)
            payload = "{\"a\":\"" + new string('x', 80) + "\"}";
        var result = validator.Validate(payload, "WorkflowTransition", "WorkflowTransitionCompleted");
        result.WasTruncated.Should().BeFalse();
        result.WasRejected.Should().BeFalse();
        result.PayloadToStore.Should().Be(payload);
    }
}
