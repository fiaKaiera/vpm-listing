# Changelog
[v1.0.0](https://github.com/fiaKaiera/vpm-listing/releases/tag/door-1.0.0) - Initial Release

[v1.0.1](https://github.com/fiaKaiera/vpm-listing/releases/tag/door-1.0.1)
- Moved `Prefabs` folder and `Door` scene outside of Runtime
- Sound no longer plays when outside of `FarUpdateDistance`
- Added Help URL to all scripts
- Added documentation to README
- Added MIT license

[v1.1.0](https://github.com/fiaKaiera/vpm-listing/releases/tag/door-1.1.0)
- Event Listeners: You can now have other UdonBehaviours listen to door events
  - `OnDoorEnabled()`, `OnDoorDisabled()`
  - `OnDoorEnableOpened()`, `OnDoorEnableClosed()`, `OnDoorEnableLocked()`
  - `OnDoorOpened()`, `OnDoorOpenedFully()`, `OnDoorClosed()`
  - `OnDoorLocked()`, `OnDoorUnlocked()`
  - `OnDoorPickup()`, `OnDoorDrop()`
  - `OnDoorLocalHandleUseUp()`, `OnDoorLocalHandleUseDown()`
  - `OnDoorGlobalHandleUseUp()`, `OnDoorGlobalHandleUseDown()`
    - `sendGlobalHandleUseUp` and `sendGlobalHandleUseDown` is required to use these
- `DoorHandle` is now entirely optional
- `DoorHandle` is no longer continuous, as it is not supposed to be
- Added `snappingDistance`
- Added Add-on: `DoorKnocker`
- Release speed is now using interpolation to prevent sudden jitter in the wrong direction when released
- Open and close points are now cached and destroyed upon `Start()`
- All door objects now has its own icon
- Merged duplicate code
- Renamed assembly definition to match namespace
- Added documentation links in package

[v1.1.1](https://github.com/fiaKaiera/vpm-listing/releases/tag/door-1.1.1)
- Opened and closed Points have been replaced with handles, no longer requiring two extra objects
- Door object is now anchored at `x:0`, instead of `x:-1` for consistency for future `HingeDoor`
- Handle object's layer is set to `Pickup`

[v1.1.2](https://github.com/fiaKaiera/vpm-listing/releases/tag/door-1.1.1)
- Fixed Editor Assembly Definition now only targets Editor
- Fixed abstract class in RequireComponent for `DoorHandle`