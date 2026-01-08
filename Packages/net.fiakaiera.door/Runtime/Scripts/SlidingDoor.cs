
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

        protected override float DoorDistanceCalc() =>
			Mathf.Abs(pointOpened.localPosition.x - pointClosed.localPosition.x);

		protected override Vector3 HandleSetOffset() =>
			doorTransform.localPosition - doorHandle.transform.localPosition;

		protected override float HeldValueCalc() => Mathf.InverseLerp(
			pointClosed.localPosition.x, pointOpened.localPosition.x,
			doorHandle.transform.localPosition.x + doorHandleOffset.x);
		
		protected override void HeldValueSetPosition(float value)
		{
			Vector3 doorPosition = doorTransform.localPosition;
			doorPosition.x = Mathf.Lerp(pointClosed.localPosition.x, pointOpened.localPosition.x, value);
			doorTransform.localPosition = doorPosition;
		}

		protected override float SoundSlidingHeldValueCalc()
		{
			float distancePrev = Mathf.Lerp(pointClosed.localPosition.x, pointOpened.localPosition.x, heldValuePrev);
			float distanceLerp = Mathf.Lerp(pointClosed.localPosition.x, pointOpened.localPosition.x, heldValueLerp);
			return Mathf.Abs(distancePrev - distanceLerp) * SLIDING_VOLUME_MULTIPLIER;
		}

		protected override void HandleUpdatePosition()
		{
			doorHandle.transform.SetLocalPositionAndRotation(
				doorTransform.localPosition - doorHandleOffset, Quaternion.identity
			);
		}
	}    
}

