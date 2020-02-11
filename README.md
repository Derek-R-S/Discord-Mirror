# Discord-Mirror Transport

Discord-Mirror is a networking transport for [Mirror](https://github.com/vis2k/Mirror) that enables sending networking packets via [Discord's Game SDK](https://discordapp.com/developers/docs/game-sdk/sdk-starter-guide).

## Why?

The point of this is for if you plan on releasing your game on Discord. This also provides benefits like:
* All UDP, including RUDP
* Encrypted through discords backend
* No IP leaks

## Prerequisites

For this to properly work, you do need the following things install into your unity project.

* [Mirror](https://github.com/vis2k/Mirror)
* [Discord's Game Sdk](https://discordapp.com/developers/docs/game-sdk/sdk-starter-guide)
* [Latest Discord-Mirror](https://github.com/Derek-R-S/Discord-Mirror/releases)

## Setting Up

To use, you need to initialize the discord transport. This sets up the callbacks and variables. 
To do this, after setting up your discord client simply call the following, passing the client in the constructor.

```c#
transport.Initalize(client);
```

There is also a testing script you are welcome to use, but its not recommended to use for production. To use the testing script, put "DiscordManager" on a gameobject in your scene and set up its variables accordingly.

## Connecting

To connect to a server, you need the lobby activity secret. The host can provide this for you by calling
```c#
transport.GetConnectString();
```
The clients can also get the activity secret by matchmaking, but thats something you need to create.

## License
[MIT](https://choosealicense.com/licenses/mit/)