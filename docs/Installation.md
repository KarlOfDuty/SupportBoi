# Installation and Setup

## Installing the Bot

The bot is tested on the following operating systems:
- Arch
- Debian 12
- Fedora 42
- RHEL 9
- Ubuntu 24.04
- Windows 10

> [!NOTE]
> The bot should work on other versions of the above systems and other distributions based on them.
> You must however use an x64 architecture, Arm systems such as the Raspberry Pi are not supported at this time.
> 
> Please contact me immediately if any of the packages don't work properly on a specific distro.

### Installation Instructions

<details>
<summary><b>Ubuntu-based (Ubuntu, Pop!_OS, Mint, Zorin, etc)</b></summary>
<br/>

SupportBoi is available in the repository at repo.karlofduty.com.

**1.** Installing the dotnet repository (Only needed for Ubuntu 24.04 and older):
```bash
sudo add-apt-repository ppa:dotnet/backports
sudo apt update
```

**2.** Installing the repo.karlofduty.com repository:
```bash
wget https://repo.karlofduty.com/ubuntu/dists/ubuntu/karlofduty-repo_latest_amd64.deb
sudo apt install ./karlofduty-repo_latest_amd64.deb
sudo apt update
```

**3.** Installing the bot:
```bash
# Release build
sudo apt install supportboi

# Dev build
sudo apt install supportboi-dev
```

</details>

<details>
<summary><b>Other Debian-based (Debian, Kali, Deepin, etc)</b></summary>
<br/>

SupportBoi is available in the repository at repo.karlofduty.com.

**1.** Installing the dotnet repository:  
The url used in the `wget` command is for Debian 12, if you are using a different version, replace `12` with the version you are using.
See this link for a list of all available versions: https://packages.microsoft.com/config/.
```bash
wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
```

**2.** Installing the repo.karlofduty.com repository:
```bash
wget https://repo.karlofduty.com/debian/dists/debian/karlofduty-repo_latest_amd64.deb
sudo apt install ./karlofduty-repo_latest_amd64.deb
sudo apt update
```

**3.** Installing the bot:
```bash
# Release build
sudo apt install supportboi

# Dev build
sudo apt install supportboi-dev
```

</details>

<details>
<summary><b>RHEL-based (RHEL, Alma, Rocky, etc)</b></summary>
<br/>

SupportBoi is available in the repository at repo.karlofduty.com.

- Installing the release build:
```bash
sudo dnf install https://repo.karlofduty.com/rhel/karlofduty-repo-latest.x86_64.rpm
sudo dnf install supportboi --refresh
```

- Installing the dev build:
```bash
sudo dnf install https://repo.karlofduty.com/rhel/karlofduty-repo-latest.x86_64.rpm
sudo dnf install supportboi-dev --refresh
```
</details>

<details>
<summary><b>Other Fedora-based (Fedora, Nobara, Bazzite, etc)</b></summary>
<br/>

SupportBoi is available in the repository at repo.karlofduty.com.

- Installing the release build:
```bash
sudo dnf install https://repo.karlofduty.com/fedora/karlofduty-repo-latest.x86_64.rpm
sudo dnf install supportboi --refresh
```

- Installing the dev build:
```bash
sudo dnf install https://repo.karlofduty.com/fedora/karlofduty-repo-latest.x86_64.rpm
sudo dnf install supportboi-dev --refresh
```
</details>

<details>
<summary><b>Arch-based (Arch, Manjaro, EndeavourOS, SteamOS, etc)</b></summary>
<br/>

SupportBoi is available in the Arch User Repository as [supportboi](https://aur.archlinux.org/packages/supportboi/) and [supportboi-git](https://aur.archlinux.org/packages/supportboi-git/).
This example uses yay, but you can use any package manager with AUR support.

- Installing the release build:
```bash
yay -S supportboi
```

- Installing the dev build:
```bash
yay -S supportboi-git
```

You may see a warnign about verifying workloads during installation, this can be ignored.

**For mariadb users:**  
When mariadb is installed it will not automatically set up its data locations like in other distros.
You have to run the following command to complete the installation: 

```bash
sudo mariadb-install-db --user=mysql --basedir=/usr --datadir=/var/lib/mysql
```

</details>
<br/><br/>

> [!WARNING]
> It is highly recommended to install the bot using the package managers listed above if possible.
> When manually installing you will not get additional features such as automatic updates, manual entries, system services, etc.

<details>
<summary><b>Manual Download (Windows / Other Linux)</b></summary>
<br/>

You can download the bot manually by downloading the binary directly from the github release or jenkins build:

**1.** Set up a mysql-compatible server, such as MariaDB.

**2.** (Optional) Install .NET 9 if it isn't already installed on your system.

**3.** Download the bot for your operating system, either a [release version](https://github.com/KarlOfDuty/SupportBoi/releases) or a [dev build](https://jenkins.karlofduty.com/blue/organizations/jenkins/DiscordBots%2FSupportBoi/activity).
While the Windows versions are fully supported they are not as well tested as the Linux ones.

| Application         | Description                                                         |
|---------------------|---------------------------------------------------------------------|
| `supportboi`        | Standard Linux version.                                             |
| `supportboi-sc`     | Larger Linux version which does not require .NET to be installed.   |
| `supportboi.exe`    | Standard Windows version.                                           |
| `supportboi-sc.exe` | Larger Windows version which does not require .NET to be installed. |

</details>

# Database Setup
This guide assumes a MySQL/MariaDB installation, but it will be the same with other compatible database servers.

**1.** Start the service if it isn't already running:
```bash
sudo systemctl start mysql
# or
sudo systemctl start mariadb
```

**2.** Open a mysql prompt:
```bash
sudo mysql
# or
sudo mariadb
```

**3.** Create a database:
```sql
CREATE DATABASE supportboi;
```

**4.** Create a user with full access to the database, and reload permissions:
```sql
GRANT ALL PRIVILEGES ON supportboi.* TO 'supportboi'@'localhost' IDENTIFIED BY '<password here>';
FLUSH PRIVILEGES;
```

This will have created a database called `supportboi` and a user called `supportboi` with the password `<password here>`.

> [!NOTE]
> This example only allows connections from the local host. If you need to host the bot on a different device from the mysql server you can change `localhost` in the query to `%`. Just make sure to properly secure your system to prevent unauthorized access.

> [!TIP]
> If anything went wrong creating the user you can simply delete it using `DROP USER 'supportboi'@localhost;` and run the above query again.


# Running the Bot for the First Time

> [!TIP]
> If you want a very simple setup with its files placed in the current working directory and the bot running in your terminal, choose the basic setup.
>
> If you want to run the bot in the background as a system service that automatically starts up when the system is restarted and runs as its own user for security, choose the system service setup.

<details>
<summary><b>Basic Setup</b></summary>
<br/>

**1.** Run the bot to generate the config file:
![image](https://github.com/user-attachments/assets/b9a2e896-d128-4b01-9fbe-b9d62f6d4490)


**2.** A config file will have been generated in the current working directory. Open it in a text editor of your choice and set it up to your liking. It contains instructions for all options.

**3.** Run the bot again and it should start without issue:

![image](https://github.com/user-attachments/assets/ace4011e-445e-4e51-b261-64a18e653c46)

</details>

<details>
<summary><b>System Service Setup</b></summary>
<br/>

**1.** Open the bot config at `/etc/supportboi/config.yml` using your preferred text editor and set it up to your liking. It contains instructions for all options.

**2.** Run the bot manually as the service user once to test that it works correctly:
```bash
sudo --user supportboi supportboi --config /etc/supportboi/config.yml --transcripts /var/lib/supportboi/transcripts
```
![image](https://github.com/user-attachments/assets/f8819bd8-99e1-4891-bcaf-92d0ecb92061)

**3.** When you have the bot working properly you can turn it off again.

**4.** Starting the bot service:
```bash
sudo systemctl start supportboi
```

**5.** Checking the service status:
```bash
systemctl status supportboi
```
![image](https://github.com/user-attachments/assets/7473d4de-36f4-4064-b13f-2ab294fdeea9)

**6.** (Optional) Make the service start automatically on boot:
```bash
sudo systemctl enable supportboi
```

Showing the full service log:
```bash
journalctl -u supportboi
```

Showing the live updating log:
```bash
journalctl -fu supportboi
```

</details>

# Set up command permissions

Go to `Settings->Integrations->Bot->Command Permissions` in your Discord server to set up permissions for the commands:

![image](https://github.com/user-attachments/assets/e220808b-6f93-4efa-89a9-a2be5e0ec603)
