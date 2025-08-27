using System.Text.Json.Serialization;

namespace ClaudeProfileManager.Core.Models;

/// <summary>
/// Represents a Claude authentication profile with metadata.
/// </summary>
public class Profile
{
    /// <summary>
    /// The unique name of the profile.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp when the profile was created (ISO 8601 format).
    /// </summary>
    [JsonPropertyName("created")]
    public DateTime Created { get; set; }

    /// <summary>
    /// The authentication method used by this profile.
    /// </summary>
    [JsonPropertyName("auth_method")]
    [JsonConverter(typeof(JsonStringEnumConverter<AuthMethod>))]
    public AuthMethod AuthMethod { get; set; }

    /// <summary>
    /// The timestamp when the profile was last used (ISO 8601 format).
    /// </summary>
    [JsonPropertyName("last_used")]
    public DateTime LastUsed { get; set; }

    /// <summary>
    /// Aliases associated with this profile.
    /// </summary>
    [JsonIgnore]
    public List<string> Aliases { get; set; } = new();

    /// <summary>
    /// The health status of the authentication token (for subscription profiles).
    /// </summary>
    [JsonIgnore]
    public string? TokenHealth { get; set; }

    /// <summary>
    /// Indicates whether this is the currently active profile.
    /// </summary>
    [JsonIgnore]
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Creates a new profile with the current timestamp.
    /// </summary>
    /// <param name="name">The profile name</param>
    /// <param name="authMethod">The authentication method</param>
    public Profile(string name, AuthMethod authMethod)
    {
        Name = name;
        AuthMethod = authMethod;
        Created = DateTime.UtcNow;
        LastUsed = DateTime.UtcNow;
    }

    /// <summary>
    /// Default constructor for JSON deserialization.
    /// </summary>
    public Profile()
    {
    }

    /// <summary>
    /// Updates the last used timestamp to the current time.
    /// </summary>
    public void UpdateLastUsed()
    {
        LastUsed = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets a formatted string representation of when the profile was created.
    /// </summary>
    public string GetFormattedCreatedDate()
    {
        return Created.ToString("MMM dd", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gets a formatted string representation of when the profile was last used.
    /// </summary>
    public string GetFormattedLastUsedDate()
    {
        return LastUsed.ToString("MMM dd", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gets the display name including aliases.
    /// </summary>
    public string GetDisplayName()
    {
        if (Aliases.Count == 0)
            return Name;

        var aliasString = string.Join(", ", Aliases);
        return $"{Name} ({aliasString})";
    }
}