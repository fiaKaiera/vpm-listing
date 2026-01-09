
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;

namespace FiaKaiera.Door
{
    [AddComponentMenu("fiaKaiera/Door/Door Knocker")]
    [HelpURL("https://github.com/fiaKaiera/vpm-listing/blob/main/Packages/net.fiakaiera.door/README.md#door-knocker")]
    [RequireComponent(typeof(Collider))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
#if UNITY_2021_2_OR_NEWER && UNITY_EDITOR
    [Icon(ICON_PATH)]
#endif
    public class DoorKnocker : UdonSharpBehaviour
    {
#if UNITY_2021_2_OR_NEWER && UNITY_EDITOR
        const string ICON_PATH = "Packages/net.fiakaiera.door/Runtime/Resources/MaterialSymbolsDoorOpen.png";
#endif
        [Header("Add this as a listener to a DoorBehaviour\n(SlidingDoor/HingeDoor) for it to properly respond\nto doors opening/closing.\n\nCan safely be placed inside of a DoorBehaviour object.")]
        [Space]
        [SerializeField] AudioSource audioSource;
        [SerializeField, Range(-3,3)] float pitchMin = 0.9f;
        [SerializeField, Range(-3,3)] float pitchMax = 1.1f;
        [Space]
        [Tooltip("UdonBehaviours that will listen for events sent by this knocker.\nSee documentation for what events are sent.")]
        [SerializeField] UdonBehaviour[] listeners;

        const string EVENT_KNOCK = "OnDoorKnock";
        float audioTimeout = float.MinValue;
        Collider col;

        void Start()
        {
            col = GetComponent<Collider>();
            if (!Utilities.IsValid(audioSource))
            {
                audioSource = GetComponent<AudioSource>();
                if (!Utilities.IsValid(audioSource))
                {
                    DisableWithWarn("Door knocker has no attached AudioSource, therefore making the knocker not have a purpose. Disabling.");
                    return;
                }
            }

            if (!Utilities.IsValid(audioSource.clip))
            {
                DisableWithWarn("Door knocker's AudioSource has no attached clip, therefore will play no sound. Disabling.");
                return;
            }

            if (pitchMax < pitchMin)
            {
                float swap = pitchMin;
                pitchMin = pitchMax;
                pitchMax = swap;
            }
            col.isTrigger = true;
            
            audioSource.enabled = false;
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

        void OnDisable()
        {
            if (Utilities.IsValid(audioSource))
            {
                audioSource.enabled = false;
                audioTimeout = float.MinValue;
            }
        }
        
        public void OnDoorEnableOpened() => OnDoorOpened();
        public void OnDoorDisabled() => OnDoorClosed();
        public void OnDoorOpened() => col.enabled = false;
        public void OnDoorClosed() => col.enabled = true;

        public override void Interact()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(_SoundEvent), Random.Range(pitchMin, pitchMax));
        }

        [NetworkCallable]
        public void _SoundEvent(float pitch)
        {
            audioSource.enabled = true;
            audioSource.pitch = pitch;
            float pitchLength = audioSource.pitch * audioSource.clip.length;
            audioTimeout = Time.time + pitchLength - 0.001f;
            audioSource.Play();

            SendCustomEventDelayedSeconds(nameof(SoundTimeout), pitchLength, VRC.Udon.Common.Enums.EventTiming.Update);
            SendEvent(EVENT_KNOCK);
        }

        public void SoundTimeout()
        {
            if (!Utilities.IsValid(audioSource)) return;
            if (Time.time < audioTimeout) return;
            audioSource.enabled = false;
            audioTimeout = float.MinValue;
        }

        void SendEvent(string _event)
        {
            if (listeners.Length == 0) return;
            foreach(UdonBehaviour behaviour in listeners)
            {
                if (!Utilities.IsValid(behaviour)) return;
                SendCustomEvent(_event);
            }
        }

        void DisableWithWarn(string message)
        {
            gameObject.SetActive(false);
            col.enabled = false;
            enabled = false;
            Debug.LogWarning($"[<color=#CCAA00>DoorKnocker</color> ({name})] {message}", this);
        }
    }
}