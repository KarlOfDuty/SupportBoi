# Support Boi [![Build Status](https://travis-ci.com/KarlOfDuty/SupportBot.svg?branch=master)](https://travis-ci.com/KarlOfDuty/SupportBot)

A support ticket Discord bot. Uses a MySQL database for storage of active tickets and also saves closed tickets as flatfile HTML documents.

## Commands:

| Command | Role | Description
|--- |--- |---- |
| `new` | Everyone | Opens a new ticket channel. |
| `close` | Everyone | Closes a ticket channel and posts a ticket transcript in the log channel. |
| `transcript` | Everyone | Generates a ticket transcript as an html file. |
| `add` | Moderator | Adds a user to the ticket. |
| `blacklist` | Moderator | Blacklists a player from opening tickets. |
| `unblacklist` | Moderator | Un-blacklists a player from opening tickets. |
| `reload` | Admin | Allows a player to grant reserved slots. |
| `setticket` | Admin | Allows a player to remove reserved slots. |
| `unsetticket` | Admin | Allows a player to grant vanilla ranks. |

## Setup:

1. Create a new bot application over at: https://discordapp.com/developers/applications/.

2. Install .NET Core 2.2: https://dotnet.microsoft.com/download/dotnet-core/2.2.

3. Download the bot for your operating system and extract it somewhere on your computer.

4. Set up the config (`config.yml`) to your specifications, follow the instructions inside of the config.

5. Run `./SupportBoi` on Linux or `SupportBoi.exe` on Windows.

## Default Config:

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
