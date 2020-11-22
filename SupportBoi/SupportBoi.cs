using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Logging;
using SupportBoi.Commands;

namespace SupportBoi
{
	internal class SupportBoi
	{
		internal static SupportBoi instance;

		private DiscordClient discordClient = null;
		private CommandsNextExtension commands = null;
		private EventHandler eventHandler;

		static void Main(string[] args)
		{
			new SupportBoi().MainAsync().GetAwaiter().GetResult();
		}

		private async Task MainAsync()
		{
			instance = this;
			
			Console.WriteLine("Starting SupportBoi version " + GetVersion() + "...");
			try
			{
				this.Reload();

				// Block this task until the program is closed.
				await Task.Delay(-1);
			}
			catch (Exception e)
			{
				Console.WriteLine("Fatal error:");
				Console.WriteLine(e);
				Console.ReadLine();
			}
		}

		public static DiscordClient GetClient()
		{
			return instance.discordClient;
		}

		public static string GetVersion()
		{
			Version version = Assembly.GetEntryAssembly()?.GetName().Version;
			return version?.Major + "." + version?.Minor + "." + version?.Build + (version?.Revision == 0 ? "" : "-" + (char)(64 + version?.Revision ?? 0));
		}

		public async void Reload()
		{
			if (this.discordClient != null)
			{
				await this.discordClient.DisconnectAsync();
				this.discordClient.Dispose();
				Console.WriteLine("Discord client disconnected.");
			}
			
			Console.WriteLine("Loading config \"" + Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "config.yml\"");
			Config.LoadConfig();
			
			// Check if token is unset
			if (Config.token == "<add-token-here>" || Config.token == "")
			{
				Console.WriteLine("You need to set your bot token in the config and start the bot again.");
				throw new ArgumentException("Invalid Discord bot token");
			}

			// Database connection and setup
			try
			{
				Console.WriteLine("Connecting to database... (" + Config.hostName + ":" + Config.port + ")");
				Database.SetConnectionString(Config.hostName, Config.port, Config.database, Config.username, Config.password);
				Database.SetupTables();
			}
			catch (Exception e)
			{
				Console.WriteLine("Could not set up database tables, please confirm connection settings, status of the server and permissions of MySQL user. Error: " + e);
				throw;
			}
			
			Console.WriteLine("Setting up Discord client...");
			
			// Checking log level
			if (!Enum.TryParse(Config.logLevel, true, out LogLevel logLevel))
			{
				Console.WriteLine("Log level " + Config.logLevel + " invalid, using 'Information' instead.");
				logLevel = LogLevel.Information;
			}
			
			// Setting up client configuration
			DiscordConfiguration cfg = new DiscordConfiguration
			{
				Token = Config.token,
				TokenType = TokenType.Bot,
				MinimumLogLevel = logLevel,
				AutoReconnect = true,
			};
			
			this.discordClient = new DiscordClient(cfg);
			
			this.eventHandler = new EventHandler(this.discordClient);
			
			Console.WriteLine("Hooking events...");
			this.discordClient.Ready += this.eventHandler.OnReady;
			this.discordClient.GuildAvailable += this.eventHandler.OnGuildAvailable;
			this.discordClient.ClientErrored += this.eventHandler.OnClientError;
			this.discordClient.MessageCreated += this.eventHandler.OnMessageCreated;
			if (Config.reactionMessage != 0)
			{
				this.discordClient.MessageReactionAdded += this.eventHandler.OnReactionAdded;
			}
			
			Console.WriteLine("Registering commands...");
			commands = discordClient.UseCommandsNext(new CommandsNextConfiguration
			{
				StringPrefixes = new []{ Config.prefix }
			});

			this.commands.RegisterCommands<AddCommand>();
			this.commands.RegisterCommands<AddStaffCommand>();
			this.commands.RegisterCommands<AssignCommand>();
			this.commands.RegisterCommands<BlacklistCommand>();
			this.commands.RegisterCommands<CloseCommand>();
			this.commands.RegisterCommands<ListCommand>();
			this.commands.RegisterCommands<ListAssignedCommand>();
			this.commands.RegisterCommands<ListUnassignedCommand>();
			this.commands.RegisterCommands<ListOldestCommand>();
			this.commands.RegisterCommands<MoveCommand>();
			this.commands.RegisterCommands<NewCommand>();
			this.commands.RegisterCommands<RandomAssignCommand>();
			this.commands.RegisterCommands<ReloadCommand>();
			this.commands.RegisterCommands<RemoveStaffCommand>();
			this.commands.RegisterCommands<SetSummaryCommand>();
			this.commands.RegisterCommands<SetTicketCommand>();
			this.commands.RegisterCommands<StatusCommand>();
			this.commands.RegisterCommands<SummaryCommand>();
			this.commands.RegisterCommands<ToggleActiveCommand>();
			this.commands.RegisterCommands<TranscriptCommand>();
			this.commands.RegisterCommands<UnassignCommand>();
			this.commands.RegisterCommands<UnblacklistCommand>();
			this.commands.RegisterCommands<UnsetTicketCommand>();

			Console.WriteLine("Hooking command events...");
			this.commands.CommandErrored += this.eventHandler.OnCommandError;

			Console.WriteLine("Connecting to Discord...");
			await this.discordClient.ConnectAsync();
		}
	}
}
