# SupportBoi [![Build Status](https://jenkins.karlofduty.com/job/DiscordBots/job/SupportBoi/job/main/badge/icon)](https://jenkins.karlofduty.com/blue/organizations/jenkins/DiscordBots%2FSupportBoi/activity) [![Downloads](https://img.shields.io/github/downloads/KarlOfDuty/SupportBoi/total.svg)](https://github.com/KarlOfDuty/SupportBoi/releases) [![Release](https://img.shields.io/github/release/KarlofDuty/SupportBoi.svg)](https://github.com/KarlOfDuty/SupportBoi/releases) [![Discord Server](https://img.shields.io/discord/430468637183442945.svg?label=discord)](https://discord.gg/C5qMvkj) [![CodeQL](https://github.com/KarlOfDuty/SupportBoi/actions/workflows/codeql.yml/badge.svg)](https://github.com/KarlOfDuty/SupportBoi/actions/workflows/codeql.yml)

A support ticket Discord bot. Uses a MySQL database for storage of ticket information. Creates amazingly formatted HTML ticket transcripts when tickets are closed.

#### Thanks to [Tyrrrz](https://github.com/Tyrrrz/DiscordChatExporter) for the great library used in the transcript function.

## Commands

| Command                              | Description                                                                                                                                  |
|--------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------|
| `/add <user>`                        | Add users to the ticket.                                                                                                                     |
| `/addcategory <title> <category>`    | Adds a category for users to open tickets in. The title is what will be used on buttons and in selection menus.                              |
| `/addmessage <identifier> <message>` | Adds a new message for the `say` command. The identifier is one word used in the say command and the message is what the bot prints.         |
| `/addstaff <user>`                   | Registers a user as a staff member for ticket assignment.                                                                                    |
| `/assign (user)`                     | Assigns a ticket to a staff member, themself if no mention or id is provided.                                                                |
| `/blacklist <user>`                  | Blacklists users from opening tickets.                                                                                                       |
| `/close`                             | Closes a ticket channel and posts a ticket transcript in the log channel.                                                                    |
| `/createbuttonpanel`                 | Creates a panel of buttons for users to open tickets with, one for each saved category.                                                      |
| `/createselectionbox (message)`      | Creates a selection menu for users to open tickets with. Message is the placeholder shown on the selection menu before anything is selected. |
| `/list (user)`                       | Lists a user's open and closed tickets.                                                                                                      |
| `/listassigned (user)`               | Lists all of a staff member's assigned tickets.                                                                                              |
| `/listopen`                          | Lists a number of the oldest still open tickets, default is 20.                                                                              |
| `/listunassigned`                    | Lists all unassigned tickets.                                                                                                                |
| `/move <category>`                   | Moves a ticket to a specific category by partial name.                                                                                       |
| `/new`                               | Opens a new ticket channel.                                                                                                                  |
| `/rassign (role)`                    | Randomly assigns a ticket to an active staff member. If a role is provided only staff member with that role are considered.                  |
| `/removecategory <category>`         | Removes a category from the bot.                                                                                                             |
| `/removemessage <identifier>`        | Removes message from the database.                                                                                                           |
| `/removestaff <user>`                | Removes a user from staff.                                                                                                                   |
| `/say (identifier)`                  | Prints a message with information from staff. Use with no arguments to list ids.                                                             |
| `/setsummary <summary>`              | Sets a summary for a ticket which can be viewed using the `summary` command.                                                                 |
| `/status`                            | Shows a status message about the bot with info such as number of tickets and which version is running.                                       |
| `/summary`                           | Shows some information about a ticket and its summary if set.                                                                                |
| `/toggleactive (user)`               | Toggles whether a staff member counts as active or not.                                                                                      |
| `/transcript (ticket id) `           | Generates a ticket transcript as an html file.                                                                                               |
| `/unassign`                          | Unassigns a ticket from the currently assigned staff member.                                                                                 |
| `/unblacklist <user>`                | Un-blacklists users from opening tickets.                                                                                                    |
| `/admin listinvalid`                 | Lists tickets which channels have been deleted, you can use the `/admin unsetticket` command to remove them from the ticket system.          |
| `/admin reload`                      | Reloads the config.                                                                                                                          |
| `/admin setticket (channel)`         | Makes the current channel a ticket.                                                                                                          |
| `/admin unsetticket (ticket id)`     | Removes a ticket without deleting the channel.                                                                                               |

## Setup

1. Set up a mysql server, create a user and empty database for the bot to use.

2. Install .NET 6 if it doesn't already exist on your system.

3. [Create a new bot application and invite it to your server](docs/CreateBot.md).

4. Go to `Settings->Integrations->Bot->Command Permissions` and turn off command access for the everyone role.

5. [Create a new bot application](https://discordpy.readthedocs.io/en/latest/discord.html).

6. Download the bot for your operating system, either a [release version](https://github.com/KarlOfDuty/SupportBoi/releases) or a [dev build](https://jenkins.karlofduty.com/blue/organizations/jenkins/SupportBoi/activity).

7. Run `./SupportBoi_Linux` on Linux or `./SupportBoi_Windows.exe` on Windows.

8. Set up the config (`config.yml`) to your specifications, there are instructions inside and also further down on this page. If you need more help either contact me in Discord or through an issue here.

9. Restart the bot.

10. Go to `Settings->Integrations->Bot->Command Permissions` in your Discord server to set up permissions for the commands.

## Default Config

```yaml
bot:
    # Bot token.
    token: "<add-token-here>"
    # Channel where ticket logs are posted (recommended)
    log-channel: 000000000000000000
    # Message posted when a ticket is opened.
    welcome-message: "Please describe your issue below, and include all information needed for us to take action. This is an example ticket message and can be changed in the config."
    # Decides what messages are shown in console
    # Possible values are: Critical, Error, Warning, Information, Debug.
    console-log-level: "Information"
    # One of the following: LongDate, LongDateTime, LongTime, RelativeTime, ShortDate, ShortDateTime, ShortTime
    # More info: https://dsharpplus.github.io/api/DSharpPlus.TimestampFormat.html
    timestamp-format: "RelativeTime"
    # Whether or not staff members should be randomly assigned tickets when they are made. Individual staff members can opt out using the toggleactive command.
    random-assignment: true
    # If set to true the rasssign command will include staff members set as inactive if a specific role is specified in the command.
    # This can be useful if you have admins set as inactive to not automatically receive tickets and then have moderators elevate tickets when needed.
    random-assign-role-override: true
    # Sets the type of activity for the bot to display in its presence status
    # Possible values are: Playing, Streaming, ListeningTo, Watching, Competing
    presence-type: "ListeningTo"
    # Sets the activity text shown in the bot's status
    presence-text: "/new"
    # Set to true if you want the /new command to show a selection box instead of a series of buttons
    new-command-uses-selector: false
    # Number of tickets a single user can have open at a time, staff members are excluded from this
    ticket-limit: 5

notifications:
    # Notifies the assigned staff member when a new message is posted in a ticket if the ticket has been silent for a configurable amount of time
    # Other staff members and bots do not trigger this.
    ticket-updated: true
    # The above notification will only be sent if the ticket has been silent for more than this amount of days. Default is 0.5 days.
    ticket-updated-delay: 0.5
    # Notifies staff when they are assigned to tickets
    assignment: true
    # Notifies the user opening the ticket that their ticket was closed and includes the transcript
    closing: true

database:
    # Address and port of the mysql server
    address: "127.0.0.1"
    port: 3306
    # Name of the database to use
    name: "supportbot"
    # Username and password for authentication
    user: ""
    password: ""
```
