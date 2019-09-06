# Support Boi [![Build Status](http://95.217.45.17:8080/job/SupportBoi/job/master/badge/icon)](http://95.217.45.17:8080/blue/organizations/jenkins/SupportBoi/activity) [![Release](https://img.shields.io/github/release/KarlofDuty/SupportBoi.svg)](https://github.com/KarlOfDuty/SupportBoi/releases) [![Discord Server](https://img.shields.io/discord/430468637183442945.svg?label=discord)](https://discord.gg/C5qMvkj) [![Patreon](https://img.shields.io/badge/patreon-donate-orange.svg)](https://patreon.com/karlofduty)

A support ticket Discord bot. Uses a MySQL database for storage of active tickets and also saves closed tickets as flatfile HTML documents.

## Commands

| Command | Role | Description |
|--- |--- |---- |
| `new` | Everyone | Opens a new ticket channel. |
| `close` | Everyone | Closes a ticket channel and posts a ticket transcript in the log channel. |
| `transcript` | Everyone | Generates a ticket transcript as an html file. |
| `add` | Moderator | Adds a user to the ticket. |
| `blacklist` | Moderator | Blacklists a player from opening tickets. |
| `unblacklist` | Moderator | Un-blacklists a player from opening tickets. |
| `reload` | Admin | Reloads the config. |
| `setticket` | Admin | Sets an existing channel as a ticket. |
| `unsetticket` | Admin | Removes a ticket without deleting the channel. |

## Setup

 1. [Create a new bot application](https://discordapp.com/developers/applications/).

 2. [Install .NET Core 2.2](https://dotnet.microsoft.com/download/dotnet-core/2.2).

 3. Download the bot for your operating system and extract it somewhere on your computer.

 4. Set up the config (`config.yml`) to your specifications, follow the instructions inside of the config.

 5. Run `./SupportBoi` on Linux or `SupportBoi.exe` on Windows.

## Default Config

```yaml
bot:
    # Bot token
    token: "<add-token-here>"
    # Command prefix
    prefix: "+"
    # Channel where ticket logs are posted
    log-channel: 000000000000000000
    # Category where the ticket will be created, it will have the same permissions of that ticket plus read permissions for the user opening the ticket
    ticket-category: 000000000000000000
    # Message posted when a ticket is opened
    welcome-message: "Please describe your issue below, and include all information needed for us to take action, such as coordinates, in-game names and screenshots/chat logs."
    # Decides what messages are shown in console, possible values are: Critical, Error, Warning, Info, Debug
    console-log-level: "Info"

transcripts:
    timestamp-format: "yyyy-MM-dd HH:mm"

database:
    # Address and port of the mysql server
    address: "127.0.0.1"
    port: 3306
    # Name of the database to use
    name: "supportbot"
    # Username and password for authentication
    user: ""
    password: ""

permissions:
    # ID of the role allowed to use admin and moderator commands
    admin-role: 000000000000000000
    # ID of the role allowed to use moderator commands
    moderator-role: 000000000000000000
```

#### Thanks to [DiscordChatExporter](https://github.com/Tyrrrz/DiscordChatExporter) for the library used in the transcript function.
