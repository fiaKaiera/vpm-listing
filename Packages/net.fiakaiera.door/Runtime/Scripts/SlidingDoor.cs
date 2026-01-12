
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace FiaKaiera.Door
{
	[AddComponentMenu("fiaKaiera/Door/Sliding Door")]
	public class SlidingDoor : DoorBehaviour
	{
		const float SLIDING_VOLUME_MULTIPLIER = 100f;

        protected override float DoorDistanceCalc() => Mathf.Abs(valueOpened - valueClosed);

		protected override Vector3 HandleSetOffset()
		{
			if (!HandleIsValid) return Vector3.zero;
			return doorTransform.localPosition - doorHandle.transform.localPosition;
		}

		protected override float HeldValueCalc()
		{
			if (!HandleIsValid) return -1;
			return Mathf.InverseLerp(
				valueClosed, valueOpened, doorHandle.transform.localPosition.x + doorHandleOffset.x);
		}
		
		protected override void HeldValueSetPosition(float value)
		{
			Vector3 doorPosition = doorTransform.localPosition;
			doorPosition.x = Mathf.Lerp(valueClosed, valueOpened, value);
			doorTransform.localPosition = doorPosition;
		}

		protected override void HandleUpdatePosition()
		{
			if (!HandleIsValid) return;
			doorHandle.transform.SetLocalPositionAndRotation(
				doorTransform.localPosition - doorHandleOffset, Quaternion.identity
			);
		}

		protected override float SoundSlidingHeldValueCalc()
		{
			float distancePrev = Mathf.Lerp(valueClosed, valueOpened, heldValuePrev);
			float distanceLerp = Mathf.Lerp(valueClosed, valueOpened, heldValueCurrent);
			return Mathf.Abs(distancePrev - distanceLerp) * SLIDING_VOLUME_MULTIPLIER;
		}
	}    
}