# SupportBoi [![Build Status](https://jenkins.karlofduty.com/job/SupportBoi/job/master/badge/icon)](https://jenkins.karlofduty.com/blue/organizations/jenkins/SupportBoi/activity) [![Downloads](https://img.shields.io/github/downloads/KarlOfDuty/SupportBoi/total.svg)](https://github.com/KarlOfDuty/SupportBoi/releases) [![Release](https://img.shields.io/github/release/KarlofDuty/SupportBoi.svg)](https://github.com/KarlOfDuty/SupportBoi/releases) [![Discord Server](https://img.shields.io/discord/430468637183442945.svg?label=discord)](https://discord.gg/C5qMvkj)

A support ticket Discord bot. Uses a MySQL database for storage of ticket information. Creates amazingly formatted HTML ticket transcripts when tickets are closed.

#### Thanks to [Tyrrrz](https://github.com/Tyrrrz/DiscordChatExporter) for the great library used in the transcript function.

## Commands

| Command | Description |
|--- |---- |
| `new` | Opens a new ticket channel. |
| `close` | Closes a ticket channel and posts a ticket transcript in the log channel. |
| `transcript (ticket number) ` | Generates a ticket transcript as an html file. |
| `status` | Shows a status message about the bot with info such as number of tickets and which version is running. |
| `summary` | Shows some information about a ticket and its summary if set. |
| `list (id/mention)` | Lists a user's open and closed tickets. |
| `add <ids/mentions>` | Add users to the ticket. |
| `assign (id/mention)` | Assigns a ticket to a staff member, themself if no mention or id is provided. |
| `rassign (role id/mention/name)` | Randomly assigns a ticket to an active staff member. If a role is provided only staff member with that role are considered. |
| `unassign` | Unassigns a ticket from the currently assigned staff member. |
| `blacklist <ids/mentions>` | Blacklists users from opening tickets. |
| `unblacklist <ids/mentions>` | Un-blacklists users from opening tickets. |
| `setsummary <summary>` | Sets a summary for a ticket which can be viewed using the `summary` command. |
| `toggleactive/ta (id/mention)` | Toggles whether a staff member counts as active or not. |
| `listassigned/la (id/mention)` | Lists all of a staff member's assigned tickets. |
| `listunassigned/lu` | Lists all unassigned tickets. |
| `listoldest/lo (limit)` | Lists a number of the oldest still open tickets, default is 20. |
| `move <category>` | Moves a ticket to a specific category by partial name. |
| `reload` | Reloads the config. |
| `setticket (id/mention)` | Makes the current channel a ticket. |
| `unsetticket` | Removes a ticket without deleting the channel. |
| `addstaff <id/mention>` | Registers a user as a staff member for ticket assignment. |
| `removestaff <id/mention>` | Removes a user from staff. |
| `say (identifier)` | Prints a message with information from staff. Use with no arguments to list ids. |
| `addmessage <identifier> <message>` | Adds a new message for the 'say' command. The identifier is one word used in the say command and the message is what the bot prints. |
| `removestaff <identifier>` | Removes message from the database. |

## Setup

1. Set up a mysql server, create a user and empty database for the bot to use.

2. [Create a new bot application](https://discordpy.readthedocs.io/en/latest/discord.html).

3. Download the bot for your operating system, either a [release version](https://github.com/KarlOfDuty/SupportBoi/releases) or a [dev build](https://jenkins.karlofduty.com/blue/organizations/jenkins/SupportBoi/activity).

4. Run `./SupportBoi_Linux` on Linux or `./SupportBoi_Windows.exe` on Windows.

5. Set up the config (`config.yml`) to your specifications, there are instructions inside and also further down on this page. If you need more help either contact me in Discord or through an issue here.

## Default Config

```yaml
bot:
    # Bot token.
    token: "<add-token-here>"
    # Command prefix.
    prefix: "+"
    # Channel where ticket logs are posted (recommended)
    log-channel: 000000000000000000
    # Category where the ticket will be created, it will have the same permissions of that ticket plus read permissions for the user opening the ticket (recommended)
    ticket-category: 000000000000000000
    # A message which will open new tickets when users react to it (optional)
    reaction-message: 000000000000000000
    # Message posted when a ticket is opened.
    welcome-message: "Please describe your issue below, and include all information needed for us to take action. This is an example ticket message and can be changed in the config."
    # Decides what messages are shown in console
    # Possible values are: Critical, Error, Warning, Information, Debug.
    console-log-level: "Information"
    # Format for timestamps in transcripts and google sheets if used
    timestamp-format: "yyyy-MM-dd HH:mm"
    # Whether or not staff members should be randomly assigned tickets when they are made. Individual staff members can opt out using the toggleactive command.
    random-assignment: true
    # If set to true the rasssign command will include staff members set as inactive if a specific role is specified in the command.
    # This can be useful if you have admins set as inactive to not automatically recieve tickets and then have moderators elevate tickets when needed.
    random-assign-role-override: true
    # Sets the type of activity for the bot to display in its presence status
    # Possible values are: Playing, Streaming, ListeningTo, Watching, Competing
    presence-type: "ListeningTo"
    # Sets the activity text shown in the bot's status
    presence-text: "+new"

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

# Set up which roles are allowed to use different commands.
# Example:
#   new: [ 000000000000000000, 111111111111111111 ]
# They are grouped into suggested command groups below for first time setup.
permissions:
    # Public commands
    close: []
    list: []
    new: []
    say: []
    status: []
    summary: []
    transcript: []
    # Moderator commands
    add: []
    addmessage: []
    assign: []
    blacklist: []
    listassigned: []
    listoldest: []
    listunassigned: []
    move: []
    rassign: []
    removemessage: []
    setsummary: []
    toggleactive: []
    unassign: []
    unblacklist: []
    # Admin commands
    addstaff: []
    reload: []
    removestaff: []
    setticket: []
    unsetticket: []
```
