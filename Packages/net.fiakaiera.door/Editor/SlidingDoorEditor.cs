using UnityEditor;
using UnityEngine;

namespace FiaKaiera.Door
{
    [CustomEditor(typeof(SlidingDoor)), CanEditMultipleObjects]
    public class SlidingDoorEditor : Editor
    {
        const float SLIDER_SNAP = 0.1f;
        protected virtual void OnSceneGUI()
        {
            SlidingDoor door = (SlidingDoor)target;
            Transform doorTransform = door.GetDoorTransform();
            Vector3 doorPosition = door.transform.position;
            Vector3 doorRight = door.transform.right;

            float scale = door.transform.lossyScale.x;
            float size = HandleUtility.GetHandleSize(doorPosition) * 0.4f;
            float halfSize = size * 0.5f;

            EditorGUI.BeginChangeCheck();
            Handles.color = Color.cyan;

            int closedId = GUIUtility.GetControlID(FocusType.Passive) + 1;
            float closedValue = door.GetValueClosed();
            float closedPos = SliderDistance(
                doorPosition, -doorRight, halfSize, -closedValue, scale, size);
            closedPos = -closedPos;

            int openedId = GUIUtility.GetControlID(FocusType.Passive) + 1;
            float openedValue = door.GetValueClosed();
            float openedPos = SliderDistance(
                doorPosition, doorRight, halfSize, door.GetValueOpened(), scale, size);

            if (GUIUtility.hotControl == 0)
            {
                Vector3 local = doorTransform.localPosition;
                local.x = closedValue;
                doorTransform.localPosition = local;
            }
            else if (GUIUtility.hotControl == closedId)
            {
                Vector3 local = doorTransform.localPosition;
                local.x = closedPos;
                doorTransform.localPosition = local;
            }
            else if (GUIUtility.hotControl == openedId)
            {
                Vector3 local = doorTransform.localPosition;
                local.x = openedPos;
                doorTransform.localPosition = local;
            }

            if (!EditorGUI.EndChangeCheck()) return;
            if (closedValue != closedPos) door.SetValueClosed(closedPos);
            if (openedValue != openedPos) door.SetValueClosed(openedPos);
        }
        
        float SliderDistance(Vector3 position, Vector3 direction, float offset, float value, float scale, float handleSize)
        {
            Vector3 basePosition = position + direction * (value * scale + offset);
            Vector3 sliderPosition = Handles.Slider(
                basePosition, direction, handleSize, Handles.ConeHandleCap, SLIDER_SNAP
            );

            if (basePosition != sliderPosition)
                return Vector3.Dot(sliderPosition - position, direction) - offset / scale;
            else
                return value;
        }
    }
}