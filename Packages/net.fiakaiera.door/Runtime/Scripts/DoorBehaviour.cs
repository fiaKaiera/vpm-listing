
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Rendering;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;

namespace FiaKaiera.Door
{
    [AddComponentMenu("")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [HelpURL("https://github.com/fiaKaiera/vpm-listing/blob/main/Packages/net.fiakaiera.door/README.md#details")]
#if UNITY_2021_2_OR_NEWER && UNITY_EDITOR
    [Icon(ICON_PATH)]
#endif
    public class DoorBehaviour : UdonSharpBehaviour
    {
#if UNITY_2021_2_OR_NEWER && UNITY_EDITOR
        const string ICON_PATH = "Packages/net.fiakaiera.door/Runtime/Resources/MaterialSymbolsDoorOpen.png";
#endif
        const float VOLUME_NEARLY_MUTE = 0.001f;
        const float LOW_UPDATE_RATE = 12f/60f;
        const float FAR_UPDATE_RATE = 60f;
        const ushort NETWORK_TICK_COUNT = 5; // (12 / 60)

        const string EVENT_ENABLED = "OnDoorEnabled";
        const string EVENT_DISABLED = "OnDoorDisabled";
        const string EVENT_ENABLE_OPENED = "OnDoorEnableOpened";
        const string EVENT_ENABLE_CLOSED = "OnDoorEnableClosed";
        const string EVENT_ENABLE_LOCKED = "OnDoorEnableLocked";
        const string EVENT_OPENED = "OnDoorOpened";
        const string EVENT_OPENED_FULLY = "OnDoorOpenedFully";
        const string EVENT_CLOSED = "OnDoorClosed";
        const string EVENT_LOCKED = "OnDoorLocked";
        const string EVENT_UNLOCKED = "OnDoorUnlocked";
        const string EVENT_PICKUP = "OnDoorPickup";
        const string EVENT_DROP = "OnDoorDrop";
        const string EVENT_LOCAL_HANDLE_UP = "OnDoorLocalHandleUseUp";
        const string EVENT_LOCAL_HANDLE_DOWN = "OnDoorLocalHandleUseDown";
        const string EVENT_GLOBAL_HANDLE_UP = "OnDoorGlobalHandleUseUp";
        const string EVENT_GLOBAL_HANDLE_DOWN = "OnDoorGlobalHandleUseDown";
        
        [Tooltip("If enabled, the door is locked upon instance start.")]
        [SerializeField, UdonSynced, FieldChangeCallback(nameof(Locked))] bool _locked = false;
        bool Locked {
            set => SetLocked(value);
            get => _locked;
        }
        [Tooltip("If enabled, the door will always close, even when fully open.")]
        [SerializeField] protected bool alwaysClose = false;
        [Tooltip("The distance the door will snap closed / opened.\n\nIn meters for SlidingDoor, degrees for HingeDoor. Cannot be changed in runtime.")]
        [SerializeField] protected float snappingDistance = 0.05f;
        [Tooltip("The speed the door rapidly shuts when locking or when the door is forced opened/closed via event. Setting this to 0 or less instantly open/closes the door. This value can be actively changed in Udon.\n\nIn meters for SlidingDoor, degrees for HingeDoor.")]
        public float forcedSlidingSpeed = 10f;
        
        [Tooltip("The miniumum speed the door slides towards being fully close/open when released.\nSetting this to 0 or less disables this.\n\nIn meters for SlidingDoor, degrees for HingeDoor.")]
        [SerializeField] float slidingMinSpeed = 0.25f;

        [Header("Sound")]
        [SerializeField] AudioClip sfxClosed;
        [SerializeField] AudioClip sfxOpened;
        [SerializeField] AudioClip sfxOpenedFully;
        [SerializeField] AudioClip sfxLocked;
        [SerializeField] AudioClip sfxUnlocked;
        [Space]
        [SerializeField] AudioClip sfxSliding;
        [Range(VOLUME_NEARLY_MUTE, 1f), SerializeField] float sfxSlidingMaxMoveVolume = 1f;

        [Header("Updating")]
        [Tooltip("Distance where the door updates less often. Updates every frame if it's lower than this value if not fully open/closed.")]
        [SerializeField] float lowUpdateDistance = 30f;
        [Tooltip("Distance where the door does not update since it's too far.")]
        [SerializeField] float farUpdateDistance = 50f;

        [Header("Listener Events")]
        [Tooltip("UdonBehaviours that will listen for events sent by this door.\nSee documentation for what events are sent.")]
        [SerializeField] UdonBehaviour[] listeners;
        [Tooltip("Enables sending `OnDoorGlobalHandleUseUp()` to listeners globally")]
        [SerializeField] bool sendGlobalHandleUseUp = false;
        [Tooltip("Enables sending `OnDoorGlobalHandleUseDown()` to listeners globally")]
        [SerializeField] bool sendGlobalHandleUseDown = false;

        [Header("References")]
        [Tooltip("Transform that indicates the door's position.")]
        [SerializeField] protected Transform doorTransform;

        [Space]
        [Tooltip("The collider that is enabled when the door is considered closed.")]
        [SerializeField] Collider colliderClosed;
        [Tooltip("Transform point where it is considered fully closed. Cannot be changed and removed during runtime.")]
        [SerializeField] protected Transform pointClosed;
        [Tooltip("Transform point where it is considered fully opened. Cannot be changed and removed during runtime.")]
        [SerializeField] protected Transform pointOpened;

        [Header("Optional References")]
        [Tooltip("Door's handle, VRC_Pickup included.")]
        [SerializeField] protected DoorHandle doorHandle;
        [SerializeField] OcclusionPortal occlusionPortal;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioSource slidingSource;

        // Holding
        [UdonSynced, FieldChangeCallback(nameof(HeldValue))]
        float _heldValue = -1f;
        protected float HeldValue
        {
            set => SetHeldValue(value);
            get => _heldValue;
        }
        protected float heldValueCurrent = 0f;
        protected float heldValuePrev = 0f;
        [UdonSynced, FieldChangeCallback(nameof(ReleaseSpeed))] float _releaseSpeed = 0f;
        float ReleaseSpeed
        {
            set => SetReleaseSpeed(value);
            get => _releaseSpeed;
        }

		float localReleaseSpeed = 0f;

        public bool IsLocked => _locked; 
        public bool IsOpen => heldValueCurrent > doorSnapClose;
        public float OpenPercent => heldValueCurrent;

        // Ticking
        bool isTicking = false;
        bool isTickingAlready = false;
        float tickDeltaTime = 0f;
        float tickLastTime = 0f;
        ushort networkTickCount = 0;

        // Other
        VRCPlayerApi localPlayer;
        bool isLoaded = false;
        protected Vector3 doorHandleOffset = Vector3.zero;
        float doorDistance = 0f;
        float doorSnapClose = 0f;
        float doorSnapFullOpen = 1f;
        float audioTimeout = float.MinValue;

        // =============================================================================
        #region Initialization

        void Start()
        {
            localPlayer = Networking.LocalPlayer;
            if (HandleIsValid)
                doorHandle._SetDoorBehaviour(this);
            CachePoints();
            Destroy(pointOpened.gameObject);
            Destroy(pointClosed.gameObject);
            RecalculateDistances();
            if (Networking.IsOwner(gameObject))
                OnDeserialization();
        }

        void OnEnable()
        {
            if (!isLoaded) return;
            isLoaded = false;
            _OnDoorEnabled();
            isLoaded = true;
            SendEvent(EVENT_ENABLED);
        }

        void OnDisable()
        {
            TickStop();
            SendEvent(EVENT_DISABLED);
        }

        void RecalculateDistances()
        {
            if (HandleIsValid)
                doorHandleOffset = HandleSetOffset();
            doorDistance = DoorDistanceCalc();
            doorSnapClose = Math.Abs(snappingDistance) / doorDistance;
            if (doorSnapClose > 0.5f) doorSnapClose = 0.5f;
            doorSnapFullOpen = 1 - doorSnapClose;
        }

        public override void OnDeserialization()
        {
            if (isLoaded) return;
            _OnDoorEnabled();
            isLoaded = true;
        }

        void _OnDoorEnabled()
        {
            // Lock if locked
            if (Locked)
            {
                localReleaseSpeed = -Mathf.Abs(forcedSlidingSpeed);
                HeldValueSetLerpAndPosition(0f);
                HeldStateClosed();
                SendEvent(EVENT_ENABLE_LOCKED);
            }

            // Leave it open if partially open without slidingMinSpeed
            else if (slidingMinSpeed <= 0)
			{
                localReleaseSpeed = 0f;
                HeldValueSetLerpAndPosition(HeldValue);
                if (heldValueCurrent <= doorSnapClose)
                {
                    HeldStateClosed();
                    SendEvent(EVENT_ENABLE_CLOSED);
                }
                else if (heldValueCurrent >= doorSnapFullOpen)
                {
                    HeldStateOpenedFully();
                    SendEvent(EVENT_ENABLE_OPENED);
                }
                else
                {
                    HeldStateOpened();
                    SendEvent(EVENT_ENABLE_OPENED);
                }
                    
			}

            // Have the door open/close when entered
            // No need to slowly animate it when player locally joins
            else
			{
                localReleaseSpeed = ReleaseSpeed;
                HeldValueSetLerpAndPosition(localReleaseSpeed <= 0f ? 0f : 1f);
                if (heldValueCurrent <= doorSnapClose)
                    HeldStateClosed();
                else
                    HeldStateOpenedFully();
			}

            HandleSetPickupable(!Locked);
			if (HandleGetPickupable())
				HandleUpdatePosition();
        }

        protected virtual void CachePoints() {}

        #endregion
        // =============================================================================
        #region Pickup

        public void _OnDoorPickup()
        {
            Networking.SetOwner(localPlayer, gameObject);
            _LocalDoorPickup();
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(_NetworkDoorPickup));
        }

        public void _OnDoorDrop()
        {
            _LocalDoorDrop();
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(_NetworkDoorDrop));
        }

        void _LocalDoorPickup()
        {
            networkTickCount = 0;
            localReleaseSpeed = 0f;
            HeldValueUpdate();
        }

        void _LocalDoorDrop()
        {
            // Don't really do anything if it doesn't slide
            if (slidingMinSpeed <= 0f) return;

            HeldValue = -1;
            localReleaseSpeed *= 1/tickDeltaTime;
			ReleaseSpeed = Mathf.Sign(localReleaseSpeed) * Mathf.Max(Mathf.Abs(localReleaseSpeed), slidingMinSpeed / doorDistance);
			networkTickCount = 0;
			RequestSerialization();
        }

        [VRC.SDK3.UdonNetworkCalling.NetworkCallable]
        public void _NetworkDoorPickup()
        {
            // Prevent stealing
            HandleSetPickupable(false);
            SendEvent(EVENT_PICKUP);
        }

        [VRC.SDK3.UdonNetworkCalling.NetworkCallable]
        public void _NetworkDoorDrop()
        {
            HandleSetPickupable(!Locked);
            localReleaseSpeed = ReleaseSpeed;

            // Snap if far away
            if (GetCameraDistance() > farUpdateDistance)
            {
                HeldValueSetLerpAndPosition(ReleaseSpeed < 0 ? 0 : 1);
                TickStop();
            }

            // Do not move if not sliding
            else if (slidingMinSpeed <= 0)
            {
                HeldValueSetLerpAndPosition(HeldValue);
				TickStop();
            }

            else
            {
                HeldValueSetLerpAndPosition(heldValueCurrent);
            }

            HandleUpdatePosition();
            SendEvent(EVENT_DROP);
        }

        #endregion
        // =============================================================================
        #region Locking

        void SetLocked(bool value)
        {
            if (_locked == value) return;
            _locked = value;
            
            if (_locked)
            {
                // Force the door to close if open
                if (forcedSlidingSpeed > 0f)
                {
                    if (heldValueCurrent > doorSnapClose)
                        ForceRelease(-forcedSlidingSpeed, heldValueCurrent);
                    else {
                        localReleaseSpeed = -forcedSlidingSpeed;
                        HeldValueSetLerpAndPosition(0f);
                        if (isLoaded)
                            SoundPlay(sfxLocked);
                    }
                }

                // Snap the door shut if it doesn't have slide force
                else
                {
                    HeldValueSetLerpAndPosition(0f);
                    if (!isLoaded)
                        SoundPlay(sfxLocked);
                }
            }
        }

        void SetLock(bool value)
        {
            if (Locked == value) return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(localPlayer, gameObject);
            
            Locked = value;
            
            if (value)
            {
                _LocalDoorLock();
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All,
                    nameof(_NetworkDoorLock));
            }

            else
            {
                RequestSerialization();
                //_LocalDoorUnlock();
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All,
                    nameof(_NetworkDoorUnlock));
            }
        }

        public void ToggleLock() => SetLock(!Locked);
        public void Lock() => SetLock(true);
        public void Unlock() => SetLock(false);
        void _LocalDoorLock()
        {
            HeldValue = -1;
			RequestSerialization();
        }
        //void _LocalDoorUnlock() {}

        [VRC.SDK3.UdonNetworkCalling.NetworkCallable]
        public void _NetworkDoorLock()
        {
            HandleSetPickupable(false);
            HandleDrop();
			colliderClosed.enabled = true;
            SendEvent(EVENT_LOCKED);
        }

        [VRC.SDK3.UdonNetworkCalling.NetworkCallable]
        public void _NetworkDoorUnlock()
        {
            HandleSetPickupable(true);
			HandleUpdatePosition();
			SoundPlay(sfxUnlocked);
            SendEvent(EVENT_UNLOCKED);
        }

        #endregion
        // =============================================================================
        #region Force State

        void ForceState(bool isOpen)
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(localPlayer, gameObject);
            if (Locked) Unlock();
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All,
                isOpen ? nameof(_NetworkDoorForceOpen) : nameof(_NetworkDoorForceClose));
        }

        public void ForceOpen() => ForceState(true);
        public void ForceClose() => ForceState(false);

        [VRC.SDK3.UdonNetworkCalling.NetworkCallable]
        public void _NetworkDoorForceOpen()
        {
            // Do not if door already open
            if (heldValueCurrent >= doorSnapFullOpen) return;
            ForceRelease(
                forcedSlidingSpeed,
                Mathf.Max(doorSnapClose + 0.001f, heldValueCurrent)
            );
        }

        [VRC.SDK3.UdonNetworkCalling.NetworkCallable]
        public void _NetworkDoorForceClose()
        {
            // Do not if door already closed
            if (heldValueCurrent <= doorSnapClose) return;
            ForceRelease(
                -forcedSlidingSpeed,
                Mathf.Min(doorSnapFullOpen - 0.001f, heldValueCurrent)
            );
        }

        void ForceRelease(float speed, float lerp)
        {
            HandleDrop();
            localReleaseSpeed = speed;
            heldValueCurrent = lerp;
            HeldValueSetPosition(heldValueCurrent);
            TickStart();
        }

        #endregion
        // =============================================================================
        #region Position

        void SetHeldValue(float value)
        {
            _heldValue = value;

            // Update door if held
            if (HeldValue > -1 && !isTickingAlready)
			{
				heldValueCurrent = HeldValue;
				TickStart();
			}
        }

        void SetReleaseSpeed(float value)
        {
            _releaseSpeed = value;

            // Update if released
            if (isTickingAlready)
                localReleaseSpeed = ReleaseSpeed;
        }

        void HeldValueUpdate()
        {
            float newValue = HeldValueClampSnap(HeldValueCalc());
            float newReleaseSpeed = newValue - HeldValue;
            if (newReleaseSpeed != 0)
                localReleaseSpeed = ExpDecay(localReleaseSpeed, newReleaseSpeed, 25, tickDeltaTime);
            
            HeldValue = newValue;

            if (networkTickCount % NETWORK_TICK_COUNT == 0)
			{
				RequestSerialization();
				networkTickCount = 0;
			}
            networkTickCount += 1;
        }

        void HeldValueTickHeld(bool isOwner)
        {
            // Doesn't use HeldValueSetLerp() to avoid HeldValueClampSnap()
            heldValueCurrent = isOwner ? HeldValue :
                ExpDecay(heldValueCurrent, HeldValue, EXP_DECAY_DEFAULT, tickDeltaTime);
            HeldValueSetPosition(HeldValueClampSnap(heldValueCurrent));

            if (heldValuePrev != heldValueCurrent)
            {
                if (heldValuePrev > doorSnapClose && heldValueCurrent <= doorSnapClose)
                    HeldStateClosed();
                else if (heldValueCurrent > doorSnapClose && heldValuePrev <= doorSnapClose)
                    HeldStateOpened();
                else if (heldValueCurrent >= doorSnapFullOpen && heldValuePrev < doorSnapFullOpen)
                    HeldStateOpenedFully();
                else
                    SoundSlidingLerpVolumeByMovement();
                heldValuePrev = heldValueCurrent;
            }

            else if (slidingSource.volume > VOLUME_NEARLY_MUTE)
                SoundSlidingLerpVolume(VOLUME_NEARLY_MUTE);
        }

        void HeldValueTickReleased()
        {
            HeldValueSetLerpAndPosition(
                heldValueCurrent + (localReleaseSpeed * tickDeltaTime)
            );
            SoundSlidingLerpVolumeByMovement();

            if (HandleGetPickupable())
                HandleUpdatePosition();

            if (Locked)
            {
                if (heldValueCurrent > doorSnapClose) return;
                heldValueCurrent = 0f;
                HeldStateClosed();
                SoundPlay(sfxLocked);
                TickStop();
            }

            else if (alwaysClose)
            {
                // Close as it reaches fully open
                if (heldValueCurrent >= 1f && localReleaseSpeed > 0f)
                {
                    heldValueCurrent = 1f;
                    HeldStateOpenedFully();
                    localReleaseSpeed = -localReleaseSpeed;
                }
                
                // Stop when closed
                if (heldValueCurrent <= doorSnapClose)
                {
                    heldValueCurrent = 0f;
                    HeldStateClosed();
                    TickStop();
                }
            }

            else if (!(heldValueCurrent > doorSnapClose && heldValueCurrent < doorSnapFullOpen))
            {
                // Stop when reaching closed/fully open
                heldValueCurrent = Mathf.Clamp01(heldValueCurrent);
                if (heldValueCurrent <= doorSnapClose)
                    HeldStateClosed();
                else
                    HeldStateOpenedFully();
                TickStop();
            }

            heldValuePrev = heldValueCurrent;
        }

        void HeldStateOpened()
        {
            colliderClosed.enabled = false;
            SetOcclusionPortalOpen(false);
            if (isLoaded)
            {
                SoundPlay(sfxOpened);
                SoundSlidingSetVolume(VOLUME_NEARLY_MUTE);
                SendEvent(EVENT_OPENED);
            }
        }

        void HeldStateOpenedFully()
        {
            colliderClosed.enabled = false;
            SetOcclusionPortalOpen(false);
            if (isLoaded)
            {
                SoundPlay(sfxOpenedFully);
                SoundSlidingSetVolume(VOLUME_NEARLY_MUTE);
                SendEvent(EVENT_OPENED_FULLY);
            }
        }

        void HeldStateClosed()
        {
            colliderClosed.enabled = true;
            SetOcclusionPortalOpen(true);
            if (isLoaded)
            {
                SoundPlay(sfxClosed);
                SoundSlidingSetVolume(VOLUME_NEARLY_MUTE);
                SendEvent(EVENT_CLOSED);
            }
        }

        void HeldValueSetLerp(float value) => heldValueCurrent = HeldValueClampSnap(value);
        void HeldValueSetLerpAndPosition(float value)
        {
            HeldValueSetLerp(value);
            HeldValueSetPosition(heldValueCurrent);
        }

        float HeldValueClampSnap(float value)
        {
            if (value < doorSnapClose) return 0f;
            else if (value > doorSnapFullOpen) return 1f;
            else return value;
        }

        protected virtual float DoorDistanceCalc() => 0f;
        protected virtual float HeldValueCalc() => 0f;
        protected virtual void HeldValueSetPosition(float value) {}
        protected virtual void HandleUpdatePosition() {}
        protected virtual Vector3 HandleSetOffset() => Vector3.zero;

        #endregion
        // =============================================================================
        #region Handle

        protected bool HandleIsValid => Utilities.IsValid(doorHandle);

        void HandleDrop()
        {
            if (!HandleIsValid) return;
            doorHandle.pickup.Drop();
        }

        void HandleSetPickupable(bool value)
        {
            if (!HandleIsValid) return;
            doorHandle.pickup.pickupable = value;
        }

        bool HandleGetPickupable()
        {
            if (!HandleIsValid) return false;
            return doorHandle.pickup.pickupable;
        }

        bool HandlePickupHeld()
        {
            if (!HandleIsValid) return false;
            return doorHandle.pickup.IsHeld;
        }

        public void _OnHandleUse(bool up)
        {
            if (up)
            {
                SendEvent(EVENT_LOCAL_HANDLE_UP);
                if (sendGlobalHandleUseUp)
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(_OnGlobalHandleUseUp));
            }
            else
            {
                SendEvent(EVENT_LOCAL_HANDLE_DOWN);
                if (sendGlobalHandleUseDown)
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(_OnGlobalHandleUseDown));
            }
        }

        [NetworkCallable]
        public void _OnGlobalHandleUseUp() => SendEvent(EVENT_GLOBAL_HANDLE_UP);

        [NetworkCallable]
        public void _OnGlobalHandleUseDown() => SendEvent(EVENT_GLOBAL_HANDLE_DOWN);

        #endregion
        // =============================================================================

        protected void SetOcclusionPortalOpen(bool value)
		{
			if (!Utilities.IsValid(occlusionPortal)) return;
            occlusionPortal.open = value;
		}


        // =============================================================================
        #region Sound

        void SoundPlay(AudioClip audioClip)
        {
            if (!(Utilities.IsValid(audioSource) && Utilities.IsValid(audioClip))) return;
            if (GetCameraDistance() >= lowUpdateDistance) return;

            audioSource.Stop();
            audioSource.enabled = true;
            audioSource.clip = audioClip;
            audioSource.Play();
            audioTimeout = Time.time + audioClip.length - 0.001f;
            SendCustomEventDelayedSeconds(nameof(SoundTimeout), audioClip.length, VRC.Udon.Common.Enums.EventTiming.Update);
        }

        void SoundStop()
        {
            if (!Utilities.IsValid(audioSource)) return;
            audioTimeout = float.MinValue;
            audioSource.Stop();
            audioSource.enabled = false;
        }

        public void SoundTimeout()
        {
            if (!Utilities.IsValid(audioSource)) return;
            if (Time.time < audioTimeout) return;
            SoundStop();
        }

        void SoundSlidingPlay()
        {
            if (!(Utilities.IsValid(slidingSource) && Utilities.IsValid(sfxSliding))) return;
            if (GetCameraDistance() >= lowUpdateDistance) return;

            if (!slidingSource.isPlaying)
            {
                slidingSource.enabled = true;
                slidingSource.clip = sfxSliding;
                slidingSource.Play();
            }
        }

        void SoundSlidingStop()
        {
            if (!Utilities.IsValid(slidingSource)) return;
            slidingSource.Stop();
            slidingSource.enabled = false;
        }

        void SoundSlidingSetVolume(float value)
        {
            if (!Utilities.IsValid(slidingSource)) return;
            slidingSource.volume = Mathf.Clamp01(value);
        }

        void SoundSlidingLerpVolume(float value)
        {
            if (!Utilities.IsValid(slidingSource)) return;
            // Copied and altered from MMMaelon's Sliding Door
            slidingSource.volume = ExpDecay(
                slidingSource.volume,
                Mathf.Clamp01(value) / Mathf.Max(VOLUME_NEARLY_MUTE, sfxSlidingMaxMoveVolume),
                EXP_DECAY_DEFAULT, tickDeltaTime
            );
        }

        void SoundSlidingLerpVolumeByMovement() => SoundSlidingLerpVolume(SoundSlidingHeldValueCalc());
        protected virtual float SoundSlidingHeldValueCalc() => 0f;

        #endregion
        // =============================================================================
        #region Ticking

        protected void TickStart()
        {
            isTicking = true;
            if (!isTickingAlready)
            {
                tickDeltaTime = Time.time - tickLastTime;
			    tickLastTime = Time.time;

                isTickingAlready = true;
                _OnTickStart();
                Tick();
            }
        }

        protected void TickStop()
        {
            isTicking = false;
        }

        public void Tick()
        {
            tickDeltaTime = Time.time - tickLastTime;
			tickLastTime = Time.time;

            if (!isTicking)
            {
                isTickingAlready = false;
                _OnTickEnd();
                return;
            }

            float cameraDistance = GetCameraDistance();

            // Too far, check in a second..
            if (cameraDistance > farUpdateDistance)
            {
                SendCustomEventDelayedSeconds(nameof(Tick), FAR_UPDATE_RATE);
                return;
            }

            _OnTick();
            SendCustomEventDelayedSeconds(nameof(Tick),
                cameraDistance < lowUpdateDistance ? 0f : LOW_UPDATE_RATE);
        }

        void _OnTickStart()
        {
            if (GetCameraDistance() > farUpdateDistance) return;
            SoundSlidingSetVolume(Locked ? forcedSlidingSpeed : VOLUME_NEARLY_MUTE);
            SoundSlidingPlay();
        }

        void _OnTickEnd()
        {
            SoundSlidingSetVolume(0f);
            SoundSlidingStop();
			HeldValueSetPosition(heldValueCurrent);
        }

        void _OnTick()
        {
            bool isOwner = Networking.IsOwner(gameObject);
			if (isOwner && HandlePickupHeld())
				HeldValueUpdate();
            
            if (HeldValue > -1)
                HeldValueTickHeld(isOwner);
            else
                HeldValueTickReleased();
        }

        #endregion
        // =============================================================================
        #region Event Listening

        void SendEvent(string _event)
        {
            if (listeners.Length == 0) return;
            foreach(UdonBehaviour behaviour in listeners)
            {
                if (!Utilities.IsValid(behaviour)) return;
                SendCustomEvent(_event);
            }
        }

        #endregion
        // =============================================================================
        protected float GetCameraDistance()
        {
            float distance = Vector3.Distance(VRCCameraSettings.ScreenCamera.Position, transform.position);
            if (Utilities.IsValid(VRCCameraSettings.PhotoCamera) && VRCCameraSettings.PhotoCamera.Active)
            {
                distance = Mathf.Min(distance, 
                    Vector3.Distance(VRCCameraSettings.PhotoCamera.Position, transform.position));
            }

            return distance;
        }

        // Exponential decay function
        // Useful decay range approx. 1-25, slow to fast
        // Thanks to @acegikmo for the function!
        // https://youtu.be/LSNQuFEDOyQ
        protected const float EXP_DECAY_DEFAULT = 10f;
        protected float ExpDecay(float a, float b, float decay, float deltaTime)
            => b + (a - b) * Mathf.Exp(-decay * deltaTime);

    }
}