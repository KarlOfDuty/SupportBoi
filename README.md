# Support Boi [![Build Status](http://95.217.45.17:8080/job/SupportBoi/job/master/badge/icon)](http://95.217.45.17:8080/blue/organizations/jenkins/SupportBoi/activity) [![Release](https://img.shields.io/github/release/KarlofDuty/SupportBoi.svg)](https://github.com/KarlOfDuty/SupportBoi/releases) [![Discord Server](https://img.shields.io/discord/430468637183442945.svg?label=discord)](https://discord.gg/C5qMvkj) [![Patreon](https://img.shields.io/badge/patreon-donate-orange.svg)](https://patreon.com/karlofduty)

A support ticket Discord bot. Uses a MySQL database for storage of ticket information. Creates amazingly formatted HTML ticket transcripts when tickets are closed. Has the ability to output ticket information to a google sheet which can be customized in a number of ways.

#### Thanks to [DiscordChatExporter](https://github.com/Tyrrrz/DiscordChatExporter) for the great library used in the transcript function.

## Commands

| Command | Description |
|--- |---- |
| `new` | Opens a new ticket channel. |
| `close` | Closes a ticket channel and posts a ticket transcript in the log channel. |
| `transcript` | Generates a ticket transcript as an html file. |
| `status` | Shows a status message about the bot with info such as number of tickets and which version is running. If ran in a ticket channel it also shows ticket information. |
| `summary` | Shows a ticket's summary if set. |
| `add <ids/mentions>` | Add users to the ticket. |
| `assign (id/mention)` | Assigns a ricket to a staff member, themself if no mention or id is provided. |
| `rassign` | Randomly assigns a ticket to an active staff member. |
| `unassign` | Unassigns a ticket from the currently assigned staff member. |
| `blacklist <ids/mentions>` | Blacklists users from opening tickets. |
| `unblacklist <ids/mentions>` | Un-blacklists users from opening tickets. |
| `setsummary <summary>` | Sets a summary for a ticket which can be viewed using the `summary` and `status` commands. |
| `toogleactive/ta` | Toggles whether a staff member counts as active or not. |
| `reload` | Reloads the config. |
| `setticket` | Makes the current channel a ticket. |
| `unsetticket` | Removes a ticket without deleting the channel. |
| `addstaff <id/mention>` | Registers a user as a staff member for ticket assignment. |
| `removestaff <id/mention>` | Removes a user from staff. |

## Setup

1. [Create a new bot application](https://discordapp.com/developers/applications/).

2. [Install .NET Core 2.2](https://dotnet.microsoft.com/download/dotnet-core/2.2).

3. Download the bot for your operating system, either a [release version](https://github.com/KarlOfDuty/SupportBoi/releases) or a [dev build](http://95.217.45.17:8080/blue/organizations/jenkins/SupportBoi/activity).

4. Run `./SupportBoi` on Linux or `./SupportBoi.exe` on Windows.

5. Set up the config (`config.yml`) to your specifications, there are instructions inside and also further down on this page. If you need more help either contact me in Discord or through an issue here.
 
#### Google Sheets integration (Optional): 
 
If you are using Google Sheets you will have to do some additional setup:

1. Generate a credentials.json file by clicking "Enable the Google Sheets API" [here](https://developers.google.com/sheets/api/quickstart/dotnet), and following the instructions.

2. Place this file in the same directory as the bot.

3. You now need to generate a token. On a desktop PC the bot will open a browser window for you to log into your Google account and generate an API token, which is then automatically saved as "token.json" in the bot directory. I have no idea how to generate it from a terminal environment so I recommend you run the bot with the Sheets API set up once on your normal PC first and then just transfer the token it creates to your server.

4. If you did step 3 in a desktop environment you do not have to do anything else, if you did it in a terminal environment you likely have to restart the application after adding the token.

## Default Config

```yaml
bot:
    # Bot token.
    token: "<add-token-here>"
    # Command prefix.
    prefix: "+"
    # Channel where ticket logs are posted.
    log-channel: 000000000000000000
    # Category where the ticket will be created, it will have the same permissions of that ticket plus read permissions for the user opening the ticket.
    ticket-category: 000000000000000000
    # Message posted when a ticket is opened.
    welcome-message: "Please describe your issue below, and include all information needed for us to take action, such as coordinates, in-game names and screenshots/chat logs."
    # Decides what messages are shown in console, possible values are: Critical, Error, Warning, Info, Debug.
    console-log-level: "Info"
    # Format for timestamps in transcripts and google sheets if used
    timestamp-format: "yyyy-MM-dd HH:mm"
    # Whether or not staff members should be randomly assigned tickets when they are made. Individual staff members can opt out using the toggleactive command.
    random-assignment: true

database:
    # Address and port of the mysql server
    address: "127.0.0.1"
    port: 3306
    # Name of the database to use
    name: "supportbot"
    # Username and password for authentication
    user: ""
    password: ""

# Set up which roles are allowed to use different commands.
# Example:
#   new: [ 000000000000000000, 111111111111111111 ]
# They are grouped into suggested command groups below for first time setup.
permissions:
    # Public commands
    new: []
    close: []
    transcript: []
    status: []
    summary: []
    # Moderator commands
    add: []
    assign: []
    rassign: []
    unassign: []
    blacklist: []
    unblacklist: []
    setsummary: []
    updatestaff: []
    toggleactive: []
    # Admin commands
    reload: []
    setticket: []
    unsetticket: []
    addstaff: []
    removestaff: []

sheets:
    # Whether or not to use the google sheets integration. 
    # You will have to generate a credentials.json file by clicking "Enable the Google Sheets API" here: https://developers.google.com/sheets/api/quickstart/dotnet 
    enabled: false
    # The spreadsheet ID of the sheet, you can find it in the sheet's URL:
    # https://docs.google.com/spreadsheets/d/<SpreadSheetID>/edit#gid=<SheetID>
    # In the above link you would use <SpreadSheetID> and not <SheetID>
    id: "ID here"
```
