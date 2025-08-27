using System.CommandLine;
using System.Runtime.Versioning;
using ClaudeProfileManager.CLI;
using ClaudeProfileManager.CLI.Commands;
using ClaudeProfileManager.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[SupportedOSPlatform("windows")]
internal sealed class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Configure services
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        // Initialize global exception handler
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var isDevelopment = Environment.GetEnvironmentVariable("CLAUDE_PROFILE_DEBUG") == "true";
        GlobalExceptionHandler.Initialize(logger, isDevelopment);

        try
        {
            // Check for direct profile switching (e.g., "claude-profile work")
            if (args.Length == 1 && !IsKnownCommand(args[0]))
            {
                return await HandleDirectProfileSwitch(args[0], serviceProvider);
            }

            // Create root command
            var rootCommand = new RootCommand("Claude Profile Manager - Windows Implementation");

            // Add profile management commands
            var profileCommands = new ProfileCommands(serviceProvider.GetRequiredService<WindowsProfileManager>());
            profileCommands.ConfigureCommands(rootCommand);

            // Add alias management commands
            var aliasCommands = new AliasCommands(serviceProvider.GetRequiredService<WindowsProfileManager>());
            aliasCommands.ConfigureCommands(rootCommand);

            // Execute command
            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            var userMessage = GlobalExceptionHandler.HandleException(ex, "Claude Profile Manager");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Error: {userMessage}");
            Console.ResetColor();
            return 1;
        }
    }

    private static bool IsKnownCommand(string arg)
    {
        // Known commands and their aliases
        var knownCommands = new[] { 
            "save", "s", 
            "list", "ls", 
            "switch", "sw", 
            "current", "cur", 
            "delete", "del", "rm", 
            "alias", 
            "aliases", 
            "unalias",
            "--help", "-h", "-?",
            "--version"
        };
        return knownCommands.Contains(arg.ToLowerInvariant());
    }

    private static async Task<int> HandleDirectProfileSwitch(string profileName, ServiceProvider serviceProvider)
    {
        try
        {
            var profileManager = serviceProvider.GetRequiredService<WindowsProfileManager>();
            
            Console.WriteLine($"Switching to profile: {profileName}");
            
            var success = await profileManager.SwitchToProfileAsync(profileName);
            if (success)
            {
                var resolvedName = await profileManager.ResolveAliasAsync(profileName);
                Console.WriteLine($"✓ Switched to profile '{resolvedName}'");
                Console.WriteLine();
                Console.WriteLine("NOTE: Restart Claude Code to use the new credentials:");
                Console.WriteLine("      Press Ctrl+D twice, then run: claude -c");
                return 0;
            }
            else
            {
                Console.WriteLine($"✗ Failed to switch to profile '{profileName}'");
                Console.WriteLine("  Use 'list' command to see available profiles");
                return 1;
            }
        }
        catch (Exception ex)
        {
            var userMessage = GlobalExceptionHandler.HandleException(ex, "Profile switching");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ Error switching profile: {userMessage}");
            Console.ResetColor();
            return 1;
        }
    }

    private static void ConfigureServices(ServiceCollection services)
    {
        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning); // Only show warnings and errors by default
        });

        // Register Windows implementations
        services.AddSingleton<WindowsCredentialStore>();
        services.AddSingleton<WindowsProfileFileManager>();
        services.AddSingleton<WindowsClaudeAuthDetector>();
        services.AddSingleton<WindowsProfileManager>();
    }
}
