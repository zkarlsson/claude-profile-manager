using System.CommandLine;
using System.Runtime.Versioning;
using ClaudeProfileManager.Core.Validation;
using ClaudeProfileManager.Windows;

namespace ClaudeProfileManager.CLI.Commands;

[SupportedOSPlatform("windows")]
public class ProfileCommands
{
    private readonly WindowsProfileManager _profileManager;

    public ProfileCommands(WindowsProfileManager profileManager)
    {
        _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
    }

    public void ConfigureCommands(RootCommand rootCommand)
    {
        ConfigureSaveCommand(rootCommand);
        ConfigureListCommand(rootCommand);
        ConfigureSwitchCommand(rootCommand);
        ConfigureCurrentCommand(rootCommand);
        ConfigureDeleteCommand(rootCommand);
    }

    private void ConfigureSaveCommand(RootCommand rootCommand)
    {
        // Make profile name optional - will use current profile if not specified
        var profileNameArg = new Argument<string?>("profile-name", "Name of the profile to save (uses current if not specified)")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        var aliasesOption = new Option<string[]>("--aliases", "Optional aliases for the profile") { AllowMultipleArgumentsPerToken = true };
        aliasesOption.AddAlias("-a");

        var saveCommand = new Command("save", "Save current credentials as a profile");
        saveCommand.AddAlias("s");
        saveCommand.AddArgument(profileNameArg);
        saveCommand.AddOption(aliasesOption);

        saveCommand.SetHandler(async (string? profileName, string[] aliases) =>
        {
            try
            {
                // If no profile name provided, try to use current profile
                if (string.IsNullOrWhiteSpace(profileName))
                {
                    var currentProfile = await _profileManager.GetCurrentProfileAsync();
                    if (string.IsNullOrEmpty(currentProfile))
                    {
                        Console.WriteLine("✗ No profile name specified and no current profile active.");
                        Console.WriteLine("Usage: claude-profile-manager save <name> [--aliases ...]");
                        Environment.Exit(1);
                        return;
                    }
                    
                    profileName = currentProfile;
                    Console.WriteLine($"No profile name specified. Using current profile: {profileName}");
                    
                    // Check if profile exists and prompt for confirmation
                    var profiles = await _profileManager.ListProfilesAsync();
                    if (profiles.Any(p => p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase)))
                    {
                        Console.Write($"Profile '{profileName}' already exists. Overwrite existing credentials? [y/N]: ");
                        var response = Console.ReadLine()?.Trim().ToLowerInvariant();
                        if (response != "y" && response != "yes")
                        {
                            Console.WriteLine("Save cancelled.");
                            return;
                        }
                    }
                }

                // Validate profile name at CLI level for immediate user feedback
                var profileValidation = InputValidator.ValidateProfileName(profileName);
                if (!profileValidation.IsValid)
                {
                    Console.WriteLine($"✗ {profileValidation.ErrorMessage}");
                    Environment.Exit(1);
                    return;
                }

                Console.WriteLine($"Saving profile: {profileValidation.Value}");
                
                var success = await _profileManager.SaveProfileAsync(profileName, aliases);
                if (success)
                {
                    Console.WriteLine($"✓ Profile '{profileName}' saved successfully");
                    if (aliases?.Length > 0)
                    {
                        Console.WriteLine($"  Aliases: {string.Join(", ", aliases)}");
                    }
                }
                else
                {
                    Console.WriteLine($"✗ Failed to save profile '{profileName}'");
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error saving profile: {ex.Message}");
                Environment.Exit(1);
            }
        }, profileNameArg, aliasesOption);

        rootCommand.AddCommand(saveCommand);
    }

    private void ConfigureListCommand(RootCommand rootCommand)
    {
        var listCommand = new Command("list", "List all profiles with their status");
        listCommand.AddAlias("ls");

        listCommand.SetHandler(async () =>
        {
            try
            {
                var profiles = await _profileManager.ListProfilesAsync();
                var currentProfile = await _profileManager.GetCurrentProfileAsync();
                var aliases = await _profileManager.ListAliasesAsync();

                if (!profiles.Any())
                {
                    Console.WriteLine("No profiles found.");
                    return;
                }

                Console.WriteLine("Profiles:");
                Console.WriteLine();

                foreach (var profile in profiles.OrderBy(p => p.Name))
                {
                    var isCurrent = profile.Name == currentProfile;
                    var marker = isCurrent ? "* " : "  ";
                    
                    Console.WriteLine($"{marker}{profile.Name}");
                    Console.WriteLine($"    Authentication: {profile.AuthMethod}");
                    Console.WriteLine($"    Created: {profile.Created:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"    Last Used: {profile.LastUsed:yyyy-MM-dd HH:mm:ss}");

                    // Show aliases for this profile more efficiently
                    var profileAliases = new List<string>();
                    foreach (var kvp in aliases)
                    {
                        if (kvp.Value == profile.Name)
                        {
                            profileAliases.Add(kvp.Key);
                        }
                    }
                    if (profileAliases.Count > 0)
                    {
                        Console.WriteLine($"    Aliases: {string.Join(", ", profileAliases)}");
                    }

                    Console.WriteLine();
                }

                if (!string.IsNullOrEmpty(currentProfile))
                {
                    Console.WriteLine($"Current profile: {currentProfile}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error listing profiles: {ex.Message}");
                Environment.Exit(1);
            }
        });

        rootCommand.AddCommand(listCommand);
    }

    private void ConfigureSwitchCommand(RootCommand rootCommand)
    {
        var profileNameArg = new Argument<string>("profile-name", "Name or alias of the profile to switch to");

        var switchCommand = new Command("switch", "Switch to the specified profile");
        switchCommand.AddAlias("sw");
        switchCommand.AddArgument(profileNameArg);

        switchCommand.SetHandler(async (string profileName) =>
        {
            try
            {
                Console.WriteLine($"Switching to profile: {profileName}");
                
                var success = await _profileManager.SwitchToProfileAsync(profileName);
                if (success)
                {
                    var resolvedName = await _profileManager.ResolveAliasAsync(profileName);
                    Console.WriteLine($"✓ Switched to profile '{resolvedName}'");
                    Console.WriteLine();
                    Console.WriteLine("NOTE: Restart Claude Code to use the new credentials:");
                    Console.WriteLine("      Press Ctrl+D twice, then run: claude -c");
                }
                else
                {
                    Console.WriteLine($"✗ Failed to switch to profile '{profileName}'");
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error switching profile: {ex.Message}");
                Environment.Exit(1);
            }
        }, profileNameArg);

        rootCommand.AddCommand(switchCommand);
    }

    private void ConfigureCurrentCommand(RootCommand rootCommand)
    {
        var currentCommand = new Command("current", "Show the currently active profile");
        currentCommand.AddAlias("cur");

        currentCommand.SetHandler(async () =>
        {
            try
            {
                var currentProfile = await _profileManager.GetCurrentProfileAsync();
                if (string.IsNullOrEmpty(currentProfile))
                {
                    Console.WriteLine("No profile is currently active.");
                }
                else
                {
                    Console.WriteLine($"Current profile: {currentProfile}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error getting current profile: {ex.Message}");
                Environment.Exit(1);
            }
        });

        rootCommand.AddCommand(currentCommand);
    }

    private void ConfigureDeleteCommand(RootCommand rootCommand)
    {
        var profileNameArg = new Argument<string>("profile-name", "Name or alias of the profile to delete");
        var forceOption = new Option<bool>("--force", "Skip confirmation prompt");
        forceOption.AddAlias("-f");

        var deleteCommand = new Command("delete", "Delete a profile and its associated credentials");
        deleteCommand.AddAlias("del");
        deleteCommand.AddAlias("rm");
        deleteCommand.AddArgument(profileNameArg);
        deleteCommand.AddOption(forceOption);

        deleteCommand.SetHandler(async (string profileName, bool force) =>
        {
            try
            {
                var resolvedName = await _profileManager.ResolveAliasAsync(profileName);
                var profile = await _profileManager.GetProfileAsync(resolvedName);
                
                if (profile == null)
                {
                    Console.WriteLine($"✗ Profile '{profileName}' not found.");
                    Environment.Exit(1);
                    return;
                }

                if (!force)
                {
                    Console.Write($"Are you sure you want to delete profile '{resolvedName}'? [y/N]: ");
                    var response = Console.ReadLine()?.Trim().ToLowerInvariant();
                    if (response != "y" && response != "yes")
                    {
                        Console.WriteLine("Deletion cancelled.");
                        return;
                    }
                }

                Console.WriteLine($"Deleting profile: {resolvedName}");
                
                var success = await _profileManager.DeleteProfileAsync(profileName);
                if (success)
                {
                    Console.WriteLine($"✓ Profile '{resolvedName}' deleted successfully");
                }
                else
                {
                    Console.WriteLine($"✗ Failed to delete profile '{resolvedName}'");
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error deleting profile: {ex.Message}");
                Environment.Exit(1);
            }
        }, profileNameArg, forceOption);

        rootCommand.AddCommand(deleteCommand);
    }
}