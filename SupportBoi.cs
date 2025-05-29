using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
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
using Microsoft.Extensions.Hosting.Systemd;
using Tmds.Systemd;
using ServiceState = Tmds.Systemd.ServiceState;

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

    private static readonly Channel<PosixSignal> signalChannel = Channel.CreateUnbounded<PosixSignal>();

    private static void HandleSignal(PosixSignalContext context)
    {
        context.Cancel = true;
        signalChannel.Writer.TryWrite(context.Signal);
    }

    // ServiceManager will steal this value later so we have to copy it while we have the chance.
    private static readonly string systemdSocket = Environment.GetEnvironmentVariable("NOTIFY_SOCKET");

    private static async Task<int> Main(string[] args)
    {
        if (SystemdHelpers.IsSystemdService())
        {
            Journal.SyslogIdentifier = Assembly.GetEntryAssembly()?.GetName().Name;
            PosixSignalRegistration.Create(PosixSignal.SIGHUP, HandleSignal);
        }

        PosixSignalRegistration.Create(PosixSignal.SIGTERM, HandleSignal);
        PosixSignalRegistration.Create(PosixSignal.SIGINT, HandleSignal);

        StringWriter sw = new();
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
            return 0;
        }

        if (args.Contains("--version"))
        {
            Console.WriteLine(Assembly.GetEntryAssembly()?.GetName().Name + ' ' + GetVersion());
            Console.WriteLine("Build time: " + BuildInfo.BuildTimeUTC.ToString("yyyy-MM-dd HH:mm:ss") + " UTC");
            return 0;
        }

        Logger.Log("Starting " + Assembly.GetEntryAssembly()?.GetName().Name + " version " + GetVersion() + "...");
        try
        {
            if (!Reload())
            {
                Logger.Fatal("Aborting startup due to a fatal error when loading the configuration and setting up the database.");
                return 1;
            }

            // Create but don't start the timer, it will be started when the bot is connected.
            statusUpdateTimer = new Timer(RefreshBotActivity, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            if (!await Connect())
            {
                Logger.Fatal("Aborting startup due to a fatal error when trying to connect to Discord.");
                return 2;
            }

            ServiceManager.Notify(ServiceState.Ready);

            // Loop here until application closes, handle any signals received
            while (await signalChannel.Reader.WaitToReadAsync())
            {
                while (signalChannel.Reader.TryRead(out PosixSignal signal))
                {
                    switch (signal)
                    {
                        case PosixSignal.SIGHUP:
                            // Tmds.Systemd.ServiceManager doesn't support the notify-reload service type so we have to send the reloading message manually.
                            // According to the documentation this shouldn't be the right way to calculate MONOTONIC_USEC, but it works for some reason.
                            byte[] data = System.Text.Encoding.UTF8.GetBytes($"RELOADING=1\nMONOTONIC_USEC={DateTimeOffset.UtcNow.ToUnixTimeMicroseconds()}\n");
                            UnixDomainSocketEndPoint ep = new(systemdSocket);
                            using (Socket cl = new(AddressFamily.Unix, SocketType.Dgram, ProtocolType.Unspecified))
                            {
                                await cl.ConnectAsync(ep);
                                cl.Send(data);
                            }

                            Reload();
                            ServiceManager.Notify(ServiceState.Ready);
                            break;
                        case PosixSignal.SIGTERM:
                            Logger.Log("Shutting down...");
                            ServiceManager.Notify(ServiceState.Stopping);
                            await client.DisconnectAsync();
                            client.Dispose();
                            return 0;
                        case PosixSignal.SIGINT:
                            Logger.Warn("Received interrupt signal, shutting down...");
                            ServiceManager.Notify(ServiceState.Stopping);
                            await client.DisconnectAsync();
                            client.Dispose();
                            return 0;
                        default:
                            break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Logger.Fatal("Fatal error.", e);
            return 3;
        }

        return 0;
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