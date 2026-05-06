using UnityEngine;
using UnityEditor;
using VRC.SDK3.Components;

namespace FiaKaiera.Spawns.Editor
{
    [CustomEditor(typeof(SpawnpointSystem))]
    [DisallowMultipleComponent]
    public class SpawnSystemEditor : UnityEditor.Editor
    {

        const string OWNER_SPAWN_LABEL = "Owner Spawn";
        readonly Vector3 DIAGONAL_LEFT = new Vector3(0.33f,0f,0.33f);
        readonly Vector3 DIAGONAL_RIGHT = new Vector3(-0.33f,0f,0.33f);

        SerializedProperty
            startDelayIn, startDelay,
            spawnOverride, overrideSpawns, overrideSpawnRadius, overrideSpawnOrientation,
            backupRespawn, backupRespawnSeconds,
            ownerSpawn, ownerSpawnBehaviour,
            childrenAsUserSpawns, assignedUserSpawns,
            persistentSavepoints, persistentSpawns, persistentKey,
            listeners;

        GUILayoutOption expandWidth = GUILayout.ExpandWidth(true);
        GUIStyle labelStyle = new GUIStyle();

        void OnEnable()
        {
            labelStyle.normal.textColor = Color.white;
            startDelayIn = serializedObject.FindProperty("startDelayIn");
            startDelay = serializedObject.FindProperty("startDelay");
            spawnOverride = serializedObject.FindProperty("spawnOverride");
            overrideSpawns = serializedObject.FindProperty("overrideSpawns");
            overrideSpawnRadius = serializedObject.FindProperty("overrideSpawnRadius");
            overrideSpawnOrientation = serializedObject.FindProperty("overrideSpawnOrientation");
            backupRespawn = serializedObject.FindProperty("backupRespawn");
            backupRespawnSeconds = serializedObject.FindProperty("backupRespawnSeconds");
            persistentSavepoints = serializedObject.FindProperty("persistentSavepoints");
            persistentSpawns = serializedObject.FindProperty("persistentSpawns");
            persistentKey = serializedObject.FindProperty("persistentKey");
            ownerSpawn = serializedObject.FindProperty("ownerSpawn");
            ownerSpawnBehaviour = serializedObject.FindProperty("ownerSpawnBehaviour");
            childrenAsUserSpawns = serializedObject.FindProperty("childrenAsUserSpawns");
            assignedUserSpawns = serializedObject.FindProperty("assignedUserSpawns");
            listeners = serializedObject.FindProperty("listeners");
        }

        public override void OnInspectorGUI()
        {
            GUILayoutOption labelWidth = GUILayout.Width(EditorGUIUtility.labelWidth);
            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(PropertyLabel(startDelay), labelWidth);
            startDelay.floatValue = EditorGUILayout.FloatField(startDelay.floatValue, GUILayout.ExpandWidth(true));
            startDelayIn.enumValueIndex = (int)(StartDelayType)EditorGUILayout.EnumPopup((StartDelayType)startDelayIn.enumValueIndex, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(spawnOverride);
            if (spawnOverride.enumValueIndex != (int)SpawnOverrideType.UseSceneDescriptor)
            {
                EditorGUILayout.PropertyField(overrideSpawns);
                EditorGUILayout.PropertyField(overrideSpawnRadius);
                EditorGUILayout.PropertyField(overrideSpawnOrientation);
                EditorGUILayout.Space();
            }
                
            EditorGUILayout.PropertyField(backupRespawn);
            if (backupRespawn.enumValueIndex == (int)BackupRespawnType.AssignedSpawnFirst ||
                backupRespawn.enumValueIndex == (int)BackupRespawnType.DefaultSpawnFirst)
                EditorGUILayout.PropertyField(backupRespawnSeconds);

            EditorGUILayout.PropertyField(persistentSavepoints);
            if (persistentSavepoints.boolValue)
            {
                EditorGUILayout.PropertyField(persistentSpawns);
                EditorGUILayout.PropertyField(persistentKey);
            }

            EditorGUILayout.PropertyField(ownerSpawn);
            if (ownerSpawn.objectReferenceValue)
                EditorGUILayout.PropertyField(ownerSpawnBehaviour);

            EditorGUILayout.PropertyField(childrenAsUserSpawns);
            GUILayout.Label(SpawnpointSystem.USER_SPAWNPOINT_NOTICE, GUI.skin.box, expandWidth);
            EditorGUILayout.PropertyField(assignedUserSpawns);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.PropertyField(listeners);
            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void OnSceneGUI()
        {
            if (spawnOverride.enumValueIndex != (int)SpawnOverrideType.UseSceneDescriptor)
            {
                float spawnRadius = overrideSpawnRadius.floatValue;
                if (spawnRadius > 0)
                {
                    for (int index = 0; index < overrideSpawns.arraySize; index++)
                    {
                        SerializedProperty spawn = overrideSpawns.GetArrayElementAtIndex(index);
                        if (spawn.objectReferenceValue == null) continue;
                        Transform t = (Transform)spawn.objectReferenceValue;
                        DrawRadius(t, spawnRadius);
                    }
                }

                else
                {
                    for (int index = 0; index < overrideSpawns.arraySize; index++)
                    {
                        SerializedProperty spawn = overrideSpawns.GetArrayElementAtIndex(index);
                        if (spawn.objectReferenceValue == null) continue;
                        Transform t = (Transform)spawn.objectReferenceValue;
                        DrawPosition(t);
                    }
                }
            }

            if (persistentSavepoints.boolValue)
            {
                for (int index = 0; index < persistentSpawns.arraySize; index++)
                {
                    SerializedProperty spawn = persistentSpawns.GetArrayElementAtIndex(index);
                    if (spawn.objectReferenceValue == null) continue;
                    Transform t = (Transform)spawn.objectReferenceValue;
                    DrawPosition(t, $"{index}: {t.name}");
                }
            }

            if (ownerSpawn.objectReferenceValue != null)
                DrawPosition((Transform)ownerSpawn.objectReferenceValue, OWNER_SPAWN_LABEL);
            
            if (childrenAsUserSpawns.boolValue)
            {
                SpawnpointSystem system = (SpawnpointSystem)target;
                for (int index = 0; index < system.transform.childCount; index++)
                {
                    Transform t = system.transform.GetChild(index);
                    if (t == null) continue;
                    DrawPosition(t, t.name);
                }
            }

            if (assignedUserSpawns.arraySize > 0)
            {
                for (int index = 0; index < assignedUserSpawns.arraySize; index++)
                {
                    SerializedProperty spawn = assignedUserSpawns.GetArrayElementAtIndex(index);
                    if (spawn.objectReferenceValue == null) continue;
                    Transform t = (Transform)spawn.objectReferenceValue;
                    DrawPosition(t, t.name);
                }
            }
        }

        GUIContent PropertyLabel(SerializedProperty property) => new GUIContent(property.displayName, property.tooltip);
        void Header(string label) {
            EditorGUILayout.Space();
            GUILayout.Label(label, EditorStyles.boldLabel);
        }

        void DrawPosition(Transform t, string label = "")
        {
            Vector3 position = t.position;
            float handleScale = HandleUtility.GetHandleSize(position);

            Vector3 scaleDiagonalLeft = DIAGONAL_LEFT * handleScale;
            Vector3 scaleDiagonalright = DIAGONAL_RIGHT * handleScale;

            Handles.color = Color.green;
            Handles.DrawLine(position, position + Vector3.up);
            Handles.color = Color.blue;
            Handles.DrawLine(position, position + t.forward);

            Handles.color = Color.white;
            Handles.DrawLine(position + scaleDiagonalLeft, position - scaleDiagonalLeft);
            Handles.DrawLine(position + scaleDiagonalright, position - scaleDiagonalright);
            if (label != "")
                Handles.Label(position + Vector3.up, label, labelStyle);
        }

        void DrawRadius(Transform t, float radius, string label = "")
        {
            Vector3 position = t.position;

            Handles.color = Color.green;
            Handles.DrawLine(position, position + Vector3.up);
            Handles.color = Color.blue;
            Handles.DrawLine(position, position + t.forward);

            Handles.color = Color.white;
            Handles.DrawWireDisc(position, Vector3.up, radius);
            if (label != "")
                Handles.Label(position + Vector3.up, label, labelStyle);
        }
    }
}