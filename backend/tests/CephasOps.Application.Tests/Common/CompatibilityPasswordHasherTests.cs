using CephasOps.Application.Common.Services;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Xunit;

namespace CephasOps.Application.Tests.Common;

/// <summary>
/// Tests for v1.3 Phase B: legacy verify, modern hash format, NeedsRehash.
/// </summary>
public class CompatibilityPasswordHasherTests
{
    private readonly CompatibilityPasswordHasher _hasher = new();

    [Fact]
    public void VerifyPassword_LegacyHash_ReturnsTrue()
    {
        var password = "password123";
        var legacyHash = DatabaseSeeder.HashPassword(password);

        _hasher.VerifyPassword(password, legacyHash).Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_LegacyHash_WrongPassword_ReturnsFalse()
    {
        var legacyHash = DatabaseSeeder.HashPassword("right");

        _hasher.VerifyPassword("wrong", legacyHash).Should().BeFalse();
    }

    [Fact]
    public void HashPassword_ProducesModernFormat()
    {
        var hash = _hasher.HashPassword("any");

        hash.Should().StartWith("$2");
    }

    [Fact]
    public void VerifyPassword_ModernHash_ReturnsTrue()
    {
        var password = "newpass456";
        var hash = _hasher.HashPassword(password);

        _hasher.VerifyPassword(password, hash).Should().BeTrue();
    }

    [Fact]
    public void NeedsRehash_LegacyHash_ReturnsTrue()
    {
        var legacyHash = DatabaseSeeder.HashPassword("x");

        _hasher.NeedsRehash(legacyHash).Should().BeTrue();
    }

    [Fact]
    public void NeedsRehash_ModernHash_ReturnsFalse()
    {
        var modernHash = _hasher.HashPassword("x");

        _hasher.NeedsRehash(modernHash).Should().BeFalse();
    }
}
