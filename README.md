# OSIS Discord Assistant
A Discord bot created for OSIS Sekolah Djuwita Batam, written on top of DSharpPlus. Developed by the Information Technology Division. Events manager, automated deadline reminder and server policing. All you need for your student council's Discord server under one bot.

[![.NET Core CI](https://github.com/redstarxx/DiscordBotOSIS/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/redstarxx/DiscordBotOSIS/actions/workflows/dotnet.yml)
[![Docker Image CI](https://github.com/redstarxx/DiscordBotOSIS/actions/workflows/docker-image.yml/badge.svg)](https://github.com/redstarxx/DiscordBotOSIS/actions/workflows/docker-image.yml)
![Maintainer](https://img.shields.io/badge/maintainer-redstarxx-blue)

## Contributing
Contributions are open to everyone! Please read the contributions guideline for more information.

## Requirements
- [.NET Core 5](https://dotnet.microsoft.com/download/dotnet/5.0)
- [PostgreSQL](https://www.postgresql.org/)

## Setting Up
Once you have installed all of the requirements listed above, you need to set your environment up properly before running the bot.

### Step One: PostgreSQL
You will need to log into your PostgreSQL server via `psql` utility.

Proceed to create a new table: `CREATE TABLE events(id SERIAL PRIMARY KEY, event_name VARCHAR(50), person_in_charge VARCHAR(100), event_date VARCHAR(50), event_date_culture_info VARCHAR(10), event_description VARCHAR(255), proposal_reminded BOOLEAN NOT NULL, previously_reminded BOOLEAN NOT NULL, expired BOOLEAN NOT NULL);` This table is to be used for the Events Manager feature (this includes the time to event reminder and the proposal submission reminder).

 `CREATE TABLE counter (id SERIAL PRIMARY KEY, pollcounter SMALLINT, verifycounter SMALLINT);` Insert number 1 into each  column: `INSERT INTO counter (pollcounter, verifycounter) VALUES(1, 1);` This table is to be used to store counter numbers.

 `CREATE TABLE tags (id SERIAL PRIMARY KEY, tag_name VARCHAR(50), tag_content VARCHAR(3000));` This table is to be used for the tags feature.

`CREATE TABLE verification (id SERIAL PRIMARY KEY, user_id BIGINT, verification_embed_id BIGINT, requested_nickname VARCHAR(32))` This table is to be used for the main guild verification system.

### Step Two: Discord API
**IF YOU HAVE YOUR BOT'S CONNECTION TOKEN ALREADY, SKIP THIS STEP.**

Go to the [Discord Developer Portal](https://discord.com/developers) page and create a new app for the bot. Give it a name and an icon, and press Save Changes. Then go to Bot tab and press Add Bot. Give the bot a username and avatar, uncheck the Public Bot checkbox and press Save Changes.

After that, press the Copy button under the token. Save the copied token as this will be used by the bot to connect to Discord in the next step.

### Step Three: Configuration
Create a new file named `config.json` in the bot's directory. Fill that file with the following format.

```json
{
    "Token": "",
    "Prefix": [""],
    "DbConnectionString": "",
    "MainGuildId": "",
    "StatusActivityType": ,
    "CustomStatusDisplay": [""],
    "EventChannelId": "",
    "ProposalChannelId": "",
    "VerificationRequestsProcessingChannelId": "",
    "MaxPendingVerificationWaitingDay": "",
    "VerificationInfoChannelId": "",
    "RolesChannelId": "",
    "ErrorChannelId": "",
    "AccessRoleId": "",
    "MainGuildRoles": [
        {
            "RoleId": "",
            "RoleName": "",
            "RoleEmoji": ""
        }
	]
}
```

- For the `Token` property, add your bot's connection token between the quotation marks obtained from Step Two.
- For the `Prefix` property, add your desired command prefix between the quotation marks.
- For the `DbConnectionString` property, add your database's connection string. A typical PostgreSQL connection string would look like this: `Host=host;Username=username;Password=password;Database=database` Change these values according to your database.
- For the `MainGuildId` property, add the guild ID that you want the event and proposal submission reminders to be sent to.
- For the `StatusActivityType` property, add the desired ActivityType enum number as specified in the D#+ documentation.
- For the `CustomStatusDisplay` property, add the array of strings to be set as the bot's custom status which is updated on a minute basis.
- For the `EventChannelId` property, add the channel ID that you want the event reminders to be sent to.
- For the `ProposalChannelId` property, add the channel ID that you want the proposal submission reminders to be sent to.
- For the `VerificationRequestsProcessingChannelId` property, add the ID of the channel to be set as the verification requests processing channel ID (where verification request embeds are sent to).
- For the `MaxPendingVerificationWaitingDay` property, add the number of days which a pending verification is retained before being removed automatically by the verification cleanup task.
- For the `VerificationInfoChannelId`, add the ID of the channel that contains the verification info embed.
- For the `RolesChannelId` property, add the ID of the channel to be used to point newly verified members to self-assign their divisional roles via the provided select menu.
- For the `ErrorChannelId` property, add the channel ID that you want the error(s) related to event and proposal submissions reminders background task to be sent to.
- For the `AccessRoleId` property, add the ID of the role that serves as the OSIS role in the main guild.
- For the `MainGuildRoles` array, add the array of divisional roles as needed.

Make sure you have saved your changes before proceeding.

### Step Four: Running Your Bot
Once you have done all of the steps above, you are ready to run your bot! Ensure that `config.json` is in the same directory which the bot is located in.

### Self-Hosting
Assuming that you want to run your bot in a Docker container, self-hosting should be straightforward by executing `docker-compose up` from the `docker-compose.yml` file. Upon first launch, you may encounter some errors either because you have not completed [step one](https://github.com/redstarxx/OSISDiscordAssistant#step-one-postgresql) or the `config.json` file is missing from the bot's directory. These issues should not persist once they have been resolved, therefore allowing you to launch the bot by only executing `docker-compose up` without hassle.

Feel free to hit me up on Discord (RedStar#9271) for questions / support.