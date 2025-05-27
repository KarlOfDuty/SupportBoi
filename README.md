[![Downloads](https://img.shields.io/github/downloads/KarlOfDuty/SupportBoi/total.svg)](https://github.com/KarlOfDuty/SupportBoi/releases) [![Release](https://img.shields.io/github/release/KarlofDuty/SupportBoi.svg)](https://github.com/KarlOfDuty/SupportBoi/releases) ![GitHub commits since latest release](https://img.shields.io/github/commits-since/karlofduty/supportboi/latest) [![Discord Server](https://img.shields.io/discord/430468637183442945.svg?label=discord)](https://discord.gg/C5qMvkj) [![Build Status](https://jenkins.karlofduty.com/job/DiscordBots/job/SupportBoi/job/main/badge/icon)](https://jenkins.karlofduty.com/blue/organizations/jenkins/DiscordBots%2FSupportBoi/activity) [![Codacy Badge](https://app.codacy.com/project/badge/Grade/756c69228dba49d78556fc464275e141)](https://app.codacy.com/gh/KarlOfDuty/SupportBoi/dashboard) ![GitHub License](https://img.shields.io/github/license/karlofduty/supportboi)
# SupportBoi

A support ticket Discord bot. Uses a MySQL database for storage of ticket information, no information is stored outside of your server. Creates formatted HTML ticket transcripts when tickets are closed.

#### Thanks to [Tyrrrz](https://github.com/Tyrrrz/DiscordChatExporter) for the amazing library used in the transcript function.

## Setup

1. [Register a bot in the Discord Developer panel](docs/RegisterBotApplication.md)

2. [Install the bot and set up a MySQL database for it](docs/Installation.md)

## Documentation

- [Commands](./docs/Commands.md)
- [Interview templates](./docs/InterviewTemplates.md)
- [Default config](./default_config.yml)

## Features

#### Opening tickets in different categories

Users can open support tickets using the following methods:
- Using the `/new` command.
- Using a button panel created by you.
- Using a selection box created by you.

You can either set up the bot to automatically open new tickets in a specific category or give the user a choice depending on their issue, regardless of which method they use.

![image](https://github.com/user-attachments/assets/318067b6-37ac-433f-885a-975aa2fd4e7c) ![image](https://github.com/user-attachments/assets/1f27ecf7-91cc-4f28-ae9d-c26b7fec4241)


#### Automated interviews

It is possible to set up a json interview tree where the bot asks questions and depending on the user's answer decides on what to do next. At the end of the interview you can configure the bot to post an interview summary where the bot will take all the previous answers and compile them in a single table. All of the messages in the interview are then deleted to keep the channel clean.

**TODO: Pictures here**

#### Fully formatted transcripts

When a ticket is closed the Discord channel is saved to a local backup file on the server which can then be downloaded by the user who originally opened the ticket and staff members.

**TODO: Pictures here**

#### Assigning tickets to staff

Tickets can be assigned to specific staff members and this can be done automatically on creation. When the bot picks a random staff member to assign it picks between all users registered as staff, but individual staff members may opt out using the `/toggleactive` command.

You can also manually assign a random staff member using the /rassign command, either among all staff members or by supplying a Discord role to only assign among staff members with that role.

**TODO: Pictures here**

#### Automated notifications

The bot can be set up to send direct message notifications when different things happen:
- For staff members when they are assigned to tickets.
- For the assigned staff member if a new message is sent in a ticket after it has been inactive for a configurable period.
- For users when their ticket is closed. They also get the ticket transcript included with their notificaiton.

**TODO: Pictures here**

#### Logging

The bot can be set to log all actions to a Discord channel, including uploading ticket transcripts to it. It can also be set to log information to a log file on the server.

**TODO: Pictures here**
