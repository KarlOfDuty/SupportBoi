using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext;
using Newtonsoft.Json.Linq;
using SupportBot.Commands;
using SupportBot.Properties;
using YamlDotNet.Serialization;

// TODO: Add ColorfulConsole

namespace SupportBot
{
	internal class SupportBot
	{
		private string token = "";
		private string prefix = "";
		private DiscordClient discordClient;
		private CommandsNextModule commands;
		static void Main(string[] args)
		{
			new SupportBot().MainAsync().GetAwaiter().GetResult();
		}

		private async Task MainAsync()
		{
			try
			{
				Console.WriteLine("Loading config...");
				this.LoadConfig();

				if (this.token == "<add-token-here>" || this.token == "" || this.token == null)
				{
					Console.WriteLine("You need to set your bot token in the config and start the bot again.");
					Console.WriteLine("Press enter to close application.");
					Console.ReadLine();
					return;
				}

				Console.WriteLine("Setting up Discord client...");
				DiscordConfiguration cfg = new DiscordConfiguration
				{
					Token = this.token,
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
				while (true) { }
			}
		}

		private void LoadConfig()
		{
			// Writes default config to file if it does not already exist
			if (!File.Exists("./config.yml"))
			{
				File.WriteAllText("./config.yml", Encoding.UTF8.GetString(Resources.default_config));
			}

			// Reads config contents into FileStream
			FileStream stream = File.OpenRead("./config.yml");

			// Converts the FileStream into a YAML object
			IDeserializer deserializer = new DeserializerBuilder().Build();
			object yamlObject = deserializer.Deserialize(new StreamReader(stream));

			// Converts the YAML object into a JSON object as the YAML ones do not support traversal or selection of nodes by name 
			ISerializer serializer = new SerializerBuilder().JsonCompatible().Build();
			JObject json = JObject.Parse(serializer.Serialize(yamlObject));

			// Sets up the bot
			this.token = json.SelectToken("token").Value<string>();
			this.prefix = json.SelectToken("prefix").Value<string>();
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
			e.Client.DebugLogger.LogMessage(LogLevel.Error, "SupportBot", $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);

			return Task.CompletedTask;
		}

		private Task OnCommandError(CommandErrorEventArgs e)
		{
			e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "SupportBot", $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);

			return Task.CompletedTask;
		}
	}
}
