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

<details>
<summary>Ubuntu-based (Ubuntu, Pop!_OS, Mint, Zorin, etc)</summary>
<br/>

SupportBoi is available in the repository at repo.karlofduty.com.

**Installing the dotnet repository (Only needed for Ubuntu 24.04 and older):**
```bash
sudo add-apt-repository ppa:dotnet/backports
sudo apt update
```

**Installing the repo.karlofduty.com repository:**
```bash
wget https://repo.karlofduty.com/ubuntu/dists/ubuntu/karlofduty-repo_latest_amd64.deb
sudo apt install ./karlofduty-repo_latest_amd64.deb
sudo apt update
```

**Installing the release build:**
```bash
sudo apt install supportboi
```

**Installing the dev build:**
```bash
sudo apt install supportboi-dev
```
</details>

<details>
<summary>Other Debian-based (Debian, Kali, Deepin)</summary>
<br/>
SupportBoi is available in the repository at repo.karlofduty.com.

**Install the Debian 12 dotnet repository:**
```bash
wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
```

> [!IMPORTANT]
> The link above is for Debian 12, if you are using a different version, replace `12` with the version you are using.
> See this link for a list of all available versions: https://packages.microsoft.com/config/.

**Installing the repo.karlofduty.com repository:**
```bash
wget https://repo.karlofduty.com/debian/dists/debian/karlofduty-repo_latest_amd64.deb
sudo apt install ./karlofduty-repo_latest_amd64.deb
sudo apt update
```

**Installing the release build:**
```bash
sudo apt install supportboi
```

**Installing the dev build:**
```bash
sudo apt install supportboi-dev
```

</details>

<details>
<summary>RHEL-based (RHEL, Alma, Rocky, etc)</summary>
<br/>

SupportBoi is available in the repository at repo.karlofduty.com.

**Installing the release build:**
```bash
sudo dnf install https://repo.karlofduty.com/rhel/karlofduty-repo-latest.x86_64.rpm
sudo dnf install supportboi --refresh
```

**Installing the dev build:**
```bash
sudo dnf install https://repo.karlofduty.com/rhel/karlofduty-repo-latest.x86_64.rpm
sudo dnf install supportboi-dev --refresh
```
</details>

<details>
<summary>Other Fedora-based (Fedora, Nobara, Bazzite, etc)</summary>
<br/>

SupportBoi is available in the repository at repo.karlofduty.com.

**Installing the release build:**
```bash
sudo dnf install https://repo.karlofduty.com/fedora/karlofduty-repo-latest.x86_64.rpm
sudo dnf install supportboi --refresh
```

**Installing the dev build:**
```bash
sudo dnf install https://repo.karlofduty.com/fedora/karlofduty-repo-latest.x86_64.rpm
sudo dnf install supportboi-dev --refresh
```
</details>

<details>
<summary>Arch-based (Arch, Manjaro, EndeavourOS, SteamOS, etc)</summary>
<br/>

SupportBoi is available in the Arch User Repository as [supportboi](https://aur.archlinux.org/packages/supportboi/) and [supportboi-git](https://aur.archlinux.org/packages/supportboi-git/).
This example uses yay, but you can use any package manager with AUR support.

**Installing the release build:**
```bash
yay -S supportboi
```
**Installing the dev build:**
```bash
yay -S supportboi-git
```

> [!IMPORTANT]
> **For mariadb users:**  
> When mariadb is installed it will not automatically set up its data locations like in other distros.
> You have to run the following command to complete the installation: `sudo mariadb-install-db --user=mysql --basedir=/usr --datadir=/var/lib/mysql`

</details>

<details>
<summary>Manual Download (Windows / Other Linux)</summary>
<br/>

> [!WARNING]
> It is highly recommended to install the bot using the package managers listed above if possible.
> When manually installing you will not get additional features such as automatic updates, manual entries, system services, etc.

1. Set up a mysql-compatible server, such as MariaDB.

2. (Optional) Install .NET 9 if it isn't already installed on your system.

3. Download the bot for your operating system, either a [release version](https://github.com/KarlOfDuty/SupportBoi/releases) or a [dev build](https://jenkins.karlofduty.com/blue/organizations/jenkins/DiscordBots%2FSupportBoi/activity).
While the Windows version is fully supported it is not as well tested as the Linux one.

| Application         | Description                                                         |
|---------------------|---------------------------------------------------------------------|
| `supportboi`        | Standard Linux version.                                             |
| `supportboi-sc`     | Larger Linux version which does not require .NET to be installed.   |
| `supportboi.exe`    | Standard Windows version.                                           |
| `supportboi-sc.exe` | Larger Windows version which does not require .NET to be installed. |

</details>

# Database Setup

**TODO:** Database instructions.

# Running the Bot for the First Time

**TODO:** Instructions about running in the current directory, running using the default locations, running the system service.

[//]: # (5. Run the bot application, `./SupportBoi-<version>`, this creates a config file in the current directory.)

[//]: # ()
[//]: # (6. Set up the config, there are instructions inside. If you need more help either contact me in Discord or through an issue here.)

[//]: # ()
[//]: # (7. Restart the bot.)

[//]: # ()
[//]: # (8. Go to `Settings->Integrations->Bot->Command Permissions` in your Discord server to set up permissions for the commands.)