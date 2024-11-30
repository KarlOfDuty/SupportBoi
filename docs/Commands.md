# Commands
These are all the commands you can use with the bot. Remember to set up your command permissions after you start the bot up, otherwise everyone will be able to use all commands.

| Command                                | Description                                                                                                                               |
|----------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------|
| `/add <user>`                          | Add a user to the ticket.                                                                                                                 |
| `/addcategory <title> <category>`      | Adds a category for users to open tickets in. The title is what will be used on buttons and in selection menus.                           |
| `/addmessage <identifier> <message>`   | Adds a new message for the `/say` command. The identifier is the word used in the say command and the message is what the bot prints.     |
| `/addstaff <user>`                     | Registers a user as a staff member for ticket assignment.                                                                                 |
| `/assign (user)`                       | Assigns a ticket to a staff member. Assigns to self if no mention or id is provided.                                                      |
| `/blacklist <user>`                    | Blocks a user from opening tickets.                                                                                                       |
| `/close`                               | Closes a ticket channel. The ticket transcript is optionally sent to the ticket creator.                                                  |
| `/createbuttonpanel`                   | Creates a panel of buttons for users to open tickets with, one for each saved category.                                                   |
| `/createselectionbox (placeholder)`    | Creates a selection menu for users to open tickets with. Placeholder is the text shown on the selection menu before anything is selected. |
| `/interview restart`                   | Restarts the interview in the current channel. It will use a new template if one is available.                                            |
| `/interview stop`                      | Stops the ongoing interview in this channel.                                                                                              |
| `/interviewtemplate get <category>`    | Get the json interview template for a specific category. A basic one will be returned if one doesn't exist.                               |
| `/interviewtemplate set <template>`    | Set the interview template for a category. The category is specified by the ID in the template file.                                      |
| `/interviewtemplate delete <category>` | Deletes the interview template for a category.                                                                                            |
| `/list (user)`                         | Lists a user's open and closed tickets.                                                                                                   |
| `/listassigned (user)`                 | Lists all of a staff member's assigned tickets.                                                                                           |
| `/listinvalid`                         | Lists tickets which channels have been deleted or the creator has left the server.                                                        |
| `/listopen`                            | Lists a number of the oldest still open tickets, default is 20. Only shows tickets the user has access to read.                           |
| `/listunassigned`                      | Lists all unassigned tickets. Only shows tickets the user has access to read.                                                             |
| `/move <category>`                     | Moves a ticket to a specific category by partial name.                                                                                    |
| `/new`                                 | Opens a new ticket channel.                                                                                                               |
| `/rassign (role)`                      | Randomly assigns a ticket to an active staff member. If a role is provided only staff members with that role are considered.              |
| `/removecategory <category>`           | Removes a category from the bot.                                                                                                          |
| `/removemessage <identifier>`          | Removes message from the database.                                                                                                        |
| `/removestaff <user>`                  | Removes a user from staff.                                                                                                                |
| `/say (identifier)`                    | Prints a message with information from staff. Use with no arguments to list identifiers.                                                  |
| `/setsummary <summary>`                | Sets a summary for a ticket which can be viewed using the `/summary` command.                                                             |
| `/status`                              | Shows a status message about the bot with info such as number of tickets and which version is running.                                    |
| `/summary`                             | Shows some information about a ticket and its summary if set.                                                                             |
| `/toggleactive (user)`                 | Toggles whether a staff member counts as active or not when automatic assigning occurs.                                                   |
| `/transcript (ticket id)`              | Generates a ticket transcript as an html file.                                                                                            |
| `/unassign`                            | Unassigns a ticket from the currently assigned staff member.                                                                              |
| `/unblacklist <user>`                  | Un-blacklists users from opening tickets.                                                                                                 |
| `/admin reload`                        | Reloads the config.                                                                                                                       |
| `/admin setticket (channel)`           | Makes the current channel a ticket. WARNING: Anyone will be able to delete the channel using /close.                                      |
| `/admin unsetticket (ticket id)`       | Deletes a ticket from the ticket system without deleting the Discord channel.                                                             |
