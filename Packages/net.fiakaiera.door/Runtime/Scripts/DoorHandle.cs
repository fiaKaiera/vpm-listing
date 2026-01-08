
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FiaKaiera.Door
{
    [AddComponentMenu("fiaKaiera/Door/Door Handle")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    [HelpURL("https://github.com/fiaKaiera/vpm-listing/blob/main/Packages/net.fiakaiera.door/README.md")]
    [RequireComponent(typeof(VRC_Pickup), typeof(Rigidbody))]

    public class DoorHandle : UdonSharpBehaviour
    {
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

        void LogWarning(string message) => Debug.LogWarning($"[<color=#FFDD88>DoorHandle</color> {name}] {message}", this);

    }
}