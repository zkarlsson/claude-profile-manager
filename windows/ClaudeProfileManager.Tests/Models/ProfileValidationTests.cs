using ClaudeProfileManager.Core.Models;
using FluentAssertions;

namespace ClaudeProfileManager.Tests.Models;

public class ProfileValidationTests
{
    [Theory]
    [InlineData("work", true)]
    [InlineData("personal", true)]
    [InlineData("my-profile", true)]
    [InlineData("profile_123", true)]
    [InlineData("Profile123", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(".hidden", false)]
    [InlineData("profile with spaces", false)]
    [InlineData("profile@invalid", false)]
    [InlineData("current", false)]
    [InlineData("aliases", false)]
    [InlineData("con", false)]
    [InlineData("aux", false)]
    public void ValidateProfileName_ShouldReturnExpectedResult(string profileName, bool expectedValid)
    {
        // Act
        var result = ProfileValidation.ValidateProfileName(profileName);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void ValidateProfileName_WithLongName_ShouldReturnInvalid()
    {
        // Arrange
        var longName = new string('a', ProfileValidation.MaxProfileNameLength + 1);

        // Act
        var result = ProfileValidation.ValidateProfileName(longName);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cannot exceed");
    }

    [Theory]
    [InlineData("sk-ant-api01-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", AuthMethod.Console, true)] // 108 chars total
    [InlineData("sk-ant-api01-short", AuthMethod.Console, false)]
    [InlineData("invalid-api-key", AuthMethod.Console, false)]
    [InlineData("{\"claudeAiOauth\":{\"accessToken\":\"token\"}}", AuthMethod.Subscription, true)]
    [InlineData("not-json", AuthMethod.Subscription, false)]
    [InlineData("{\"missingOauth\":true}", AuthMethod.Subscription, false)]
    public void ValidateCredentialFormat_ShouldReturnExpectedResult(string credential, AuthMethod authMethod, bool expectedValid)
    {
        // Act
        var result = ProfileValidation.ValidateCredentialFormat(credential, authMethod);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void ValidationResult_ImplicitOperator_ShouldWork()
    {
        // Arrange
        var successResult = ValidationResult.Success();
        var failureResult = ValidationResult.Failure("Error");

        // Act & Assert
        ((bool)successResult).Should().BeTrue();
        ((bool)failureResult).Should().BeFalse();
    }
}