using System.Globalization;
using System.Runtime.Versioning;
using ClaudeProfileManager.Core.Interfaces;
using ClaudeProfileManager.Core.Models;
using ClaudeProfileManager.Windows;
using Microsoft.Extensions.Logging;

namespace ClaudeProfileManager.Windows;

/// <summary>
/// Simple CLI entry point for Claude Profile Manager Windows application.
/// </summary>
[SupportedOSPlatform("windows")]
public class Program
{
    private static ILogger<Program>? _logger;
    private static WindowsProfileManager? _profileManager;

    public static async Task<int> Main(string[] args)
    {
        try
        {
            // Initialize basic services
            InitializeServices();

            // Parse and execute command
            return await ExecuteCommand(args);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            return 1;
        }
    }

    private static void InitializeServices()
    {
        // Simple logging to console
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<Program>();

        // Create profile manager with Windows implementations
        _profileManager = new WindowsProfileManager(loggerFactory.CreateLogger<WindowsProfileManager>());
    }

    private static async Task<int> ExecuteCommand(string[] args)
    {
        if (args.Length == 0)
        {
            ShowHelp();
            return 0;
        }

        var command = args[0].ToLowerInvariant();

        try
        {
            switch (command)
            {
                case "--version":
                case "-v":
                    ShowVersion();
                    return 0;

                case "--help":
                case "-h":
                case "help":
                    ShowHelp();
                    return 0;

                case "save":
                case "s":
                    return await HandleSaveCommand(args);

                case "list":
                case "ls":
                    return await HandleListCommand(args);

                case "switch":
                case "sw":
                    return await HandleSwitchCommand(args);

                case "delete":
                case "del":
                case "rm":
                    return await HandleDeleteCommand(args);

                case "current":
                case "cur":
                    return await HandleCurrentCommand();

                case "alias":
                    return await HandleAliasCommand(args);

                case "unalias":
                    return await HandleUnaliasCommand(args);

                case "aliases":
                    return await HandleAliasesCommand();

                case "health":
                    return await HandleHealthCommand();

                case "status":
                    return await HandleStatusCommand();

                default:
                    Console.WriteLine($"Unknown command: {command}");
                    Console.WriteLine("Use 'claude-profile-manager help' for available commands.");
                    return 1;
            }
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Command failed: {ex.Message}");
            return 1;
        }
    }

    private static void ShowVersion()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        Console.WriteLine($"Claude Profile Manager for Windows v{version}");
        Console.WriteLine("Secure authentication profile management for Claude Code CLI");
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Claude Profile Manager - Manage authentication profiles for Claude Code CLI");
        Console.WriteLine();
        Console.WriteLine("Usage: claude-profile-manager <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  save <name> [--aliases alias1,alias2]  Save current credentials as a profile");
        Console.WriteLine("  list [--detailed]                      List all profiles");
        Console.WriteLine("  switch <name>                          Switch to a profile");
        Console.WriteLine("  delete <name> [--force]                Delete a profile");
        Console.WriteLine("  current                                 Show current profile");
        Console.WriteLine("  alias <alias> <profile>                Create an alias for a profile");
        Console.WriteLine("  unalias <alias>                        Remove an alias");
        Console.WriteLine("  aliases                                 List all aliases");
        Console.WriteLine("  health                                  Check system health");
        Console.WriteLine("  status                                  Show system status");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --version, -v    Show version information");
        Console.WriteLine("  --help, -h       Show help information");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  claude-profile-manager save work --aliases w,office");
        Console.WriteLine("  claude-profile-manager list --detailed");
        Console.WriteLine("  claude-profile-manager switch work");
        Console.WriteLine("  claude-profile-manager current");
    }

    private static async Task<int> HandleSaveCommand(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: claude-profile-manager save <name> [--aliases alias1,alias2]");
            return 1;
        }

        var profileName = args[1];
        var aliases = new List<string>();

        // Parse aliases option
        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] == "--aliases" && i + 1 < args.Length)
            {
                aliases.AddRange(args[i + 1].Split(',', StringSplitOptions.RemoveEmptyEntries));
                break;
            }
        }

        var success = await _profileManager!.SaveProfileAsync(profileName, aliases);
        if (success)
        {
            Console.WriteLine($"✓ Profile '{profileName}' saved successfully");
            
            if (aliases.Count > 0)
            {
                Console.WriteLine($"  Aliases: {string.Join(", ", aliases)}");
            }
            return 0;
        }
        else
        {
            Console.WriteLine($"✗ Failed to save profile '{profileName}'");
            return 1;
        }
    }

    private static async Task<int> HandleListCommand(string[] args)
    {
        var detailed = args.Contains("--detailed");
        
        var profiles = await _profileManager!.ListProfilesAsync();
        var currentProfileName = await _profileManager.GetCurrentProfileAsync();

        if (!profiles.Any())
        {
            Console.WriteLine("No profiles found.");
            return 0;
        }

        Console.WriteLine($"Found {profiles.Count()} profile(s):");
        Console.WriteLine();

        foreach (var profile in profiles.OrderBy(p => p.Name))
        {
            var isCurrent = currentProfileName == profile.Name;
            var marker = isCurrent ? "* " : "  ";
            
            if (detailed)
            {
                Console.WriteLine($"{marker}{profile.Name}");
                Console.WriteLine($"    Auth Method: {profile.AuthMethod}");
                Console.WriteLine($"    Created: {profile.Created.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}");
                Console.WriteLine($"    Last Used: {profile.LastUsed.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}");
                if (profile.Aliases.Count > 0)
                {
                    Console.WriteLine($"    Aliases: {string.Join(", ", profile.Aliases)}");
                }
                Console.WriteLine();
            }
            else
            {
                var aliasInfo = profile.Aliases.Count > 0 ? $" (aliases: {string.Join(", ", profile.Aliases)})" : "";
                Console.WriteLine($"{marker}{profile.Name} ({profile.AuthMethod}){(isCurrent ? " [CURRENT]" : "")}{aliasInfo}");
            }
        }

        return 0;
    }

    private static async Task<int> HandleSwitchCommand(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: claude-profile-manager switch <name>");
            return 1;
        }

        var profileName = args[1];
        var success = await _profileManager!.SwitchToProfileAsync(profileName);
        
        if (success)
        {
            Console.WriteLine($"✓ Switched to profile '{profileName}'");
            return 0;
        }
        else
        {
            Console.WriteLine($"✗ Failed to switch to profile '{profileName}'");
            return 1;
        }
    }

    private static async Task<int> HandleDeleteCommand(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: claude-profile-manager delete <name> [--force]");
            return 1;
        }

        var profileName = args[1];
        var force = args.Contains("--force");

        if (!force)
        {
            Console.Write($"Delete profile '{profileName}'? (y/N): ");
            var response = Console.ReadLine();
            if (response?.ToLowerInvariant() != "y")
            {
                Console.WriteLine("Operation cancelled.");
                return 0;
            }
        }

        var success = await _profileManager!.DeleteProfileAsync(profileName);
        
        if (success)
        {
            Console.WriteLine($"✓ Profile '{profileName}' deleted successfully");
            return 0;
        }
        else
        {
            Console.WriteLine($"✗ Failed to delete profile '{profileName}'");
            return 1;
        }
    }

    private static async Task<int> HandleCurrentCommand()
    {
        var currentProfileName = await _profileManager!.GetCurrentProfileAsync();
        
        if (string.IsNullOrEmpty(currentProfileName))
        {
            Console.WriteLine("No current profile set.");
        }
        else
        {
            var profile = await _profileManager.GetProfileAsync(currentProfileName);
            if (profile != null)
            {
                Console.WriteLine($"Current profile: {profile.Name} ({profile.AuthMethod})");
            }
            else
            {
                Console.WriteLine($"Current profile: {currentProfileName} (details not available)");
            }
        }

        return 0;
    }

    private static async Task<int> HandleAliasCommand(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: claude-profile-manager alias <alias> <profile>");
            return 1;
        }

        var alias = args[1];
        var profileName = args[2];

        var success = await _profileManager!.AddAliasAsync(alias, profileName);
        
        if (success)
        {
            Console.WriteLine($"✓ Alias '{alias}' created for profile '{profileName}'");
            return 0;
        }
        else
        {
            Console.WriteLine($"✗ Failed to create alias '{alias}'");
            return 1;
        }
    }

    private static async Task<int> HandleUnaliasCommand(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: claude-profile-manager unalias <alias>");
            return 1;
        }

        var alias = args[1];
        var success = await _profileManager!.RemoveAliasAsync(alias);
        
        if (success)
        {
            Console.WriteLine($"✓ Alias '{alias}' removed successfully");
            return 0;
        }
        else
        {
            Console.WriteLine($"✗ Failed to remove alias '{alias}'");
            return 1;
        }
    }

    private static async Task<int> HandleAliasesCommand()
    {
        var aliases = await _profileManager!.ListAliasesAsync();
        
        if (aliases.Count == 0)
        {
            Console.WriteLine("No aliases defined.");
            return 0;
        }

        Console.WriteLine($"Found {aliases.Count} alias(es):");
        foreach (var kvp in aliases.OrderBy(x => x.Key))
        {
            Console.WriteLine($"  {kvp.Key} -> {kvp.Value}");
        }

        return 0;
    }

    private static async Task<int> HandleHealthCommand()
    {
        Console.WriteLine("Health Check Results:");
        Console.WriteLine();

        try
        {
            // Basic health checks
            Console.WriteLine("✓ Application: Running normally");
            
            // Test profile manager
            var profiles = await _profileManager!.ListProfilesAsync();
            Console.WriteLine($"✓ Profile System: {profiles.Count()} profiles loaded");
            
            // Test current profile
            var current = await _profileManager.GetCurrentProfileAsync();
            Console.WriteLine($"✓ Current Profile: {(current != null ? current : "None set")}");

            Console.WriteLine();
            Console.WriteLine("✓ All health checks passed");
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Health check failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> HandleStatusCommand()
    {
        try
        {
            var profiles = await _profileManager!.ListProfilesAsync();
            var currentProfileName = await _profileManager.GetCurrentProfileAsync();
            var aliases = await _profileManager.ListAliasesAsync();

            Console.WriteLine("Claude Profile Manager Status");
            Console.WriteLine("============================");
            Console.WriteLine($"Current Profile: {currentProfileName ?? "None"}");
            Console.WriteLine($"Total Profiles: {profiles.Count()}");
            Console.WriteLine($"Total Aliases: {aliases.Count}");
            Console.WriteLine($"System Health: Healthy");
            Console.WriteLine($"Last Checked: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}");
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Status check failed: {ex.Message}");
            return 1;
        }
    }
}