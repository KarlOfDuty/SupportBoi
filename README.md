[![Downloads](https://img.shields.io/github/downloads/KarlOfDuty/SupportBoi/total.svg)](https://github.com/KarlOfDuty/SupportBoi/releases) [![Release](https://img.shields.io/github/release/KarlofDuty/SupportBoi.svg)](https://github.com/KarlOfDuty/SupportBoi/releases) ![GitHub commits since latest release](https://img.shields.io/github/commits-since/karlofduty/supportboi/latest) [![Discord Server](https://img.shields.io/discord/430468637183442945.svg?label=discord)](https://discord.gg/C5qMvkj) [![Build Status](https://jenkins.karlofduty.com/job/DiscordBots/job/SupportBoi/job/main/badge/icon)](https://jenkins.karlofduty.com/blue/organizations/jenkins/DiscordBots%2FSupportBoi/activity) [![Codacy Badge](https://app.codacy.com/project/badge/Grade/756c69228dba49d78556fc464275e141)](https://app.codacy.com/gh/KarlOfDuty/SupportBoi/dashboard) ![GitHub License](https://img.shields.io/github/license/karlofduty/supportboi)
# SupportBoi

A support ticket Discord bot. Uses a MySQL database for storage of ticket information. Creates formatted HTML ticket transcripts when tickets are closed.

#### Thanks to [Tyrrrz](https://github.com/Tyrrrz/DiscordChatExporter) for the amazing library used in the transcript function.

## Setup

1. Set up a mysql-compatible server, create a user and empty database for the bot to use.

2. (Optional) Install .NET 8 if it doesn't already exist on your system.

3. [Create a new bot application and invite it to your server](docs/CreateBot.md).

4. Download the bot for your operating system, either a [release version](https://github.com/KarlOfDuty/SupportBoi/releases) or a [dev build](https://jenkins.karlofduty.com/blue/organizations/jenkins/DiscordBots%2FSupportBoi/activity). Get the normal version if you have installed .NET 8 on your system, get the self contained version otherwise.

5. Run `./SupportBoi_Linux` on Linux or `./SupportBoi_Windows.exe` on Windows, this creates a config file in the current directory.

6. Set up the config, there are instructions inside. If you need more help either contact me in Discord or through an issue here.

7. Restart the bot.

8. Go to `Settings->Integrations->Bot->Command Permissions` in your Discord server to set up permissions for the commands.

## Documentation

- [Commands](./docs/Commands.md)
- [Interview templates](./docs/InterviewTemplates.md)
- [Default config](./default_config.yml)
