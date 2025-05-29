using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Commands;
using Microsoft.Extensions.Logging;
using SupportBoi.Commands;
using CommandLine;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Tmds.Systemd;

namespace SupportBoi;

internal static class SupportBoi
{
    internal static DiscordClient client = null;

    private static Timer statusUpdateTimer;

    public class CommandLineArguments
    {
        [Option('c',
                "config",
                Required = false,
                HelpText = "Select a config file to use.",
                MetaValue = "PATH")]
        public string configPath { get; set; }

        [Option('t',
                "transcripts",
                Required = false,
                HelpText = "Select directory to store transcripts in.",
                MetaValue = "PATH")]
        public string transcriptDir { get; set; }

        [Option('l',
            "log-file",
            Required = false,
            HelpText = "Select log file to write bot logs to.",
            MetaValue = "PATH")]
        public string logFilePath { get; set; }

        [Option("leave",
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

        Journal.SyslogIdentifier = Assembly.GetEntryAssembly()?.GetName().Name;
        MainAsync().GetAwaiter().GetResult();
    }

    private static async Task MainAsync()
    {
        Logger.Log("Starting " + Assembly.GetEntryAssembly()?.GetName().Name + " version " + GetVersion() + "...");
        try
        {
            if (!Reload())
            {
                Logger.Fatal("Aborting startup due to a fatal error.");
                Environment.ExitCode = 1;
                return;
            }

            // Create but don't start the timer, it will be started when the bot is connected.
            statusUpdateTimer = new Timer(RefreshBotActivity, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            if (!await Connect())
            {
                Logger.Fatal("Aborting startup due to a fatal error when trying to connect to Discord.");
                Environment.ExitCode = 2;
                return;
            }

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }
        catch (Exception e)
        {
            Logger.Fatal("Fatal error.", e);
            Environment.ExitCode = 3;
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

    public static bool Reload()
    {
        try
        {
            Config.LoadConfig();
        }
        catch (Exception e)
        {
            Logger.Fatal("Unable to read the config file: \"" + Config.ConfigPath + "\"", e);
            return false;
        }

        // Check if token is unset
        if (Config.token is "<add-token-here>" or "")
        {
            Logger.Fatal("You need to set your bot token in the config and start the bot again.");
            return false;
        }

        // Database connection and setup
        try
        {
            Logger.Log("Connecting to database. (" + Config.hostName + ":" + Config.port + ")");
            Database.Connection.SetConnectionString(Config.hostName, Config.port, Config.database, Config.username, Config.password);
            Database.Connection.SetupTables();
        }
        catch (Exception e)
        {
            Logger.Fatal("Could not set up database tables, please confirm connection settings, status of the server and permissions of MySQL user.", e);
            return false;
        }

        return true;
    }

    private static async Task<bool> Connect()
    {
        Logger.Log("Setting up Discord client.");
        DiscordClientBuilder clientBuilder = DiscordClientBuilder.CreateDefault(Config.token, DiscordIntents.All)
                                                                 .SetReconnectOnFatalGatewayErrors();

        clientBuilder.ConfigureServices(configure =>
        {
            configure.AddSingleton<IClientErrorHandler>(new ErrorHandler());
        });

        clientBuilder.ConfigureEventHandlers(builder =>
        {
            builder.HandleGuildDownloadCompleted(EventHandler.OnReady);
            builder.HandleGuildAvailable(EventHandler.OnGuildAvailable);
            builder.HandleMessageCreated(EventHandler.OnMessageCreated);
            builder.HandleGuildMemberAdded(EventHandler.OnMemberAdded);
            builder.HandleGuildMemberRemoved(EventHandler.OnMemberRemoved);
            builder.HandleComponentInteractionCreated(EventHandler.OnComponentInteractionCreated);
        });

        clientBuilder.UseInteractivity(new InteractivityConfiguration
        {
            PaginationBehaviour = PaginationBehaviour.Ignore,
            PaginationDeletion = PaginationDeletion.DeleteMessage,
            Timeout = TimeSpan.FromMinutes(15)
        });

        clientBuilder.UseCommands((_, extension) =>
        {
            extension.AddCommands(
            [
                typeof(AddCategoryCommand),
                typeof(AddCommand),
                typeof(AddStaffCommand),
                typeof(AdminCommands),
                typeof(AssignCommand),
                typeof(BlacklistCommand),
                typeof(CloseCommand),
                typeof(CreateButtonPanelCommand),
                typeof(CreateSelectionBoxPanelCommand),
                typeof(InterviewCommands),
                typeof(InterviewTemplateCommands),
                typeof(ListAssignedCommand),
                typeof(ListCommand),
                typeof(ListInvalidCommand),
                typeof(ListOpen),
                typeof(ListUnassignedCommand),
                typeof(MoveCommand),
                typeof(NewCommand),
                typeof(RandomAssignCommand),
                typeof(RemoveCategoryCommand),
                typeof(RemoveStaffCommand),
                typeof(SayCommand),
                typeof(SetMessageCommand),
                typeof(SetSummaryCommand),
                typeof(StatusCommand),
                typeof(SummaryCommand),
                typeof(ToggleActiveCommand),
                typeof(TranscriptCommand),
                typeof(UnassignCommand),
                typeof(UnblacklistCommand),
            ]);
            extension.AddProcessor(new SlashCommandProcessor());
            extension.CommandErrored += EventHandler.OnCommandError;
        }, new CommandsConfiguration
        {
            RegisterDefaultCommandProcessors = false,
            UseDefaultCommandErrorHandler = false
        });

        clientBuilder.ConfigureExtraFeatures(clientConfig =>
        {
            clientConfig.LogUnknownEvents = false;
            clientConfig.LogUnknownAuditlogs = false;
        });

        clientBuilder.ConfigureLogging(config =>
        {
            config.AddProvider(new LoggerProvider());
        });

        client = clientBuilder.Build();

        Logger.Log("Connecting to Discord.");
        EventHandler.hasLoggedGuilds = false;

        try
        {
            await client.ConnectAsync();
        }
        catch (Exception e)
        {
            Logger.Fatal("Error occured while connecting to Discord.", e);
            return false;
        }

        return true;
    }

    internal static void RefreshBotActivity(object state = null)
    {
        try
        {
            if (!Enum.TryParse(Config.presenceType, true, out DiscordActivityType activityType))
            {
                Logger.Log("Presence type '" + Config.presenceType + "' invalid, using 'Playing' instead.");
                activityType = DiscordActivityType.Playing;
            }

            client.UpdateStatusAsync(new DiscordActivity(Config.presenceText, activityType), DiscordUserStatus.Online);
        }
        finally
        {
            statusUpdateTimer.Change(TimeSpan.FromMinutes(1), Timeout.InfiniteTimeSpan);
        }
    }
}

internal class ErrorHandler : IClientErrorHandler
{
    public ValueTask HandleEventHandlerError(string name,
                                             Exception exception,
                                             Delegate invokedDelegate,
                                             object sender,
                                             object args)
    {
        Logger.Error("Client exception occured:\n" + exception);
        if (exception is BadRequestException ex)
        {
            Logger.Error("JSON Message: " + ex.JsonMessage);
        }

        return ValueTask.FromException(exception);
    }

    public ValueTask HandleGatewayError(Exception exception)
    {
        Logger.Error("A gateway error occured:\n" + exception);
        return ValueTask.FromException(exception);
    }
}