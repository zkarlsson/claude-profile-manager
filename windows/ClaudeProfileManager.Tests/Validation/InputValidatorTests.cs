using ClaudeProfileManager.Core.Validation;
using FluentAssertions;
using Xunit;

namespace ClaudeProfileManager.Tests.Validation;

public class InputValidatorTests
{
    [Theory]
    [InlineData("valid-profile")]
    [InlineData("valid_profile")]
    [InlineData("valid.profile")]
    [InlineData("ValidProfile123")]
    [InlineData("a")]
    [InlineData("profile-with-123")]
    public void ValidateProfileName_WithValidNames_ReturnsValid(string profileName)
    {
        // Act
        var result = InputValidator.ValidateProfileName(profileName);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Value.Should().Be(profileName);
        result.ErrorMessage.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("profile with spaces")]
    [InlineData("profile@invalid")]
    [InlineData("profile!invalid")]
    [InlineData("profile#invalid")]
    [InlineData("../traversal")]
    [InlineData("..\\traversal")]
    [InlineData("very-long-profile-name-that-exceeds-the-maximum-length-limit-of-64-characters")]
    [InlineData("CON")] // Windows reserved name
    [InlineData("current")] // Application reserved name
    public void ValidateProfileName_WithInvalidNames_ReturnsInvalid(string? profileName)
    {
        // Act
        var result = InputValidator.ValidateProfileName(profileName);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("valid-alias")]
    [InlineData("valid_alias")]
    [InlineData("ValidAlias123")]
    [InlineData("a")]
    [InlineData("alias-123")]
    public void ValidateAliasName_WithValidNames_ReturnsValid(string aliasName)
    {
        // Act
        var result = InputValidator.ValidateAliasName(aliasName);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Value.Should().Be(aliasName);
        result.ErrorMessage.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("alias with spaces")]
    [InlineData("alias@invalid")]
    [InlineData("alias.invalid")] // Dots not allowed in aliases
    [InlineData("save")] // Command name conflict
    [InlineData("list")] // Command name conflict
    [InlineData("very-long-alias-name-that-exceeds-limit")]
    [InlineData("CON")] // Windows reserved name
    public void ValidateAliasName_WithInvalidNames_ReturnsInvalid(string? aliasName)
    {
        // Act
        var result = InputValidator.ValidateAliasName(aliasName);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("normal text")]
    [InlineData("some-valid-input")]
    [InlineData("ValidInput123")]
    [InlineData("")] // Empty is allowed for general input
    [InlineData(null)] // Null is allowed for general input
    public void ValidateUserInput_WithValidInput_ReturnsValid(string? input)
    {
        // Act
        var result = InputValidator.ValidateUserInput(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Value.Should().Be(input?.Trim() ?? string.Empty);
    }

    [Theory]
    [InlineData("../path/traversal")]
    [InlineData("..\\path\\traversal")]
    [InlineData("command; rm -rf /")]
    [InlineData("input | dangerous")]
    [InlineData("input && rm file")]
    [InlineData("'; DROP TABLE users; --")]
    [InlineData("SELECT * FROM users")]
    public void ValidateUserInput_WithDangerousInput_ReturnsInvalid(string input)
    {
        // Act
        var result = InputValidator.ValidateUserInput(input);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateUserInput_WithExcessiveLength_ReturnsInvalid()
    {
        // Arrange
        var longInput = new string('a', 1025); // Exceeds 1024 limit

        // Act
        var result = InputValidator.ValidateUserInput(longInput);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("exceeds maximum length");
    }

    [Theory]
    [InlineData("sk-ant-api03-abcdefg", "[REDACTED]")]
    [InlineData("Bearer token123", "[REDACTED]")]
    [InlineData("password123", "[REDACTED]")]
    [InlineData("normal text", "normal text")]
    [InlineData("", "[empty]")]
    [InlineData(null, "[empty]")]
    public void SanitizeForLogging_WithVariousInputs_ReturnsExpected(string? input, string expected)
    {
        // Act
        var result = InputValidator.SanitizeForLogging(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void SanitizeForLogging_WithLongString_TruncatesCorrectly()
    {
        // Arrange
        var longInput = new string('a', 150);

        // Act
        var result = InputValidator.SanitizeForLogging(longInput);

        // Assert
        result.Should().HaveLength(100); // 97 chars + "..."
        result.Should().EndWith("...");
    }

    [Fact]
    public void ValidationResult_ThrowIfInvalid_WithValidResult_DoesNotThrow()
    {
        // Arrange
        var validResult = ValidationResult.Valid("test");

        // Act & Assert
        var act = () => validResult.ThrowIfInvalid();
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidationResult_ThrowIfInvalid_WithInvalidResult_ThrowsException()
    {
        // Arrange
        var invalidResult = ValidationResult.Invalid("Test error");

        // Act & Assert
        var act = () => invalidResult.ThrowIfInvalid();
        act.Should().Throw<ArgumentException>().WithMessage("Test error");
    }

    [Theory]
    [InlineData("test ")]  // Trailing space
    [InlineData(" test")] // Leading space
    [InlineData(" test ")] // Both
    public void ValidateProfileName_TrimsWhitespace_ReturnsCleanValue(string profileName)
    {
        // Act
        var result = InputValidator.ValidateProfileName(profileName);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Value.Should().Be("test");
    }

    [Fact]
    public void ValidateProfileName_WithOnlyWhitespace_ReturnsInvalid()
    {
        // Act
        var result = InputValidator.ValidateProfileName("   ");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }
}