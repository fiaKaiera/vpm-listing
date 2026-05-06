# [Unique Spawnpoints](https://github.com/fiaKaiera/vpm-listing/blob/main/Packages/net.fiakaiera.spawnpoints)
by [fiaKaiera](https://github.com/fiaKaiera)

### <img src="https://vcc.docs.vrchat.com/images/favicon.ico" width=24> [Add to VCC / ALCOM](https://fiakaiera.github.io/vpm-listing)
[ [*.unitypackage](https://github.com/fiaKaiera/vpm-listing/releases/download/door-1.0.1/net.fiakaiera.spawnpoints-1.0.1.unitypackage) ]
[ [*.zip](https://github.com/fiaKaiera/vpm-listing/releases/download/door-1.0.1/net.fiakaiera.spawnpoints-1.0.1.zip) ]

[ [Changelog](https://github.com/fiaKaiera/vpm-listing/blob/main/Packages/net.fiakaiera.spawnpoints/CHANGELOG.md) ] [ [How to Install?](https://github.com/fiaKaiera/vpm-listing/wiki#how-to-install) ]

<a href='https://ko-fi.com/fiaKaiera' target='_blank'><img height='36' style='border:0px;height:36px;' src='https://storage.ko-fi.com/cdn/kofi1.png?v=6' border='0' alt='Buy Me a Coffee at ko-fi.com' /></a>

> Unity > Package Manager > Add from git URL:
> <br>`https://github.com/fiaKaiera/vpm-listing.git?path=/Packages/net.fiakaiera.spawnpoints`

## Features
- Override VRC Scene Descriptor's default spawns
  - Useful in situations where you "hide the player until fully loaded" scenarios
- Assign unique spawnpoints to specific users
  - Use the system's children as unique spawnpoints to specific users
- Assign a default spawn only for the instance owner
- Save and load spawnpoints through persistence
- Event listening for other UdonBehaviours (see [Events](#variables-functions--events) below)
- Clicking the help docs leads here :)

## How To Use
> If installing via `*.zip` make sure you extract the contents into a new folder called `net.fiakaiera.spawnpoints` inside your Unity project's `Packages` folder.
1. Install Unique Spawnpoints
2. On a blank Game Object, add a component: "fiaKaiera > Spawns > Spawnpoint System"
3. Customize the Spawnpoint System to your liking!

## Attribution
Simply put: It would be appreciated if you credit when you use this asset to fiaKaiera.
<br>It can be in any form as long as it is clear and concise.

**Example:**
```
Unique Spawnpoints by fiaKaiera
https://github.com/fiaKaiera/vpm-listing
```

It's entirely optional but if you do, it will help spread the word and supports the growth of this asset within the VRChat community.

---

## Details
The system runs on a "Spawnpoint System" component inside a game object.
**NOTE:** You should only have one instance of the Spawnpoint System running as having multiple instances of them will have unintended effects.

- **Start Delay:** - How soon the player gets teleported to their assigned spawn in frames or seconds. (decimals discarded in frames)
- **Respawning**
  - **Spawn Override:** - Overrides where the player respawns by default and sends them there after start delay upon joining the world. Works similar to VRC Scene Descriptor's Spawn Order.
    - **Override Spawns:** - Dictates where the players respawn when overridden. You can move the relocate the transforms around and they will respawn there instead. Works similar to VRC Scene Descriptor's Spawns list.
    - **Override Spawn Radius:** - Players spawn at a random position within the radius. Set this to 0 to be precise. Works similar to VRC Scene Descriptor's Spawn Radius.
    - **Override Spawn Orientation:** - The orientation where the players respawn. Works similar to VRC Scene Descriptor's Spawn Orientation.
  - **Backup Respawn:** - The behavior when the player respawns twice in a row within a time period.
    - [Assigned Spawn First] will send you the assigned spawn first, then default spawn second. [Default Spawn First] is vice-versa.
    - [Assigned Spawn Only] will send you to the assigned spawn only. If it's not assigned it will send your to default spawn.
    - [Default Spawn Only] will send you to the default spawn regardless if there's an assigned spawn.
  - **Backup Respawn Seconds:** - The amount of seconds the player is sent to the backup respawn if they respawned within the time period.
    - Disabled if it's set to [Assigned Spawn Only] or [Default Spawn Only]
- **Persistence**
  - **Persistent Savepoints:** - Allows saving and loading of savepoints via "[Save](#variables-functions--events)" functions
    - If enabled, the system will forcibly wait for the local player's data to be loaded first if the start delay is faster before loading player data.
    - If any user has a spawnpoint saved, this will replace any assigned spawnpoint, even if it's a user spawnpoint or instance owner spawnpoint.
      - The only way to remove that override is through any script that calls `SpawnClear()`
  - **Persistent Spawns:** - A set of transforms that are considered persistent spawns. Any Transforms that are not in the list when saving is not considered.
    - This is a performance measure, as checking every object will be end up slow when there are a large amount of objects in the scene to scan.
  - **Persistent Key:** - The key used when saving the current spawn with persistence. (Default: "saved_spawnpoint")
- **Instance Owner Spawn**
  - **Owner Spawn:** - Sets the spawn of the instance owner. Can be moved around.
  - **Owner Spawn Behaviour:** The behaviour of the instance owner's spawn.
    - [Assigned Spawn] sets it as the their assigned spawn, even if they have a user spawn.
    - [Assigned If First] only assigns their spawn if they're the first user in the instance. (Changes to Default/User Spawn upon rejoining)
    - [Use User Spawn] sets their assigned spawn to their own user spawn upon respawn. (Default if nothing is assigned)
    - [Use Default Spawn] uses the default spawn upon respawn.
- **User Spawns**
  - NOTE: For clarity, user spawns uses the player's *display name* on the object's name.
    - Example: If the user is "Tupper", the object's name must also be "Tupper".
    - We suggest you copy the user's display name as is in [VRChat's website](https://vrchat.com/home) to catch any character changes from their system
  - **Children As User Spawns:** - Uses this object's children as user spawns. Good for having all user spawns within one object.
  - **Assigned User Spawns:** - The positions where the player respawns. You can actually relocate this object and they will spawn there instead.
    - Use this if you want to anchor user spawns to other objects.
- **Listener Events**
  - **Listeners:** - UdonBehaviours that will listen for [custom events](#variables-functions--events) sent by the system.

### Variables, Functions & Events
NOTE: The `DefaultExecutionOrder` of this is set to `-1`, so other UdonBehaviours can run `OnPlayerRespawn()` after the spawnpoint system does what its supposed to do.

Public Variables
- `float` `overrideSpawnRadius` - spawn radius if spawn override is not set to use Scene Descriptor.
  - It's configured like this because variables from VRCSceneDescriptor cannot be accessed via Udon.
- `string` `persistentKey` - the key used in persistence data.
- `VRCPlayerApi` `lastRespawnedPlayer` - the last player who respawned. Useful with events.

These functions can be called through UdonSharp as functions or through "Send Custom Event" in Udon Graph.
- `Respawn()` - Respawns the local player as if they triggered `OnPlayerRespawn` themselves

These functions can only be called with arguments
- Only functions with Persistent Savepoints enabled
    - `void` `SpawnSave(int id, bool assignSpawn = true)` - saves the local player's spawnpoint using the index id within  `persistentSpawns` and stores it as an `int` in `PlayerData`. If `assignSpawn` is true, this also sets it as the assigned spawnpoint.
    - `void` `SpawnSave(Transform savepoint, bool assignSpawn = true)` - saves the local player's spawnpoint with a specified `Transform` within `persistentSpawns` and stores it as an `int` in `PlayerData`. If `assignSpawn` is true, this also sets it as the assigned spawnpoint.
    - `void` `SpawnSaveAsName(Transform savepoint, bool assignSpawn = true)` - saves the local player's spawnpoint as a `string` in `PlayerData`. There should be a `Transform` with the same name within `persistentSpawns` for this to properly load. If `assignSpawn` is true, this also sets it as the assigned spawnpoint.
    - `void` `SpawnSave(Vector3 position, Quaternion rotation, bool assignSpawn = true)` - saves the local player's spawnpoint as a `DataList` containing 7 `float`s in `PlayerData` that specifies the given `position` then `rotation`. When loaded, this will create a placeholder transform as the assigned spawnpoint. If `assignSpawn` is true, this also sets it as the assigned spawnpoint.
- `void` `SpawnClear(bool assignSpawn = true)` - If it exists, sets the local player's spawnpoint to `false` in `PlayerData`. Can be used even with Persistent Savepoints OFF. If `assignSpawn` is true, this also sets the assigned spawnpoint to `null`.
    - This is due to VRChat not having the ability to remove keys in `PlayerData`, so it can only be set to a value that is not `null`.
- `void` `AssignSpawn(Transform newSpawn)` - Sets the assigned spawn to the given `newSpawn`.
- `Transform` `GetAssignedSpawn()` - Returns the current assigned spawn. Returns `null` if there's nothing assigned.

For listeners, these will be the events the spawnpoint system will send.
- `OnLocalFirstSpawn()` - Triggers when the local player first spawns
    - Triggers before `OnLocalAssignedRespawn()`, `OnLocalOverrideRespawn()`, or `OnLocalDefaultRespawn()`
- `OnLocalAssignedRespawn()` - Triggers when the local player respawns to an assigned spawnpoint. If the owner spawn is set, this will also trigger regardless of owner spawn behaviour
- `OnLocalOverrideRespawn()` - Triggers when the local player respawns to an overridden spawnpoint 
- `OnLocalDefaultRespawn()` - Triggers when the local player respawns to a VRC Scene Descriptor spawnpoint
- `OnLocalBackupRespawn()` - Triggers when the local player respawns twice to a backup respawn
    - Does not trigger when backup respawn is set to "Assigned Spawn Only" or "Default Spawn Only"
    - Triggers before `OnLocalAssignedRespawn()`, `OnLocalOverrideRespawn()`, `OnLocalDefaultRespawn()`, or `OnPostRespawn()`
- `OnPostRespawn()` - Triggers after the player respawns
    - Triggers after `OnLocalAssignedRespawn()`, `OnLocalOverrideRespawn()`, `OnLocalDefaultRespawn()`, or `OnLocalBackupRespawn()`
- `OnLocalSpawnAssigned()` - Triggers after the local player's spawnpoint is assigned
    - Triggers before `OnLocalSpawnLoaded()`, `OnLocalSpawnSaved()` or `OnLocalSpawnCleared()`
- `OnLocalSpawnLoaded()` - Triggers after the local player's spawnpoint is loaded
- `OnLocalSpawnSaved()` - Triggers after the local player's spawnpoint is saved
- `OnLocalSpawnCleared()` - Triggers after the local player's spawnpoint is cleared

---

## Issues? Feature Requests?
Best report them through the [Issues](https://github.com/fiaKaiera/vpm-listing/issues) tab.
<br>If you are savvy enough, then you can try making a [pull request](https://github.com/fiaKaiera/vpm-listing/pulls) fixing the issue.

## Credits
- Unique Spawnpoints by [fiaKaiera](https://github.com/fiaKaiera)
- Icon: `material-symbols-light:location-on` from [Material Symbols Light](https://github.com/google/material-design-icons), fetched from [Icônes](https://icones.js.org/collection/material-symbols-light)

## Inspiration
The system is created first for a world called HopCat Hometown (private world) where multiple residents live in the same instance.
<br>This was cleaned up, honed and shared to others through fiaKaiera's talk [A Shared Home: How to Live with Friends in VRChat](https://github.com/fiaKaiera/vrc-shared-spaces) since there is no powerful solution to having unique spawnpoints.