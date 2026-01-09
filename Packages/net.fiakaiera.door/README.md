# [Real-ish Doors](https://github.com/fiaKaiera/vpm-listing/blob/main/Packages/net.fiakaiera.door)
by [fiaKaiera](https://github.com/fiaKaiera)

### <img src="https://vcc.docs.vrchat.com/images/favicon.ico" width=24> [Add to VCC / ALCOM](https://fiakaiera.github.io/vpm-listing)
[ [*.unitypackage](https://github.com/fiaKaiera/vpm-listing/releases/download/door-1.1.0/net.fiakaiera.door-1.1.0.unitypackage) ]
[ [*.zip](https://github.com/fiaKaiera/vpm-listing/releases/download/door-1.1.0/net.fiakaiera.door-1.1.0.zip) ]

VRChat door system that acts semi-realisitically.
<br>These doors can be locked or forced open/closed using events.
<br>Currently, only sliding doors exist.

[ [Changelog](https://github.com/fiaKaiera/vpm-listing/blob/main/Packages/net.fiakaiera.door/CHANGELOG.md) ]

## Features
- Sliding Doors, with variable distance
- Slowly open/close the door when left on either state
- Snapping the door open/closed by a minimum value
- Locking via `Lock()`, `Unlock()`, and `ToggleLock()` custom events
- Force open and close through `ForceOpen()` and `ForceClose()` custom events (will unlock the door)
- Event listening from other UdonBehaviours (see [Events](#variables-functions--events) below)
- Doors can be dynamically repositioned if not static, as position is calculated locally
- Doors can be disabled and still sync when re-enabled
- Late-joiner friendly
- Clicking the help docs leads here :)
- (Optional) Disable/remove the door's handle entirely (via disabling the GameObject or removing the reference)
- (Optional) Sound effect support
- (Optional) Occlusion Portal support
- Optimizations
  - Low FPS when far away
    - Doesn't update when fully far away, will snap open/close depending on door state
    - Also respects camera distance
  - Audio-limit friendly
    - Only 2 audio sources, and disables when not in-use, and not used at all when too far away
  - Doesn't update if the door doesn't slide at all (if `SlidingMinSpeed` is `0` or less)
  - Entirely runs on `SendCustomEventDelayedFrames` instead of `Update`
  - Only use 3 `UdonSynced` variables (2 floats, 1 bool)
    - Entirely Manual and doesn't sync every frame (12 network ticks per second)
- Add-ons
  - [Door Knocker](#door-knocker): Allows the door to be knocked and be heard globally

## How to Use
> If installing via `*.zip` make sure you extract the contents into a new folder called `net.fiakaiera.door` inside your Unity project's `Packages` folder.
1. Install Real-ish Doors
2. In your Unity project, drag the door you need under `Packages\Real-ish Doors\Prefabs`
<br>※ You can also copy them from the example scene found in `Packages\Real-ish Doors\Door` showing how the door works!
3. Customize the door to your liking!

## Details
The door prefab contains a `DoorBehaviour` object inside it, depending on what door it is. (`SlidingDoor` / `HingeDoor`)
- **Locked:** If enabled, the door is locked upon instance start.
  - Value can be obtained via `IsLocked` variable and can only be changed with `Lock()` and `Unlock()` events
- **Always Close:** If enabled, the door will always close, even when fully open.
- **Snapping Distance:** The distance the door will snap closed / opened.
  - In meters for `SlidingDoor`, in degrees for `HingeDoor`
  - Cannot be changed during runtime
- **Forced Sliding Speed:** The speed the door rapidly shuts when locked or forced open.
  - Setting this to `0` or less will immediately close/open the door
  - In meters for `SlidingDoor`, in degrees for `HingeDoor`
  - This value can be changed via Udon
- **Sliding Min Speed:** The miniumum speed the door slides towards being fully close/open when released.
  - Setting this to `0` or less disables this
  - In meters for `SlidingDoor`, in degrees for `HingeDoor`
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
- **Listener Events**
  - **Listeners:** UdonBehaviours that will listen for [custom events](#variables-functions--events) sent by this door.
  - **Send Global Handle Use Up:** Enables sending `OnDoorGlobalHandleUseUp()` to listeners globally.
  - **Send Global Handle Use Down:** Enables sending `OnDoorGlobalHandleUseDown()` to listeners globally.
- **References:** Important objects to make the door work.
  <br>If using the prefab, all of these doesn't need to be changed at all unless modified.
  - **Door Transform:** The `Transform` where the door is located. If you need a visual for the door, it is placed here.
  - **Collider Closed:** The door's `Collider` when closed. Is enabled when closed.
  - **Point Closed:** The `Transform` point the door is considered closed.
  - **Point Opened:** The `Transform` point where the door is considered fully open.
- **Optional References:**
  - **Door Handle:** The `DoorHandle` that makes the door's handle. Requires a `VRC Pickup` to function.
  - **Occlusion Portal:** The `OcclusionPortal` that blocks occlusion when closed.
  - **Audio Source:** The door's `AudioSource` that plays closing, opening, locking and unlocking sounds.
  - **Sliding Source:** The door's `AudioSource` that plays when the door is sliding.

### Variables, Functions & Events
Public Variables
- `IsLocked` - If the door is locked.
- `IsOpen` - If the door is open. This uses a calculation, so cache if needed.
- `OpenPercent` - The percentage the door is opened.
- `forcedSlidingSpeed` - See **Forced Sliding Speed**.

These functions can be called through UdonSharp as functions or through "Send Custom Event" in Udon Graph.
- `Lock()` - Locks the door, does nothing when already locked
- `Unlock()` - Unlocks the door, does nothing when already unlocked
- `ToggleLock()` - Toggles the door's lock state. Depending on its state, it will automatically call `Lock()` or `Unlock()`
- `ForceOpen()` - Forces the door open, unlocking the door if it is locked
- `ForceClose()` - Forces the door closed, unlocking the door if it is locked
  - If you want to force close the door and lock it at the same time, use `Lock()` instead

For listeners, these will be the events the door will send.
- `OnDoorEnabled()` - Triggers when the door (or its gameObject) is enabled
  - Triggers after `OnDoorEnableOpened()`, `OnDoorEnableClosed()`, or `OnDoorEnableLocked()`
- `OnDoorDisabled()` - Triggers when the door (or its gameObject) is disabled
- `OnDoorEnableOpened()` - Triggers when the door on start or is enabled while in the open state
- `OnDoorEnableClosed()` - Triggers when the door on start or is enabled while in the closed state
- `OnDoorEnableLocked()` - Triggers when the door on start or is enabled while in the locked state
  - `OnDoorEnableOpened()` and `OnDoorEnableClosed()` is not sent when locked
- `OnDoorOpened()` - Triggers when the door is opened from being closed
- `OnDoorOpenedFully()` - Triggers when the door is opened fully
- `OnDoorClosed()` - Triggers when the door is closed from being opened
- `OnDoorLocked()` - Triggers when the door is locked, before the door is closed if opened (will trigger OnDoorClosed if locked from open)
- `OnDoorUnlocked()` - Triggers when the door is unlocked
- `OnDoorPickup()` - Triggers when the door handle is picked up
- `OnDoorDrop()` - Triggers when the door handle is dropped
- `OnDoorLocalHandleUseDown()` - Triggers when the handle's use is pressed locally
- `OnDoorLocalHandleUseUp()` - Triggers when the handle's use is released locally
- `OnDoorGlobalHandleUseDown()` - Triggers when the handle's use is pressed globally
  - `OnDoorLocalHandleUseDown()` triggers first locally
  - Requires `sendGlobalHandleUseUp` to be enabled
- `OnDoorGlobalHandleUseUp()` - Triggers when the handle's use is released globally
  - `OnDoorLocalHandleUseUp()` triggers first locally
  - Requires `sendGlobalHandleUseDown` to be enabled

### Hinge Door
Unimplemented.

### Sliding Door
A sliding door that opens from left to right.
- If you need to rotate the door, it is better to rotate the `SlidingDoor` transform itself than adjusting the `PointClosed` and `PointOpened` objects.
- `PointClosed` and `PointOpened` dictate how far the doors open/close.
  - If you need to adjust these, only change its local X-position.
  - The door will not go past these points.
  - Cannot be adjusted once enabled during runtime.

### Door Knocker
An interactible `Collider` that plays a knocking sound for everyone to hear.
<br>Attach this as a listener to any `DoorBehaviour` (`SlidingDoor` / `HingeDoor`) to make it automatically disable when the door is opened.

The scene example shows it separate, but can be attached to the door if needed.

- **Audio Source:** Required, an `AudioSource` that will play the knocking sound.
- **Pitch Min:** The minimum pitch for the sound when the door is knocked.
- **Pitch Max:** The minimum pitch for the sound when the door is knocked.
- **Listeners:** 
  - `OnDoorKnock()` - Triggers when the door is knocked


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
- Icon: `material-symbols:door-open` from [Material Symbols](https://github.com/google/material-design-icons), fetched from [Icônes](https://icones.js.org/collection/material-symbols)

## Inspiration
Seeing MMMaellon's [Real Fake Doors](https://github.com/MMMaellon/real-fake-doors) not being updated since [LightSync](https://github.com/MMMaellon/LightSync) was out for testing, but have the ability to do it optimally without the reliance of SmartObjectSync