using System;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext;
using SupportBot.Commands;

// TODO: Add ColorfulConsole

namespace SupportBot
{
	internal class SupportBot
	{
		public static SupportBot instance;

		private DiscordClient discordClient;
		private CommandsNextModule commands;

		static void Main(string[] args)
		{
			new SupportBot().MainAsync().GetAwaiter().GetResult();
		}

		private async Task MainAsync()
		{
			instance = this;
			try
			{
				Console.WriteLine(Directory.GetCurrentDirectory());
				Console.WriteLine("Loading config...");
				Config.LoadConfig();

				if (Config.token == "<add-token-here>" || Config.token == "" || Config.token == null)
				{
					Console.WriteLine("You need to set your bot token in the config and start the bot again.");
					Console.WriteLine("Press enter to close application.");
					Console.ReadLine();
					return;
				}

				Console.WriteLine("Connecting to database...");
				Database.SetConnectionString(Config.hostName, Config.port, Config.database, Config.username, Config.password);
				Database.SetupTables();

				Console.WriteLine("Setting up Discord client...");
				DiscordConfiguration cfg = new DiscordConfiguration
				{
					Token = Config.token,
					TokenType = TokenType.Bot,

					AutoReconnect = true,
					LogLevel = LogLevel.Debug,
					UseInternalLogHandler = true
				};

				this.discordClient = new DiscordClient(cfg);

				Console.WriteLine("Hooking events...");
				this.discordClient.Ready += this.OnReady;
				this.discordClient.GuildAvailable += this.OnGuildAvailable;
				this.discordClient.ClientErrored += this.OnClientError;

				Console.WriteLine("Registering commands...");
				commands = discordClient.UseCommandsNext(new CommandsNextConfiguration
				{
					StringPrefix = Config.prefix
				});

				this.commands.RegisterCommands<TicketCommands>();
				this.commands.RegisterCommands<ModeratorCommands>();
				this.commands.RegisterCommands<AdminCommands>();

				Console.WriteLine("Hooking command events...");
				this.commands.CommandErrored += this.OnCommandError;

				Console.WriteLine("Connecting to Discord...");
				await this.discordClient.ConnectAsync();

				// Block this task until the program is closed.
				await Task.Delay(-1);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				Console.ReadLine();
			}
		}

		private Task OnReady(ReadyEventArgs e)
		{
			e.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBot", "Client is ready to process events.", DateTime.Now);
			return Task.CompletedTask;
		}

		private Task OnGuildAvailable(GuildCreateEventArgs e)
		{
			e.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBot", $"Guild available: {e.Guild.Name}", DateTime.Now);

			return Task.CompletedTask;
		}

		private Task OnClientError(ClientErrorEventArgs e)
		{
			e.Client.DebugLogger.LogMessage(LogLevel.Error, "SupportBot", $"Exception occured: {e.Exception.GetType()}: {e.Exception}", DateTime.Now);

			return Task.CompletedTask;
		}

		private Task OnCommandError(CommandErrorEventArgs e)
		{
			e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "SupportBot", $"Exception occured: {e.Exception.GetType()}: {e.Exception}", DateTime.Now);

			return Task.CompletedTask;
		}
	}
}
