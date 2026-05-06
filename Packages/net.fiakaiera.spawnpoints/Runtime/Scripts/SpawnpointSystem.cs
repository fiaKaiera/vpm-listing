
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;

namespace FiaKaiera.Spawns
{
    [AddComponentMenu("fiaKaiera/Spawns/Spawnpoint System")]
    [HelpURL("https://github.com/fiaKaiera/vpm-listing/blob/main/Packages/net.fiakaiera.spawnpoints/README.md#details")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]

    // This is set to -1 so that other systems that messes with OnRespawn() can overtake this after
    [DefaultExecutionOrder(-1)]

#if UNITY_2021_2_OR_NEWER && UNITY_EDITOR
    [Icon(ICON_PATH)]
#endif
    public class SpawnpointSystem : UdonSharpBehaviour
    {
#if UNITY_2021_2_OR_NEWER && UNITY_EDITOR
        const string ICON_PATH = "Packages/net.fiakaiera.spawns/Runtime/Resources/MaterialSymbolsLightLocationOn.png";
        public const string USER_SPAWNPOINT_NOTICE = "Note: For clarity, user spawns uses the\nplayer's *display name* on the object's name.";
#endif

        const string EVENT_FIRST_SPAWN = "OnLocalFirstSpawn";
        const string EVENT_ASSIGNED_RESPAWN = "OnLocalAssignedRespawn";
        const string EVENT_OVERRIDE_RESPAWN = "OnLocalOverrideRespawn";
        const string EVENT_DEFAULT_RESPAWN = "OnLocalDefaultRespawn";
        const string EVENT_BACKUP_RESPAWN = "OnLocalBackupRespawn";
        const string EVENT_POST_RESPAWN = "OnPostRespawn";
        const string EVENT_SPAWN_ASSIGNED = "OnLocalSpawnAssigned";
        const string EVENT_SPAWN_LOADED = "OnLocalSpawnLoaded";
        const string EVENT_SPAWN_SAVED = "OnLocalSpawnSaved";
        const string EVENT_SPAWN_CLEARED = "OnLocalSpawnCleared";

        [Tooltip("How soon the player gets teleported to their assigned spawn in frames or seconds. (decimals discarded in frames)")]
        [SerializeField] float startDelay = 20f;
        [SerializeField] StartDelayType startDelayIn = StartDelayType.Frames;

        [Header("Respawning")]
        [Tooltip("Overrides where the player respawns and sends them there after start delay upon joining the world. \n\nGood for \"hide the player until they're fully waited\" scenarios.")]
        [SerializeField] SpawnOverrideType spawnOverride;
        [Tooltip("The positions where the player respawns.\n\nYou can actually relocate this object and they will spawn there instead.")]
        [SerializeField] Transform[] overrideSpawns;
        [Tooltip("Players spawn at a random position within the radius.\nSet this to 0 to be precise.")]
        public float overrideSpawnRadius = 0;
        [SerializeField] VRC_SceneDescriptor.SpawnOrientation overrideSpawnOrientation = VRC_SceneDescriptor.SpawnOrientation.Default;
        [Tooltip("The behavior when the player respawns twice in a row within a time period.\n\n[Assigned Spawn First] will send you the assigned spawn first, then default spawn second. [Default Spawn First] is vice-versa.")]
        [SerializeField] BackupRespawnType backupRespawn = BackupRespawnType.AssignedSpawnFirst;
        [Tooltip("The amount of seconds the player is sent to the backup respawn if they respawned within the time period.\n\nDisabled if backup respawn is set to either \"Assigned Spawn Only\" or \"Default Spawn Only\". Setting this to 0 disables also this.")]
        [SerializeField] float backupRespawnSeconds = 10f;

        [Header("Persistence")]
        [Tooltip("Allows saving and loading of savepoints via \"Save\" functions. This takes priority over any assigned spawnpoints. \nSee documentation for what these functions are.\n\nEnabling this makes sure that start delay waits for player data to be waited first if the value is lower than the loading time.")]
        [SerializeField] bool persistentSavepoints = false;
        [Tooltip("A set of transforms that are considered persistent spawns. Any Transforms that are not in the list when saving is not considered.")]
        [SerializeField] Transform[] persistentSpawns;
        [Tooltip("The key used when saving the current spawn with persistence.\nDefault: saved_spawnpoint")]
        public string persistentKey = SPAWNPOINT_DATA_KEY;

        [Header("Instance Owner Spawn")]
        [Tooltip("Sets the spawn of the instance owner. Can be moved around.")]
        [SerializeField] Transform ownerSpawn;
        [Tooltip("The behaviour of the instance owner's spawn.\n\n• [Assigned Spawn] sets it as the their assigned spawn.\n\n• [Assigned If First] only assigns their spawn if they're the first user in the instance. (Default/User Spawn on rejoin)\n\n• [Use User Spawn] sets their assigned spawn to their own user spawn upon respawn. (Default if nothing is assigned)\n\n• [Use Default Spawn] uses the default spawn upon respawn.")]
        [SerializeField] OwnerSpawnType ownerSpawnBehaviour = OwnerSpawnType.AssignedSpawn;

        [Header("User Spawns")]
        [Tooltip("Uses the children of this object as assigned user spawns for each user. The list below takes priority over children.")]
        [SerializeField] bool childrenAsUserSpawns = true;
        [Tooltip("A set of transforms that are considered user spawns.")]
        [SerializeField] Transform[] assignedUserSpawns;

        [Header("Listener Events")]
        [Tooltip("UdonBehaviours that will listen for events sent by the spawnpoint system.\nSee documentation for what events are sent.")]
        [SerializeField] UdonBehaviour[] listeners;
        
        const string SPAWNPOINT_DATA_KEY = "saved_spawnpoint";
        const int SPAWNPOINT_DATA_LIST_COUNT = 7;
        [HideInInspector, SerializeField] GameObject placeholderSpawn; // This is assigned to a prefab

        Transform assignedSpawn;
        Transform _placeholderSpawn;
        float respawnTime = float.MinValue;
        bool useEvents = false;
        bool waited = false;
        bool firstSpawn = true;

        public VRCPlayerApi lastRespawnedPlayer;

        void Start()
        {
            // FindObjectsOfType is not exposed.... so this doesn't work...
            // MultipleInstanceCheck();

            useEvents = listeners.Length > 0;
            InitAssignSpawn();

            if (!persistentSavepoints)
                waited = true;

            if (startDelayIn == StartDelayType.Frames)
                SendCustomEventDelayedFrames(nameof(StartSpawn), Mathf.FloorToInt(startDelay), VRC.Udon.Common.Enums.EventTiming.LateUpdate);
            else
                SendCustomEventDelayedSeconds(nameof(StartSpawn), Mathf.FloorToInt(startDelay), VRC.Udon.Common.Enums.EventTiming.LateUpdate);
        }

        public void StartSpawn()
        {
            if (!waited)
            {
                waited = true;
                return;
            }
            Respawn(Networking.LocalPlayer);
        }

        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            lastRespawnedPlayer = player;
            if (!player.isLocal || !waited)
            {
                SendEvent(EVENT_POST_RESPAWN);
                return;
            }
            Respawn(player);
        }

        public void Respawn() => OnPlayerRespawn(Networking.LocalPlayer);

        void Respawn(VRCPlayerApi player)
        {
            if (!enabled) return;

            if (firstSpawn)
            {
                lastRespawnedPlayer = player;
                firstSpawn = false;
                SendEvent(EVENT_FIRST_SPAWN);

                if (Utilities.IsValid(ownerSpawn))
                {
                    player.TeleportTo(ownerSpawn.position, ownerSpawn.rotation, VRC_SceneDescriptor.SpawnOrientation.Default, false);
                    SendEvent(EVENT_ASSIGNED_RESPAWN);
                    return;
                }
            }

            switch (backupRespawn)
            {
                case BackupRespawnType.AssignedSpawnFirst:
                    if (BackupSpawnCheck())
                        SpawnDefault(player);
                    else
                    {
                        SendEvent(EVENT_BACKUP_RESPAWN);
                        SpawnAssigned(player);
                    }
                    break;
                case BackupRespawnType.AssignedSpawnOnly:
                    SpawnAssigned(player);
                    break;
                case BackupRespawnType.DefaultSpawnFirst:
                    if (BackupSpawnCheck())
                        SpawnAssigned(player);
                    else
                    {
                        SendEvent(EVENT_BACKUP_RESPAWN);
                        SpawnDefault(player);
                    }
                    SendEvent(EVENT_BACKUP_RESPAWN);
                    break;
                case BackupRespawnType.DefaultSpawnOnly:
                    SpawnDefault(player);
                    break;
            }

            SendEvent(EVENT_POST_RESPAWN);
        }

        bool BackupSpawnCheck()
        {
            if (backupRespawn == BackupRespawnType.AssignedSpawnOnly || backupRespawn == BackupRespawnType.DefaultSpawnOnly)
                return false;
            
            float currentTime = Time.time;
            bool isBackupSpawning = currentTime < respawnTime + backupRespawnSeconds;
            respawnTime = isBackupSpawning ? float.MinValue : currentTime;
            return isBackupSpawning;
        }

        void SpawnAssigned(VRCPlayerApi player)
        {
            if (Utilities.IsValid(assignedSpawn))
            {
                player.TeleportTo(assignedSpawn.position, assignedSpawn.rotation, VRC_SceneDescriptor.SpawnOrientation.Default, false);
                SendCustomEvent(EVENT_ASSIGNED_RESPAWN);
            }
            
            else
            {
                // Respawn normally otherwise
                SpawnDefault(player);
            }
        }

        void SpawnDefault(VRCPlayerApi player)
        {
            if (spawnOverride == SpawnOverrideType.UseSceneDescriptor) return;
            Transform spawn = null;
            switch(spawnOverride)
            {
                case SpawnOverrideType.First:
                    if (!Utilities.IsValid(overrideSpawns[0])) return;
                    spawn = overrideSpawns[0];
                    break;
                case SpawnOverrideType.Sequential:
                    int ind = Networking.LocalPlayer.playerId % overrideSpawns.Length;
                    if (!Utilities.IsValid(overrideSpawns[ind])) return;
                    spawn = overrideSpawns[ind];
                    break;
                case SpawnOverrideType.Random:
                    int rand = Random.Range(0, overrideSpawns.Length);
                    if (!Utilities.IsValid(overrideSpawns[rand])) return;
                    spawn = overrideSpawns[rand];
                    break;
            }

            if (Utilities.IsValid(spawn))
            {
                Vector3 position = spawn.position;
                if (overrideSpawnRadius > 0)
                {
                    float randomRadius = Random.Range(0, overrideSpawnRadius);
                    float randomRadians = Random.Range(0, Mathf.PI * 2);
                    Vector2 direction = new Vector2(Mathf.Cos(randomRadians), Mathf.Sin(randomRadians)) * randomRadius;
                    position.x += direction.x;
                    position.z += direction.y;
                }
                
                player.TeleportTo(position, spawn.rotation, overrideSpawnOrientation, false);
                SendCustomEvent(EVENT_OVERRIDE_RESPAWN);
            }

            else
                SendCustomEvent(EVENT_DEFAULT_RESPAWN);
        }

        void InitAssignSpawn()
        {
            string displayName = Networking.LocalPlayer.displayName.Trim();
            if (assignedUserSpawns.Length > 0)
            {
                foreach (Transform spawn in assignedUserSpawns)
                {
                    if (spawn.name.Trim() != displayName) continue;
                    assignedSpawn = spawn;
                    break;
                }
            }

            if (childrenAsUserSpawns && !assignedSpawn)
            {
                foreach (Transform spawn in transform)
                {
                    if (spawn.name.Trim() != displayName) continue;
                    assignedSpawn = spawn;
                    break;
                }
            }

            if (Utilities.IsValid(ownerSpawn))
            {
                switch (ownerSpawnBehaviour)
                {
                    case OwnerSpawnType.AssignedSpawn:
                        assignedSpawn = ownerSpawn;
                        break;
                    case OwnerSpawnType.AssignedIfFirst:
                        if (Networking.LocalPlayer.playerId == 0)
                            assignedSpawn = ownerSpawn;
                        break;
                    case OwnerSpawnType.UseUserSpawn:
                        // do nothing, pretty much it's assigned beforehand
                        break;
                    case OwnerSpawnType.UseDefaultSpawn:
                        assignedSpawn = null;
                        break;
                }
            }
        }

        /*
        const string DUPLICATE_ERROR = "There are {x} spawnpoint systems in the scene. Please ensure there is always exactly one spawnpoint system in the scene. All others will be forcefully disabled except one. ({y})";

        void MultipleInstanceCheck()
        {
            SpawnpointSystem[] instances = FindObjectsOfType<SpawnpointSystem>(true);
            if (instances.Length > 1)
            {
                //LogWarning($"Another SpawnpointSystem ({system.name}) is active in the world. Disabling this instance to avoid conflicts.");
                string warning = DUPLICATE_ERROR.Replace("{x}", instances.Length.ToString());
                string objects = name;
                foreach (SpawnpointSystem duplicate in instances)
                {
                    if (duplicate == this) continue;
                    objects += $", {duplicate.name}";
                    if (duplicate.enabled)
                        enabled = false;
                }
                warning = warning.Replace("{y}", objects);
                LogWarning(warning);
            }
        }
        */

        // =============================================================================
        #region Persistence

        public override void OnPlayerRestored(VRCPlayerApi player)
        {
            if (!player.isLocal) return;
            if (!waited)
            {
                waited = true;
                return;
            }

            SpawnLoad(player);
            Respawn(player);
        }

        void SpawnLoad(VRCPlayerApi player)
        {
            if (!PlayerData.HasKey(player, persistentKey)) return;
            if (PlayerData.TryGetInt(player, persistentKey, out int index))
            {
                assignedSpawn = persistentSpawns[index];
                return;
            }
            if (PlayerData.TryGetString(player, persistentKey, out string str))
            {
                if (VRCJson.TryDeserializeFromJson(str, out DataToken result))
                {
                    if (result.TokenType != TokenType.DataList) return;
                    DataList list = result.DataList;
                    if (list.Count != SPAWNPOINT_DATA_LIST_COUNT) return;

                    Vector3 position = new Vector3(list[0].Float, list[1].Float, list[2].Float);
                    Quaternion rotation = new Quaternion(list[3].Float, list[4].Float, list[5].Float, list[6].Float);
                    AssignPlaceholderSpawn(position, rotation);
                }

                foreach (Transform spawn in persistentSpawns)
                {
                    if (spawn.name == str)
                    {
                        AssignSpawn(spawn);
                        SendEvent(EVENT_SPAWN_LOADED);
                        return;
                    }
                }
            }
        }

        bool SpawnSaveCheck()
        {
            if (!persistentSavepoints)
            {
                LogWarning("Persistent savepoints are not enabled while someone is trying to save. Ignoring.");
                return false;
            }

            if (!waited)
            {
                LogWarning("Trying to save location while the player is not waited. Ignoring.");
                return false;
            }
            
            return true;
        }

        public void SpawnSave(int id, bool assignSpawn = true)
        {
            if (!SpawnSaveCheck()) return;
            if (!Utilities.IsValid(persistentSpawns[id])) return;
            PlayerData.SetInt(persistentKey, id);
            if (assignSpawn)
                AssignSpawn(persistentSpawns[id]);
            SendEvent(EVENT_SPAWN_SAVED);
        }

        public void SpawnSave(Transform savepoint, bool assignSpawn = true)
        {
            if (!SpawnSaveCheck()) return;
            for (int index = 0; index < persistentSpawns.Length; index++)
            {
                if (persistentSpawns[index] == savepoint)
                {
                    PlayerData.SetInt(persistentKey, index);
                    if (assignSpawn)
                        AssignSpawn(savepoint);
                    SendEvent(EVENT_SPAWN_SAVED);
                    return;
                }
            }
        }

        public void SpawnSaveAsName(Transform savepoint, bool assignSpawn = true)
        {
            if (!SpawnSaveCheck()) return;
            PlayerData.SetString(persistentKey, savepoint.name);
            if (assignSpawn)
                AssignSpawn(savepoint);
            SendEvent(EVENT_SPAWN_SAVED);
        }

        public void SpawnSave(Vector3 position, Quaternion rotation, bool assignSpawn = true)
        {
            if (!SpawnSaveCheck()) return;
            string str_position = position.ToString().Replace('(', ' ').Replace(')', ' ');
            string str_rotation = rotation.ToString().Replace('(', ' ').Replace(')', ' ');
            PlayerData.SetString(persistentKey, $"[{str_position},{str_rotation}]");
            if (assignSpawn)
                AssignPlaceholderSpawn(position, rotation);
            SendEvent(EVENT_SPAWN_SAVED);
        }

        public void SpawnClear(bool assignSpawn = true)
        {
            if (PlayerData.HasKey(Networking.LocalPlayer, persistentKey))
                PlayerData.SetBool(persistentKey, false);
            if (assignSpawn)
            {
                assignedSpawn = null;
                SendEvent(EVENT_SPAWN_ASSIGNED);
            }
            SendEvent(EVENT_SPAWN_CLEARED);
        }

        void AssignPlaceholderSpawn(Vector3 position, Quaternion rotation)
        {
            if (!Utilities.IsValid(_placeholderSpawn))
                _placeholderSpawn = Instantiate(placeholderSpawn, transform).transform;
            _placeholderSpawn.SetPositionAndRotation(position, rotation);
            AssignSpawn(_placeholderSpawn);
        }

        #endregion
        // =============================================================================
        #region Assignment

        public void AssignSpawn(Transform newSpawn)
        {
            if (Utilities.IsValid(newSpawn))
            {
                assignedSpawn = newSpawn;
                SendEvent(EVENT_SPAWN_ASSIGNED);
            }
        }

        public Transform GetAssignedSpawn()
        {
            if (Utilities.IsValid(assignedSpawn))
                return assignedSpawn;
            return null;
        }

        #endregion
        // =============================================================================
        #region Event Listening

        void SendEvent(string _event)
        {
            if (useEvents) return;
            foreach(UdonBehaviour behaviour in listeners)
            {
                if (!Utilities.IsValid(behaviour)) return;
                SendCustomEvent(_event);
            }
        }

        #endregion
        void LogWarning(string message) => Debug.LogWarning($"[<color=#DDAA11>SpawnpointSystem</color> {name}] {message}", this);
    }

    public enum SpawnOverrideType: int
    {
        UseSceneDescriptor, First, Sequential, Random
    }

    public enum OwnerSpawnType: int
    {
        AssignedSpawn, AssignedIfFirst, UseUserSpawn, UseDefaultSpawn
    }

    public enum StartDelayType: int
    {
        Frames, Seconds
    }

    public enum BackupRespawnType: int
    {
        AssignedSpawnFirst, AssignedSpawnOnly, DefaultSpawnFirst, DefaultSpawnOnly
    }
}