
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Rendering;
using VRC.SDKBase;
using VRC.Udon;

namespace FiaKaiera.Door
{
    [AddComponentMenu("")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DoorBehaviour : UdonSharpBehaviour
    {
        
        const float VOLUME_NEARLY_MUTE = 0.001f;
        const float LOW_UPDATE_RATE = 12f/60f;
        const float FAR_UPDATE_RATE = 60f;
        const ushort NETWORK_TICK_COUNT = 5; // (12 / 60)
        
        [Tooltip("If enabled, the door is locked upon instance start.")]
        [SerializeField, UdonSynced, FieldChangeCallback(nameof(IsLocked))] bool _isLocked = false;
        bool IsLocked {
            set => SetIsLocked(value);
            get => _isLocked;
        }
        [Tooltip("If enabled, the door will always close, even when fully open.")]
        [SerializeField] protected bool alwaysClose = false;
        [Tooltip("The speed the door rapidly shuts when locking or when the door is forced opened/closed via event. Setting this to 0 or less instantly open/closes the door. This value can be actively changed in Udon.")]
        public float forcedSlidingSpeed = 10f;
        
        [Tooltip("The miniumum speed the door slides towards being fully close/open when released.\nSetting this to 0 or less disables this.")]
        [SerializeField] protected float slidingMinSpeed = 0.25f;        

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
        [Space]
        [Header("References")]
        [Tooltip("Transform that indicates the door's position.")]
        [SerializeField] protected Transform doorTransform;
        [Tooltip("Door's handle, VRC_Pickup included.")]
        [SerializeField] protected DoorHandle doorHandle;

        [Space]
        [Tooltip("The collider that is enabled when the door is considered closed.")]
        [SerializeField] Collider colliderClosed;
        [Tooltip("Transform point where it is considered fully closed.")]
        [SerializeField] protected Transform pointClosed;
        [Tooltip("Transform point where it is considered fully opened.")]
        [SerializeField] protected Transform pointOpened;

        [Header("Optional References")]
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
        protected float heldValueLerp = 0f;
        protected float heldValuePrev = 0f;
        [UdonSynced, FieldChangeCallback(nameof(ReleaseSpeed))] float _releaseSpeed = 0f;
        float ReleaseSpeed
        {
            set => SetReleaseSpeed(value);
            get => _releaseSpeed;
        }

		float localReleaseSpeed = 0f;

        // Ticking
        bool isTicking = false;
        bool isTickingAlready = false;
        protected bool IsTickingAlready => isTickingAlready;
        protected float tickDeltaTime = 0f;
        float tickLastTime = 0f;
        ushort networkTickCount = 0;

        // Other
        VRCPlayerApi localPlayer;
        bool isLoaded = false;
        protected Vector3 doorHandleOffset = Vector3.zero;
        float doorDistance = 0f;
        float audioTimeout = float.MinValue;

        // =============================================================================
        #region Initialization

        void Start()
        {
            localPlayer = Networking.LocalPlayer;
            doorHandle._SetDoorBehaviour(this);
            doorHandleOffset = HandleSetOffset();
            doorDistance = DoorDistanceCalc();
        }

        public override void OnDeserialization()
        {
            if (isLoaded) return;
            isLoaded = true;
            _OnDoorLoaded();
        }

        void _OnDoorLoaded()
        {
            // Lock if locked
            if (IsLocked)
            {
                localReleaseSpeed = -Mathf.Abs(forcedSlidingSpeed);
				heldValueLerp = 0f;
				colliderClosed.enabled = true;
                SetOcclusionPortalOpen(false);
            }

            // Leave it open if partially open without slidingMinSpeed
            else if (slidingMinSpeed <= 0)
			{
				localReleaseSpeed = 0;
				heldValueLerp = HeldValue;
				colliderClosed.enabled = HeldValue <= 0;
				SetOcclusionPortalOpen(colliderClosed.enabled);
			}

            // Have the door open/close when entered
            // No need to slowly animate it on enter
            else
			{
				localReleaseSpeed = ReleaseSpeed;
				heldValueLerp = ReleaseSpeed < 0 ? 0 : 1;
				colliderClosed.enabled = ReleaseSpeed < 0;
				SetOcclusionPortalOpen(colliderClosed.enabled);
			}

            HeldValueSetPosition(heldValueLerp);
            doorHandle.pickup.pickupable = !IsLocked;
			if (doorHandle.pickup.pickupable)
				HandleUpdatePosition();
        }

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
            doorHandle.pickup.pickupable = false;
        }

        [VRC.SDK3.UdonNetworkCalling.NetworkCallable]
        public void _NetworkDoorDrop()
        {
            doorHandle.pickup.pickupable = !IsLocked;
            HandleUpdatePosition();
            localReleaseSpeed = ReleaseSpeed;

            // Do not move if not sliding
            if (slidingMinSpeed <= 0)
            {
                heldValueLerp = HeldValue;
				HeldValueSetPosition(heldValueLerp);
				TickStop();
            }

            // Snap if far away
            if (GetCameraDistance() > farUpdateDistance)
            {
                heldValueLerp = ReleaseSpeed < 0 ? 0 : 1;
                HeldValueSetPosition(heldValueLerp);
                TickStop();
            }
        }

        #endregion
        // =============================================================================
        #region Locking

        void SetIsLocked(bool value)
        {
            if (_isLocked == value) return;
            _isLocked = value;
            
            if (_isLocked)
            {
                // Force the door to close if open
                if (forcedSlidingSpeed > 0f)
                {
                    localReleaseSpeed = -forcedSlidingSpeed;
                    if (heldValueLerp > 0f)
                        TickStart();
                    else {
                        heldValueLerp = 0f;
                        SoundPlay(sfxLocked);
                    }
                }

                // Snap the door shut if it doesn't have slide force
                else
                {
                    heldValueLerp = 0f;
                    HeldValueSetPosition(heldValueLerp);
                    SoundPlay(sfxLocked);
                }
            }
        }

        void SetLock(bool value)
        {
            if (IsLocked == value) return;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(localPlayer, gameObject);
            
            IsLocked = value;
            
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

        public void ToggleLock() => SetLock(!IsLocked);
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
            doorHandle.pickup.pickupable = false;
			doorHandle.pickup.Drop();
			colliderClosed.enabled = true;
        }

        [VRC.SDK3.UdonNetworkCalling.NetworkCallable]
        public void _NetworkDoorUnlock()
        {
            doorHandle.pickup.pickupable = true;
			HandleUpdatePosition();
			SoundPlay(sfxUnlocked);
        }

        #endregion
        // =============================================================================
        #region Force State

        void ForceState(bool isOpen)
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(localPlayer, gameObject);
            if (IsLocked) Unlock();
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All,
                isOpen ? nameof(_NetworkDoorForceOpen) : nameof(_NetworkDoorForceClose));
        }

        public void ForceOpen() => ForceState(true);
        public void ForceClose() => ForceState(false);

        [VRC.SDK3.UdonNetworkCalling.NetworkCallable]
        public void _NetworkDoorForceOpen()
        {
            // Do not if door already open
            if (heldValueLerp > 1f) return;

			doorHandle.pickup.Drop();
			localReleaseSpeed = forcedSlidingSpeed;
			heldValueLerp = Mathf.Max(0.001f, heldValueLerp);
			TickStart();
        }

        [VRC.SDK3.UdonNetworkCalling.NetworkCallable]
        public void _NetworkDoorForceClose()
        {
            // Do not if door already closed
            if (heldValueLerp < 0f) return;

			doorHandle.pickup.Drop();
			localReleaseSpeed = -forcedSlidingSpeed;
			heldValueLerp = Mathf.Min(0.999f, heldValueLerp);
			TickStart();
        }

        #endregion
        // =============================================================================
        #region Position

        void SetHeldValue(float value)
        {
            _heldValue = value;

            // Update door if held
            if (HeldValue > -1 && !IsTickingAlready)
			{
				heldValueLerp = HeldValue;
				TickStart();
			}
        }

        void SetReleaseSpeed(float value)
        {
            _releaseSpeed = value;

            // Update if released
            if (IsTickingAlready)
                localReleaseSpeed = ReleaseSpeed;
        }

        void HeldValueUpdate()
        {
            float newValue = Mathf.Clamp01(HeldValueCalc());
            float newReleaseSpeed = newValue - HeldValue;
            if (newReleaseSpeed != 0)
                localReleaseSpeed = newReleaseSpeed;
            
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
            heldValueLerp = isOwner ? HeldValue :
                ExpDecay(heldValueLerp, HeldValue, EXP_DECAY_DEFAULT, tickDeltaTime);

            HeldValueSetPosition(heldValueLerp);
            colliderClosed.enabled = heldValueLerp <= 0;
            SetOcclusionPortalOpen(colliderClosed.enabled);

            if (heldValuePrev != heldValueLerp)
            {
                if (heldValuePrev > 0 && heldValueLerp == 0) {
                    SoundSlidingSetVolume(VOLUME_NEARLY_MUTE);
                    SoundPlay(sfxClosed);
                } else if (heldValueLerp > 0 && heldValuePrev == 0) {
                    SoundSlidingSetVolume(VOLUME_NEARLY_MUTE);
                    SoundPlay(sfxOpened);
                } else if (heldValueLerp == 1 && heldValuePrev < 1) {
                    SoundSlidingSetVolume(VOLUME_NEARLY_MUTE);
                    SoundPlay(sfxOpenedFully);
                } else
                {
                    SoundSlidingLerpVolumeByMovement();
                }
                heldValuePrev = heldValueLerp;
            }

            else if (slidingSource.volume > VOLUME_NEARLY_MUTE)
                SoundSlidingLerpVolume(VOLUME_NEARLY_MUTE);
        }

        void HeldValueTickReleased()
        {
            heldValueLerp += localReleaseSpeed * tickDeltaTime;

            SoundSlidingLerpVolumeByMovement();
            HeldValueSetPosition(heldValueLerp);
            if (doorHandle.pickup.pickupable)
                HandleUpdatePosition();

            if (IsLocked)
            {
                if (heldValueLerp > 0f) return;
                heldValueLerp = 0f;
                colliderClosed.enabled = true;

                SoundPlay(sfxLocked);
                SetOcclusionPortalOpen(false);
                TickStop();
            }

            else if (alwaysClose)
            {
                if (heldValueLerp >= 1f && localReleaseSpeed > 0f)
                {
                    heldValueLerp = 1f;
                    SoundPlay(sfxOpenedFully);
                    localReleaseSpeed = -localReleaseSpeed;
                }
                
                if (heldValueLerp <= 0f)
                {
                    heldValueLerp = 0f;
                    colliderClosed.enabled = true;
                    SoundPlay(sfxClosed);
                    SetOcclusionPortalOpen(false);
                    TickStop();
                }
            }

            else if (!(heldValueLerp > 0f && heldValueLerp < 1f))
            {
                heldValueLerp = Mathf.Clamp01(heldValueLerp);
                colliderClosed.enabled = heldValueLerp <= 0f;

                SoundPlay(heldValueLerp <= 0f ? sfxClosed : sfxOpenedFully);
                SetOcclusionPortalOpen(heldValueLerp <= 0f);
                TickStop();
            }

            heldValuePrev = heldValueLerp;
        }

        protected virtual float DoorDistanceCalc() => 0f;
        protected virtual float HeldValueCalc() => 0f;
        protected virtual void HeldValueSetPosition(float value) {}
        protected virtual void HandleUpdatePosition() {}
        protected virtual Vector3 HandleSetOffset() => Vector3.zero;

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
            SoundSlidingSetVolume(IsLocked ? forcedSlidingSpeed : VOLUME_NEARLY_MUTE);
            SoundSlidingPlay();
        }

        void _OnTickEnd()
        {
            SoundSlidingSetVolume(0f);
            SoundSlidingStop();
			HeldValueSetPosition(heldValueLerp);
        }

        void _OnTick()
        {
            bool isOwner = Networking.IsOwner(gameObject);
			if (isOwner && doorHandle.pickup.IsHeld)
				HeldValueUpdate();
            
            if (HeldValue > -1)
                HeldValueTickHeld(isOwner);
            else
                HeldValueTickReleased();
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