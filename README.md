Are you tired of being the only one investing in your hideout? Want to share production rewards with others?

SharedHideout is a mod that allows you to have a shared hideout between players. Area upgrades, production states, and rewards are synchronized between players, including bonuses given by area upgrades and item crafting. You can work together towards completing the hideout and sharing the cost of investment and rewards!

There are configurations (currently very limited) for you to decide what should be synchronized. As development continues, more features will be added.

# Disclaimer
This is an early version. There are most likely bugs. **You have been warned.**

Please help with development by reporting bugs and edge cases using [GitHub Issues](https://github.com/trippyone/SharedHideout/issues).

# Reporting Bugs
When reporting bugs in GitHub Issues, make sure that you provide a clear description of the issue. You need to provide:
- Did you create a new profile? Are you using the same edition across all players?
- The hideout area affected by the issue (Water Collector, Generator, etc.)
- The action taken (start producing, retrieving production, placing an item in an area slot, upgrading, etc.)
- Did the synchronization fix itself by restarting the game?

# How to Install
- Place `SharedHideoutServer.dll` in ```SPT\user\mods```. Only required by the server host.
- Place `SharedHideoutClient.dll` in ```BepInEx\plugins```. Required for all players.

# Requirements
- **A new playthrough is required.** You need to create new profiles to have the hideout synchronized properly. If you use an existing profile, the hideout progression won't match and you will encounter issues.
- Create profiles with the same edition to avoid synchronization issues.
- SPT 4.0.13 ONLY. Do not use on any other version.

# Features
## Area Upgrade Synchronization
Area upgrades will be synchronized with other players. The cost of the upgrade is only paid by the player who upgraded the area.

## Area State Synchronization
Area states will be synchronized with other players. Examples: generator toggle, placing items in area slots, producing items, etc. This means that if a player starts a production, you will not be able to use that area to produce items until the production is complete. Area upgrade synchronization must be enabled for this to work.

## Production Rewards Synchronization
Production rewards can be obtained by other players if enabled. This means that if a player produces an item and retrieves it, other players will also receive that item free of cost. However, you must make sure your inventory has free space, otherwise the item will not be given.

## Circle of Cultist Synchronization
The Circle of Cultist can be synchronized independently of other areas. Rewards can also be shared, and the rewards can also differ per player from the same sacrifice recipe.

# Not Implemented
- Mannequin synchronization
- Gun rack synchronization
- Possibly more that I've missed

# Configuration
The configuration is available in ```SharedHideoutServer\config.json```. It will be generated when SPT is launched with the mod installed. Here is the current format:
```json
{
  "sync": {
    "areaUpgrade": true, // Enable or disable area upgrade sync
    "areaState": {
      "enabled": true, // Enable or disable area state sync
      "rewards": true  // Enable or disable sharing production rewards
    },
    "circleOfCultist": {
      "enabled": true, // Enable or disable circle of cultist sync
      "rewards": true, // Enable or disable sharing sacrifice rewards
      "sameRewards": true // Whether sacrifice rewards should be the same or different for players
    }
  }
}
```

# Notes
- Area upgrade costs and item production costs are only paid by the player who initiated them.
- In most cases, a synchronization issue will fix itself by restarting the game. Please report if that is not the case.

# Building
This project was made using Visual Studio 2026.

## SharedHideoutServer
SharedHideoutServer already contains the necessary packages to build against SPT.

## SharedHideoutClient
SharedHideoutClient requires the following dependencies to build:

- BepInEx librairies in: ```SharedHideoutClient\Librairies\BepInEx```
- Escape From Tarkov librairies in: ```SharedHideoutClient\Librairies\EscapeFromTarkov```
- SPT core librairies in: ```SharedHideoutClient\Librairies\SPT```