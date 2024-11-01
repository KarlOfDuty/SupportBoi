﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Commands;
using Microsoft.Extensions.Logging;
using SupportBoi.Commands;
using CommandLine;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.EventArgs;
using DSharpPlus.Commands.Exceptions;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.TextCommands.Parsing;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace SupportBoi;

internal static class SupportBoi
{
    internal static DiscordClient client = null;
    public class CommandLineArguments
    {
        [CommandLine.Option('c',
                            "config",
                            Required = false,
                            HelpText = "Select a config file to use.",
                            Default = "config.yml",
                            MetaValue = "PATH")]
        public string configPath { get; set; }

        [CommandLine.Option('t',
                            "transcripts",
                            Required = false,
                            HelpText = "Select directory to store transcripts in.",
                            Default = "./transcripts",
                            MetaValue = "PATH")]
        public string transcriptDir { get; set; }

        [CommandLine.Option(
            "leave",
            Required = false,
            HelpText = "Leaves one or more Discord servers. " +
                       "You can check which servers your bot is in when it starts up.",
            MetaValue = "ID,ID,ID...",
            Separator = ','
        )]
        public IEnumerable<ulong> serversToLeave { get; set; }
    }

    internal static CommandLineArguments commandLineArgs;

    private static void Main(string[] args)
    {
        StringWriter sw = new StringWriter();
        commandLineArgs = new Parser(settings =>
        {
            settings.AutoHelp = true;
            settings.HelpWriter = sw;
            settings.AutoVersion = false;
        }).ParseArguments<CommandLineArguments>(args).Value;

        // CommandLineParser has some bugs related to the built-in version option, ignore the output if it isn't found.
        if (!sw.ToString().Contains("Option 'version' is unknown."))
        {
            Console.Write(sw);
        }

        if (args.Contains("--help"))
        {
            return;
        }

        if (args.Contains("--version"))
        {
            Console.WriteLine(Assembly.GetEntryAssembly()?.GetName().Name + ' ' + GetVersion());
            Console.WriteLine("Build time: " + BuildInfo.BuildTimeUTC.ToString("yyyy-MM-dd HH:mm:ss") + " UTC");
            return;
        }

        MainAsync().GetAwaiter().GetResult();
    }

    private static async Task MainAsync()
    {
        Logger.Log("Starting " + Assembly.GetEntryAssembly()?.GetName().Name + " version " + GetVersion() + "...");
        try
        {
            Reload();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }
        catch (Exception e)
        {
            Logger.Fatal("Fatal error:\n" + e);
            Console.ReadLine();
        }
    }

    public static string GetVersion()
    {
        Version version = Assembly.GetEntryAssembly()?.GetName().Version;
        return version?.Major + "."
             + version?.Minor + "."
             + version?.Build
             + (version?.Revision == 0 ? "" : "-" + (char)(64 + version?.Revision ?? 0))
             + " (" + ThisAssembly.Git.Commit + ")";
    }

    public static async void Reload()
    {
        if (client != null)
        {
            await client.DisconnectAsync();
            client.Dispose();
        }

        Config.LoadConfig();

        // Check if token is unset
        if (Config.token is "<add-token-here>" or "")
        {
            Logger.Fatal("You need to set your bot token in the config and start the bot again.");
            throw new ArgumentException("Invalid Discord bot token");
        }

        // Database connection and setup
        try
        {
            Logger.Log("Connecting to database... (" + Config.hostName + ":" + Config.port + ")");
            Database.SetConnectionString(Config.hostName, Config.port, Config.database, Config.username, Config.password);
            Database.SetupTables();
        }
        catch (Exception e)
        {
            Logger.Fatal("Could not set up database tables, please confirm connection settings, status of the server and permissions of MySQL user. Error: " + e);
            throw;
        }

        Logger.Log("Setting up Discord client...");
        DiscordClientBuilder clientBuilder = DiscordClientBuilder.CreateDefault(Config.token, DiscordIntents.All)
            .SetReconnectOnFatalGatewayErrors()
            .ConfigureServices(configure =>
            {
                configure.AddSingleton<IClientErrorHandler>(new ErrorHandler());
            })
            .ConfigureEventHandlers(builder =>
            {
                builder.HandleGuildDownloadCompleted(EventHandler.OnReady);
                builder.HandleGuildAvailable(EventHandler.OnGuildAvailable);
                builder.HandleMessageCreated(EventHandler.OnMessageCreated);
                builder.HandleGuildMemberAdded(EventHandler.OnMemberAdded);
                builder.HandleGuildMemberRemoved(EventHandler.OnMemberRemoved);
                builder.HandleComponentInteractionCreated(EventHandler.OnComponentInteractionCreated);
            })
            .UseInteractivity(new InteractivityConfiguration
            {
                PaginationBehaviour = PaginationBehaviour.Ignore,
                PaginationDeletion = PaginationDeletion.DeleteMessage,
                Timeout = TimeSpan.FromMinutes(15)
            })
            .UseCommands((_, extension) =>
            {
                extension.AddCommands(
                [
                    typeof(AddCategoryCommand),
                    typeof(AddCommand),
                    typeof(AddMessageCommand),
                    typeof(AddStaffCommand),
                    typeof(AssignCommand),
                    typeof(BlacklistCommand),
                    typeof(CloseCommand),
                    typeof(CreateButtonPanelCommand),
                    typeof(CreateSelectionBoxPanelCommand),
                    typeof(ListAssignedCommand),
                    typeof(ListCommand),
                    typeof(ListOpen),
                    typeof(ListUnassignedCommand),
                    typeof(MoveCommand),
                    typeof(NewCommand),
                    typeof(RandomAssignCommand),
                    typeof(RemoveCategoryCommand),
                    typeof(RemoveMessageCommand),
                    typeof(RemoveStaffCommand),
                    typeof(SayCommand),
                    typeof(SetSummaryCommand),
                    typeof(StatusCommand),
                    typeof(SummaryCommand),
                    typeof(ToggleActiveCommand),
                    typeof(TranscriptCommand),
                    typeof(UnassignCommand),
                    typeof(UnblacklistCommand),
                    typeof(AdminCommands)
                ]);
                extension.AddProcessor(new SlashCommandProcessor());
                extension.CommandErrored += EventHandler.OnCommandError;
            }, new CommandsConfiguration()
            {
                RegisterDefaultCommandProcessors = false,
                UseDefaultCommandErrorHandler = false
            })
            .ConfigureExtraFeatures(clientConfig =>
            {
                clientConfig.LogUnknownEvents = false;
                clientConfig.LogUnknownAuditlogs = false;
            })
            .ConfigureLogging(config =>
            {
                config.AddProvider(new LogTestFactory());
            });

        client = clientBuilder.Build();

        Logger.Log("Connecting to Discord...");
        await client.ConnectAsync();
    }
}

internal class ErrorHandler : IClientErrorHandler
{
    public ValueTask HandleEventHandlerError(string name, Exception exception, Delegate invokedDelegate, object sender, object args)
    {
        Logger.Error("Client exception occured:\n" + exception);
        switch (exception)
        {
            case BadRequestException ex:
                Logger.Error("JSON Message: " + ex.JsonMessage);
                break;
            default:
                break;
        }
        return ValueTask.FromException(exception);
    }

    public ValueTask HandleGatewayError(Exception exception)
    {
        Logger.Error("A gateway error occured:\n" + exception);
        return ValueTask.FromException(exception);
    }
}