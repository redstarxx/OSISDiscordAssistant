# OSIS Discord Assistant
A Discord bot created for OSIS Sekolah Djuwita Batam, written on top of DSharpPlus. Developed by the Information Technology Division. Events manager, deadline reminder, server policing. All you need for your event planning under one bot.

[![.NET Core CI](https://github.com/redstarxx/DiscordBotOSIS/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/redstarxx/DiscordBotOSIS/actions/workflows/dotnet.yml)
[![Docker Image CI](https://github.com/redstarxx/DiscordBotOSIS/actions/workflows/docker-image.yml/badge.svg)](https://github.com/redstarxx/DiscordBotOSIS/actions/workflows/docker-image.yml)
![Maintainer](https://img.shields.io/badge/maintainer-redstarxx-blue)

## Contributing
Contributions are open to everyone! Please read the contributions guideline for more information.

## Requirements
- [.NET Core 5](https://dotnet.microsoft.com/download/dotnet/5.0)
- [PostgreSQL 13.3](https://www.postgresql.org/)

## Setting Up
Once you have installed all of the requirements listed above, you need to set your environment up properly before running the bot.

### Step One: PostgreSQL
You will need to log into your PostgreSQL server via `psql` utility.

Proceed to create a new table: `CREATE TABLE events(id SERIAL PRIMARY KEY, event_name VARCHAR(50), person_in_charge VARCHAR(100), event_date VARCHAR(50), event_date_culture_info VARCHAR(10), event_description VARCHAR(255), proposal_reminded BOOLEAN NOT NULL, previously_reminded BOOLEAN NOT NULL, expired BOOLEAN NOT NULL);` This table will later be used for the Events Manager feature.

Then create another new table: `CREATE TABLE counter (id SERIAL PRIMARY KEY, pollcounter SMALLINT, verifycounter SMALLINT);` Insert number 1 into the counter column: `INSERT INTO counter (pollcounter, verifycounter) VALUES(1, 1);` This table will later be used to store counter numbers.

Finally, create a new table again: `CREATE TABLE tags (id SERIAL PRIMARY KEY, tag_name VARCHAR(50), tag_content VARCHAR(3000));` This table will later be used for the tags feature.

### Step Two: Discord API
**IF YOU HAVE YOUR BOT'S CONNECTION TOKEN ALREADY, SKIP THIS STEP.**

Go to the [Discord Developer Portal](https://discord.com/developers) page and create a new app for the bot. Give it a name and an icon, and press Save Changes. Then go to Bot tab and press Add Bot. Give the bot a username and avatar, uncheck the Public Bot checkbox and press Save Changes.

Once changes are saved, press the Copy button under the token. Save the copied token as this will be used by the bot to connect to Discord in the next step.

### Step Three: Configuration
Create a new file named `config.json` in the `src` directory. Fill that file with the following format.

`{
  "token": "",
  "prefix": "",
  "connectionstring": ""
}`

- For the `token` property, add your bot's connection token between the quotation marks obtained from Step Two.
- For the `prefix` property, add your desired command prefix between the quotation marks.
- For the `connectionstring` property, add your database's connection string. A typical PostgreSQL connection string would look like this: `Host=host;Username=username;Password=password;Database=database` Change these values according to your database.

Make sure you have saved your changes before proceeding.

### Step Four: Running Your Bot
Once you have done all of the steps above, you are ready to run your bot!

### Self-Hosting
Yes, this bot can be self-hosted. And as a matter of fact, this bot is self-hosted. However, I will not provide support. You are on your own.