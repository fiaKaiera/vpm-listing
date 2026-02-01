
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace FiaKaiera.Door
{
    [AddComponentMenu("fiaKaiera/Door/Door Handle")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [HelpURL("https://github.com/fiaKaiera/vpm-listing/blob/main/Packages/net.fiakaiera.door/README.md#door-handle")]
    [RequireComponent(typeof(VRCPickup), typeof(Rigidbody))]
#if UNITY_2021_2_OR_NEWER && UNITY_EDITOR
    [Icon(ICON_PATH)]
#endif
    public class DoorHandle : UdonSharpBehaviour
    {
#if UNITY_2021_2_OR_NEWER && UNITY_EDITOR
        const string ICON_PATH = "Packages/net.fiakaiera.door/Runtime/Resources/MaterialSymbolsDoorOpen.png";
#endif
        [System.NonSerialized] DoorBehaviour doorBehaviour;
        [System.NonSerialized] public VRC_Pickup pickup;
        [System.NonSerialized] public Rigidbody body;
        bool isValid = true;

        public override void OnPickup()
        {
            if (!IsDoorValid()) return;
            doorBehaviour._OnDoorPickup();
        }

        public override void OnDrop()
        {
            if (!IsDoorValid()) return;
            doorBehaviour._OnDoorDrop();
        }

        bool IsDoorValid()
        {
            if (!Utilities.IsValid(doorBehaviour))
            {
                LogWarning("Handle is not tied to any door. Disabling.");
                gameObject.SetActive(false);
                isValid = false;
            }
            if (!Utilities.IsValid(body))
            {
                body = GetComponent<Rigidbody>();
                if (!Utilities.IsValid(body))
                {
                    LogWarning("Somehow, handle doesn't have a Rigidbody assigned. Disabling.");
                    gameObject.SetActive(false);
                    isValid = false;
                }
            }
            _CheckPickup();
            return isValid;
        }

        public void _SetDoorBehaviour(DoorBehaviour value)
        {
            doorBehaviour = value;
            IsDoorValid();
        }

        public bool _CheckPickup()
        {
            if (!Utilities.IsValid(pickup))
            {
                pickup = GetComponent<VRC_Pickup>();
                if (!Utilities.IsValid(pickup))
                {
                    LogWarning("Somehow, handle doesn't have a VRC_Pickup assigned. Disabling.");
                    gameObject.SetActive(false);
                    isValid = false;
                }
            }
            return isValid;
        }

        public override void OnPickupUseDown() => doorBehaviour._OnHandleUse(false);
        public override void OnPickupUseUp() => doorBehaviour._OnHandleUse(true);

        void LogWarning(string message) => Debug.LogWarning($"[<color=#DDAA11>DoorHandle</color> {name}] {message}", this);

    }
}