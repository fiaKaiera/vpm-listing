# Real-ish Doors
by [fiaKaiera](https://github.com/fiaKaiera)

### <img src="https://vcc.docs.vrchat.com/images/favicon.ico" width=24> [Add to VCC / ALCOM](https://fiakaiera.github.io/vpm-listing)
[ [*.unitypackage](https://github.com/fiaKaiera/vpm-listing/releases/download/door-1.0.1/net.fiakaiera.door-1.0.1.unitypackage) ]
[ [*.zip](https://github.com/fiaKaiera/vpm-listing/releases/download/door-1.0.1/net.fiakaiera.door-1.0.1.zip) ]

VRChat door system that acts semi-realisitically.
<br>These doors can be locked or forced open/closed using events.
<br>Currently, only sliding doors exist.

## Features
- Sliding Doors, with variable distance
- Slowly open/close the door when left on either state
- Locking via `Lock()`, `Unlock()`, and `ToggleLock()` custom events
- Force open and close through `ForceOpen()` and `ForceClose()` custom events (will unlock the door)
- (Optional) Sound effect support
- (Optional) Occlusion Portal support
- Optimizations
  - Low FPS when far away
    - Doesn't update when fully far away, will snap open/close depending on door state
    - Also respects camera distance
  - Audio-limit friendly
    - Only 2 audio sources, and disables when not in-use, and not used at all when too far away
  - Doesn't update if the door doesn't slide at all (if `SlidingMinSpeed` is `0` or less)

## How to Use
> If installing via `*.zip` make sure you extract the contents into a new folder called `net.fiakaiera.door` inside your Unity project's `Packages` folder.
1. Install Real-ish Doors
2. In your Unity project, drag the door you need under `Packages\Real-ish Doors\Prefabs`
3. Customize the door to your liking!

There's an example scene in `Packages\Real-ish Doors\Door` showing how it works!

## Details
The door prefab contains a `DoorBehaviour` object inside it, depending on what door it is. (`SlidingDoor` / `HingeDoor`)
- **Is Locked:** If enabled, the door is locked upon instance start.
- **Always Close:** If enabled, the door will always close, even when fully open.
- **Forced Sliding Speed:** The speed the door rapidly shuts when locked or forced open
  - Setting this to `0` or less will immediately close/open the door
  - This value can be changed via Udon
- **Sliding Min Speed:** The miniumum speed the door slides towards being fully close/open when released.
  - Setting this to `0` or less disables this
- **Sound**
  - **Sfx Closed:** Sound played when the door is closed.
  - **Sfx Opened:** Sound played when the door is opened from being closed.
  - **Sfx Opened Fully:** Sound played when the door is fully opened.
  - **Sfx Locked:** Sound played when the door is locked.
  - **Sfx Unlocked:** Sound played when the door is unlocked.
  - **Sfx Sliding:** Sound played when the door is sliding.
  - **Sfx Sliding Max Move Volume:** The maximum volume the door will reach when sliding the door.
- **Updating**
  - **Low Update Distance:** The distance in meters when the door starts updating slowly (12 FPS)
  - **Far Update Distance:** The distance in meters when the door will not update since it is too far
- **References:** Important objects to make the door work.
  <br>If using the prefab, all of these doesn't need to be changed at all unless modified.
  - **Door Transform:** The `Transform` where the door is located. If you need a visual for the door, it is placed here.
  - **Door Handle:** The door's handle, which is a `DoorHandle` component. Requires a `VRC Pickup` to function.
  - **Collider Closed:** The door's `Collider` when closed. Is enabled when closed.
  - **Point Closed:** The `Transform` point the door is considered closed.
  - **Point Opened:** The `Transform` point where the door is considered fully open.
- **Optional References:**
  - **Occlusion Portal:** The occlusion portal that blocks occlusion when closed.
  - **Audio Source:** The door's `AudioSource` that plays closing, opening, locking and unlocking sounds.
  - **Sliding Source:** The door's `AudioSource` that plays when the door is sliding.

### Hinge Door
Unimplemented.

### Sliding Door
A slidiing door that opens from left to right.
- If you need to rotate the door, it is better to rotate the `SlidingDoor` object itself than adjusting `PointClosed` and `PointOpened` objects.
- `PointClosed` and `PointOpened` dictate how far the doors open/close.
  - If you need to adjust these, only change its local X-position.
  - The door will not go past these points.

---
## Credits
- Real-ish Doors by [fiaKaiera](https://github.com/fiaKaiera)
- Exponential Decay Function: [acegikmo](https://acegikmo.com) (See how it works [here](https://youtu.be/LSNQuFEDOyQ))
- Sliding Sound Code: Altered from MMMaellon's [Real Fake Doors](https://github.com/MMMaellon/real-fake-doors) Sliding Door Code
- Sound Effects: From Freesound and Zapsplat, from MMMaellon's [Real Fake Doors](https://github.com/MMMaellon/real-fake-doors) to keep it consistent
  - [Heavy Door Creaking_04.wav](https://freesound.org/people/rambler52/sounds/455319/) by rambler52
  - [Snowboard_Slide_Loop_Mono_01.wav](https://freesound.org/people/Nox_Sound/sounds/612665/) by Nox_Sound
  - [Wood door, internal, close hard](https://www.zapsplat.com/music/wood-door-internal-close-hard/) by Zapsplat
  - [https://www.zapsplat.com/music/wood-door-internal-open-4/](https://www.zapsplat.com/music/wood-door-internal-open-4/) by Zapsplat
  - [Back door unlock with key, version 1](https://www.zapsplat.com/music/back-door-unlock-with-key-version-1/) by Sonic SoundFX
  - [Front door lock](https://www.zapsplat.com/music/front-door-lock/) by Zapsplat
  

## Inspiration
Seeing MMMaellon's [Real Fake Doors](https://github.com/MMMaellon/real-fake-doors) not being updated since [LightSync](https://github.com/MMMaellon/LightSync) was out for testing, but have the ability to do it optimally without the reliance of SmartObjectSync