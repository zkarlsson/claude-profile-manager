using System.CommandLine;
using System.Runtime.Versioning;
using ClaudeProfileManager.Windows;

namespace ClaudeProfileManager.CLI.Commands;

[SupportedOSPlatform("windows")]
public class AliasCommands
{
    private readonly WindowsProfileManager _profileManager;

    public AliasCommands(WindowsProfileManager profileManager)
    {
        _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
    }

    public void ConfigureCommands(RootCommand rootCommand)
    {
        ConfigureAliasCommand(rootCommand);
        ConfigureAliasesCommand(rootCommand);
        ConfigureUnaliasCommand(rootCommand);
    }

    private void ConfigureAliasCommand(RootCommand rootCommand)
    {
        var aliasNameArg = new Argument<string>("alias-name", "Name of the alias to create");
        var profileNameArg = new Argument<string>("profile-name", "Name of the target profile");

        var aliasCommand = new Command("alias", "Create an alias for an existing profile");
        aliasCommand.AddArgument(aliasNameArg);
        aliasCommand.AddArgument(profileNameArg);

        aliasCommand.SetHandler(async (string aliasName, string profileName) =>
        {
            try
            {
                Console.WriteLine($"Creating alias '{aliasName}' for profile '{profileName}'");
                
                var success = await _profileManager.AddAliasAsync(aliasName, profileName);
                if (success)
                {
                    Console.WriteLine($"✓ Alias '{aliasName}' created successfully");
                }
                else
                {
                    Console.WriteLine($"✗ Failed to create alias '{aliasName}'");
                    Console.WriteLine("  Possible reasons:");
                    Console.WriteLine("  - Target profile does not exist");
                    Console.WriteLine("  - Alias name already exists");
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error creating alias: {ex.Message}");
                Environment.Exit(1);
            }
        }, aliasNameArg, profileNameArg);

        rootCommand.AddCommand(aliasCommand);
    }

    private void ConfigureAliasesCommand(RootCommand rootCommand)
    {
        var aliasesCommand = new Command("aliases", "List all defined aliases");

        aliasesCommand.SetHandler(async () =>
        {
            try
            {
                var aliases = await _profileManager.ListAliasesAsync();

                if (aliases.Count == 0)
                {
                    Console.WriteLine("No aliases defined.");
                    return;
                }

                Console.WriteLine("Aliases:");
                Console.WriteLine();

                var maxAliasLength = aliases.Keys.Max(k => k.Length);
                foreach (var alias in aliases.OrderBy(kv => kv.Key))
                {
                    var padding = new string(' ', maxAliasLength - alias.Key.Length);
                    Console.WriteLine($"  {alias.Key}{padding} -> {alias.Value}");
                }

                Console.WriteLine();
                Console.WriteLine($"Total: {aliases.Count} alias(es)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error listing aliases: {ex.Message}");
                Environment.Exit(1);
            }
        });

        rootCommand.AddCommand(aliasesCommand);
    }

    private void ConfigureUnaliasCommand(RootCommand rootCommand)
    {
        var aliasNameArg = new Argument<string>("alias-name", "Name of the alias to remove");
        var forceOption = new Option<bool>("--force", "Skip confirmation prompt");
        forceOption.AddAlias("-f");

        var unaliasCommand = new Command("unalias", "Remove an alias");
        unaliasCommand.AddArgument(aliasNameArg);
        unaliasCommand.AddOption(forceOption);

        unaliasCommand.SetHandler(async (string aliasName, bool force) =>
        {
            try
            {
                // Check if alias exists
                var aliases = await _profileManager.ListAliasesAsync();
                if (!aliases.TryGetValue(aliasName, out var targetProfile))
                {
                    Console.WriteLine($"✗ Alias '{aliasName}' not found.");
                    Environment.Exit(1);
                    return;
                }

                if (!force)
                {
                    Console.Write($"Remove alias '{aliasName}' (points to '{targetProfile}')? [y/N]: ");
                    var response = Console.ReadLine()?.Trim().ToLowerInvariant();
                    if (response != "y" && response != "yes")
                    {
                        Console.WriteLine("Removal cancelled.");
                        return;
                    }
                }

                Console.WriteLine($"Removing alias: {aliasName}");
                
                var success = await _profileManager.RemoveAliasAsync(aliasName);
                if (success)
                {
                    Console.WriteLine($"✓ Alias '{aliasName}' removed successfully");
                }
                else
                {
                    Console.WriteLine($"✗ Failed to remove alias '{aliasName}'");
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error removing alias: {ex.Message}");
                Environment.Exit(1);
            }
        }, aliasNameArg, forceOption);

        rootCommand.AddCommand(unaliasCommand);
    }
}