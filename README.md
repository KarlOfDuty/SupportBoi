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

### Ticket Categories

Users can open support tickets using the following methods:
- Using the `/new` command.
- Using a button panel created by you.
- Using a selection box created by you.

You can either set up the bot to automatically open new tickets in a specific category or give the user a choice depending on their issue, regardless of which method they use.

![image](https://github.com/user-attachments/assets/318067b6-37ac-433f-885a-975aa2fd4e7c) ![image](https://github.com/user-attachments/assets/1f27ecf7-91cc-4f28-ae9d-c26b7fec4241)


### Automated Interviews

It is possible to set up a json interview tree where the bot asks questions and depending on the user's answer decides on what to do next. At the end of the interview you can configure the bot to post an interview summary where the bot will take all the previous answers and compile them in a single table. All of the messages in the interview are then deleted to keep the channel clean.

![Screenshot_20250528_181315_resize](https://github.com/user-attachments/assets/a81189a5-a330-42d8-8086-7490b1c1a564) ![Screenshot_20250528_181540_resize](https://github.com/user-attachments/assets/1f1f3e17-8dc6-4630-b1a6-3f01ec141b6a)


### Fully Rendered Transcripts

When a ticket is closed the Discord channel is saved to a local backup file on the server which can then be downloaded by the user who originally opened the ticket and staff members.

**TODO: Pictures here**


### Staff Tools

Tickets can be assigned automatically on ticket creation or using commands. The commands can assign randomly across all staff, a specific Discord role or assign a specific user. Individual staff members can also opt-out of automatic assignment if they wish.

![image](https://github.com/user-attachments/assets/279d2410-8fad-426c-b848-02c309b6d615) ![image](https://github.com/user-attachments/assets/3cbf67db-ce68-47c7-a551-d8857b1e8622)


The bot can be set up to send direct message notifications when different things happen:
- For staff members when they are assigned to tickets.
- For the assigned staff member if a new message is sent in a ticket after it has been inactive for a configurable period.
- For users when their ticket is closed. They also get the ticket transcript included with their notificaiton.

![image](https://github.com/user-attachments/assets/c5ac4000-701f-4cbd-86a8-e521a72d98f3)

![image](https://github.com/user-attachments/assets/37c804a0-d273-43df-a160-77f91b89fcdd) 

Staff can show tickets assigned to a specific staff member, tickets opened by a specific user, the oldest open tickets, and more.

![image](https://github.com/user-attachments/assets/0c2725a3-da6c-4c9a-a6d9-b55dce4cbf44) ![image](https://github.com/user-attachments/assets/a7e6f920-7306-450d-9580-962d05068b9d)

Staff can set reusable messages which can then be reposted using the /say command.

![Screenshot_20250531_160944](https://github.com/user-attachments/assets/59cc1c36-9d5f-427c-ad18-ac25e9b48787) ![Screenshot_20250531_161640](https://github.com/user-attachments/assets/920415bc-002e-490b-9447-e863d4c3b1c4)


### Logging

The bot can be set to log all actions to a Discord channel, including uploading ticket transcripts to it. It can also be set to log information to a log file on the server.

**Action:**

![image](https://github.com/user-attachments/assets/6baca401-d925-4632-a92d-9731dad0f60c)

**Discord Log Channel:**

![image](https://github.com/user-attachments/assets/21ff12f1-1fb9-42db-92ce-413a8e9aaf31)

**Bot Console:**

![image](https://github.com/user-attachments/assets/4b289b11-3896-4b74-85f1-969cf70bf529)

**Log File:**

![image](https://github.com/user-attachments/assets/b77b0587-5a33-4b99-ac2e-4955a415bace)

