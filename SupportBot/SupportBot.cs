using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Newtonsoft.Json.Linq;
using SupportBot.Properties;
using YamlDotNet.Serialization;

namespace SupportBot
{
	internal class SupportBot
	{
		private string token = "";
		private DiscordClient discordClient;
		static void Main(string[] args)
		{
			new SupportBot().MainAsync().GetAwaiter().GetResult();
		}

		public async Task MainAsync()
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
				this.discordClient.Ready += this.Client_Ready;
				this.discordClient.GuildAvailable += this.Client_GuildAvailable;
				this.discordClient.ClientErrored += this.Client_ClientError;

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
		}

		private Task Client_Ready(ReadyEventArgs e)
		{
			// let's log the fact that this event occured
			Console.WriteLine("Ready.");
			e.Client.DebugLogger.LogMessage(LogLevel.Info, "ExampleBot", "Client is ready to process events.", DateTime.Now);
			return Task.CompletedTask;
		}

		private Task Client_GuildAvailable(GuildCreateEventArgs e)
		{
			// let's log the name of the guild that was just
			// sent to our client
			e.Client.DebugLogger.LogMessage(LogLevel.Info, "ExampleBot", $"Guild available: {e.Guild.Name}", DateTime.Now);

			return Task.CompletedTask;
		}

		private Task Client_ClientError(ClientErrorEventArgs e)
		{
			// let's log the details of the error that just 
			// occured in our client
			e.Client.DebugLogger.LogMessage(LogLevel.Error, "ExampleBot", $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);

			return Task.CompletedTask;
		}
	}
}
