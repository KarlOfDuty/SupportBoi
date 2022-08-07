using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using SupportBoi.Commands;

namespace SupportBoi
{
	internal static class SupportBoi
	{
		// Sets up a dummy client to use for logging
		public static DiscordClient discordClient = new DiscordClient(new DiscordConfiguration { Token = "DUMMY_TOKEN", TokenType = TokenType.Bot, MinimumLogLevel = LogLevel.Debug });
		private static SlashCommandsExtension commands = null;

		static void Main(string[] _)
		{
			MainAsync().GetAwaiter().GetResult();
		}

		private static async Task MainAsync()
		{
			Logger.Log("Starting " + Assembly.GetEntryAssembly().GetName().Name + " version " + GetVersion() + "...");
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
			return version?.Major + "." + version?.Minor + "." + version?.Build + (version?.Revision == 0 ? "" : "-" + (char)(64 + version?.Revision ?? 0));
		}

		public static async void Reload()
		{
			if (discordClient != null)
			{
				await discordClient.DisconnectAsync();
				discordClient.Dispose();
				Logger.Log("Discord client disconnected.");
			}
			
			Logger.Log("Loading config \"" + Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "config.yml\"");
			Config.LoadConfig();
			
			// Check if token is unset
			if (Config.token == "<add-token-here>" || Config.token == "")
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
			
			// Checking log level
			if (!Enum.TryParse(Config.logLevel, true, out LogLevel logLevel))
			{
				Logger.Warn("Log level '" + Config.logLevel + "' invalid, using 'Information' instead.");
				logLevel = LogLevel.Information;
			}
			
			// Setting up client configuration
			DiscordConfiguration cfg = new DiscordConfiguration
			{
				Token = Config.token,
				TokenType = TokenType.Bot,
				MinimumLogLevel = logLevel,
				AutoReconnect = true,
				Intents = DiscordIntents.All
			};
			
			discordClient = new DiscordClient(cfg);
			
			Logger.Log("Hooking events...");
			discordClient.Ready += EventHandler.OnReady;
			discordClient.GuildAvailable += EventHandler.OnGuildAvailable;
			discordClient.ClientErrored += EventHandler.OnClientError;
			discordClient.MessageCreated += EventHandler.OnMessageCreated;
			discordClient.GuildMemberAdded += EventHandler.OnMemberAdded;
			discordClient.GuildMemberRemoved += EventHandler.OnMemberRemoved;
			discordClient.ComponentInteractionCreated += EventHandler.OnComponentInteractionCreated;
			Logger.Log("Registering commands...");
			commands = discordClient.UseSlashCommands();

			commands.RegisterCommands<AddCategoryCommand>();
			commands.RegisterCommands<AddCommand>();
			commands.RegisterCommands<AddMessageCommand>();
			commands.RegisterCommands<AddStaffCommand>();
			commands.RegisterCommands<AssignCommand>();
			commands.RegisterCommands<BlacklistCommand>();
			commands.RegisterCommands<CloseCommand>();
			commands.RegisterCommands<CreateSelectionBoxPanelCommand>();
			commands.RegisterCommands<ListAssignedCommand>();
			commands.RegisterCommands<ListCommand>();
			commands.RegisterCommands<ListOldestCommand>();
			commands.RegisterCommands<ListUnassignedCommand>();
			commands.RegisterCommands<MoveCommand>();
			commands.RegisterCommands<NewCommand>();
			commands.RegisterCommands<RandomAssignCommand>();
			commands.RegisterCommands<ReloadCommand>();
			commands.RegisterCommands<RemoveCategoryCommand>();
			commands.RegisterCommands<RemoveMessageCommand>();
			commands.RegisterCommands<RemoveStaffCommand>();
			commands.RegisterCommands<SayCommand>();
			commands.RegisterCommands<SetSummaryCommand>();
			commands.RegisterCommands<SetTicketCommand>();
			commands.RegisterCommands<StatusCommand>();
			commands.RegisterCommands<SummaryCommand>();
			commands.RegisterCommands<ToggleActiveCommand>();
			commands.RegisterCommands<TranscriptCommand>();
			commands.RegisterCommands<UnassignCommand>();
			commands.RegisterCommands<UnblacklistCommand>();
			commands.RegisterCommands<UnsetTicketCommand>();

			Logger.Log("Hooking command events...");
			commands.SlashCommandErrored += EventHandler.OnCommandError;

			Logger.Log("Connecting to Discord...");
			await discordClient.ConnectAsync();
		}
	}
}
