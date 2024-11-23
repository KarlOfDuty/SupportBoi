# Commands
These are all the commands you can use with the bot. Remember to set up your command permissions after you start the bot up, otherwise everyone will be able to use all commands.

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
| `/listinvalid`                       | Lists tickets which channels have been deleted or the creator has left the server.                                                           |
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
| `/admin reload`                      | Reloads the config.                                                                                                                          |
| `/admin setticket (channel)`         | Makes the current channel a ticket.                                                                                                          |
| `/admin unsetticket (ticket id)`     | Removes a ticket without deleting the channel.                                                                                               |
