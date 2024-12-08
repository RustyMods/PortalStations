using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ItemManager;
using JetBrains.Annotations;
using Managers;
using PieceManager;
using PortalStations.Managers;
using PortalStations.Stations;
using ServerSync;
using UnityEngine;
using CraftingTable = PieceManager.CraftingTable;

namespace PortalStations
{
    [BepInDependency("org.bepinex.plugins.groups", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("org.bepinex.plugins.guilds", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class PortalStationsPlugin : BaseUnityPlugin
    {
        internal const string ModName = "PortalStations";
        internal const string ModVersion = "1.2.5";
        internal const string Author = "RustyMods";
        private const string ModGUID = Author + "." + ModName;
        private static readonly string ConfigFileName = ModGUID + ".cfg";
        private static readonly string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource PortalStationsLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
        private static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        public static PortalStationsPlugin _plugin = null!;
        public static AssetBundle _asset = null!;
        public static GameObject _root = null!;
        public enum Toggle { On = 1, Off = 0 }
        public void Awake()
        {
            _plugin = this;
            _asset = GetAssetBundle("portal_station_assets");
            _root = new GameObject("root");
            DontDestroyOnLoad(_root);
            _root.SetActive(false);

            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On,
                "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            InitPieces();
            InitItems();
            InitConfigs();
            
            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private static void InitPieces()
        {
            BuildPiece PortalStation = new(_asset, "portalstation");
            PortalStation.Name.English("Ancient Portal");
            PortalStation.Description.English("Teleportation portal");
            PortalStation.RequiredItems.Add("Stone", 20, true);
            PortalStation.RequiredItems.Add("SurtlingCore", 2, true);
            PortalStation.RequiredItems.Add("FineWood", 20, true);
            PortalStation.RequiredItems.Add("GreydwarfEye", 10, true);
            PortalStation.Category.Set("Portal Stations");
            PortalStation.Crafting.Set(CraftingTable.Workbench);
            PortalStation.PlaceEffects.Add("vfx_Place_stone_wall_2x1");
            PortalStation.PlaceEffects.Add("sfx_build_hammer_stone");
            PortalStation.HitEffects.Add("vfx_RockHit");
            PortalStation.HitEffects.Add("sfx_rock_hit");
            PortalStation.DestroyedEffects.Add("vfx_RockHit");
            PortalStation.DestroyedEffects.Add("sfx_rock_destroyed");
            PortalStation.ClonePortalSFXFrom = "portal_wood";
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(PortalStation.Prefab.transform, "vanilla_effects").gameObject);
            PortalStation.Prefab.AddComponent<PortalStation>();
            Stations.Stations.PrefabsToSearch.Add(PortalStation.Prefab.name);
            
            BuildPiece PortalStationFountain = new(_asset, "PortalStation_Fountain");
            PortalStationFountain.Name.English("Fountain Portal");
            PortalStationFountain.Description.English("Teleportation portal");
            PortalStationFountain.RequiredItems.Add("Stone", 20, true);
            PortalStationFountain.RequiredItems.Add("SurtlingCore", 2, true);
            PortalStationFountain.RequiredItems.Add("FineWood", 20, true);
            PortalStationFountain.RequiredItems.Add("GreydwarfEye", 10, true);
            PortalStationFountain.Category.Set("Portal Stations");
            PortalStationFountain.Crafting.Set(CraftingTable.Workbench);
            PortalStationFountain.PlaceEffects.Add("vfx_Place_stone_wall_2x1");
            PortalStationFountain.PlaceEffects.Add("sfx_build_hammer_stone");
            PortalStationFountain.HitEffects.Add("vfx_RockHit");
            PortalStationFountain.HitEffects.Add("sfx_rock_hit");
            PortalStationFountain.DestroyedEffects.Add("vfx_RockHit");
            PortalStationFountain.DestroyedEffects.Add("sfx_rock_destroyed");
            PortalStationFountain.ClonePortalSFXFrom = "portal_wood";
            var fountainModel = Utils.FindChild(PortalStationFountain.Prefab.transform, "model");
            MaterialReplacer.RegisterGameObjectForShaderSwap(fountainModel.gameObject, MaterialReplacer.ShaderType.PieceShader);
            var fountainPortal = PortalStationFountain.Prefab.AddComponent<PortalStation>();
            fountainPortal.m_model = Utils.FindChild(fountainModel, "platform").GetComponent<MeshRenderer>();
            var emissiveMat = Utils.FindChild(fountainModel, "$part_slab").GetComponent<MeshRenderer>().material;
            fountainPortal.m_emissiveMaterials.Add(emissiveMat);
            fountainPortal.m_baseColor = emissiveMat.GetColor("_EmissionColor");
            Stations.Stations.PrefabsToSearch.Add(PortalStationFountain.Prefab.name);

            BuildPiece PortalStationOne = new(_asset, "portalStationOne");
            PortalStationOne.Name.English("Chained Portal");
            PortalStationOne.Description.English("Teleportation portal");
            PortalStationOne.RequiredItems.Add("Stone", 20, true);
            PortalStationOne.RequiredItems.Add("SurtlingCore", 2, true);
            PortalStationOne.RequiredItems.Add("FineWood", 20, true);
            PortalStationOne.RequiredItems.Add("GreydwarfEye", 10, true);
            PortalStationOne.Category.Set("Portal Stations");
            PortalStationOne.PlaceEffects.Add("vfx_Place_stone_wall_2x1");
            PortalStationOne.PlaceEffects.Add("sfx_build_hammer_stone");
            PortalStationOne.HitEffects.Add("vfx_RockHit");
            PortalStationOne.HitEffects.Add("sfx_rock_hit");
            PortalStationOne.DestroyedEffects.Add("vfx_RockHit");
            PortalStationOne.DestroyedEffects.Add("sfx_rock_destroyed");
            PortalStationOne.ClonePortalSFXFrom = "portal_wood";
            PortalStationOne.Crafting.Set(CraftingTable.Workbench);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(PortalStationOne.Prefab.transform, "vanilla_effects").gameObject);
            PortalStationOne.Prefab.AddComponent<PortalStation>();
            Stations.Stations.PrefabsToSearch.Add(PortalStationOne.Prefab.name);
            
            BuildPiece portalPlatform = new(_asset, "portalPlatform");
            portalPlatform.Name.English("Platform Portal");
            portalPlatform.Description.English("Teleportation portal");
            portalPlatform.RequiredItems.Add("Stone", 20, true);
            portalPlatform.RequiredItems.Add("SurtlingCore", 2, true);
            portalPlatform.RequiredItems.Add("FineWood", 20, true);
            portalPlatform.RequiredItems.Add("GreydwarfEye", 10, true);
            portalPlatform.Category.Set("Portal Stations");
            portalPlatform.PlaceEffects.Add("vfx_Place_stone_wall_2x1");
            portalPlatform.PlaceEffects.Add("sfx_build_hammer_stone");
            portalPlatform.HitEffects.Add("vfx_RockHit");
            portalPlatform.HitEffects.Add("sfx_rock_hit");
            portalPlatform.DestroyedEffects.Add("vfx_RockHit");
            portalPlatform.DestroyedEffects.Add("sfx_rock_destroyed");
            portalPlatform.ClonePortalSFXFrom = "portal_wood";
            portalPlatform.Crafting.Set(CraftingTable.Workbench);

            MaterialReplacer.MaterialData StartPlatformMat = new MaterialReplacer.MaterialData(_asset, "_REPLACE_startplatform", MaterialReplacer.ShaderType.RockShader);
            StartPlatformMat.m_texProperties["_EmissiveTex"] = _asset.LoadAsset<Texture>("startstone_emissive");
            StartPlatformMat.m_floatProperties["_Glossiness"] = 0.216f;
            StartPlatformMat.m_texProperties["_MossTex"] = _asset.LoadAsset<Texture>("tex_stone_moss");
            StartPlatformMat.m_floatProperties["_MossAlpha"] = 0f;
            StartPlatformMat.m_floatProperties["_MossBlend"] = 10f;
            StartPlatformMat.m_floatProperties["_MossGloss"] = 0f;
            StartPlatformMat.m_floatProperties["_MossNormal"] = 0.263f;
            StartPlatformMat.m_floatProperties["_MossTransition"] = 0.48f;
            StartPlatformMat.m_floatProperties["_AddSnow"] = 1f;
            StartPlatformMat.m_floatProperties["_AddRain"] = 1f;
            StartPlatformMat.PrefabToModify = Utils.FindChild(portalPlatform.Prefab.transform, "model").gameObject;
            
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(portalPlatform.Prefab.transform, "vanilla_effects").gameObject);
            var portal = portalPlatform.Prefab.AddComponent<PortalStation>();
            portal.m_model = portalPlatform.Prefab.GetComponentInChildren<MeshRenderer>();
            portal.m_baseColor = portal.m_model.material.GetColor("_EmissionColor");
            Stations.Stations.PrefabsToSearch.Add(portalPlatform.Prefab.name);
            
            BuildPiece portalStationDoor = new(_asset, "portalStationDoor");
            portalStationDoor.Name.English("Gate Portal");
            portalStationDoor.Description.English("Teleportation portal");
            portalStationDoor.RequiredItems.Add("Stone", 20, true);
            portalStationDoor.RequiredItems.Add("SurtlingCore", 2, true);
            portalStationDoor.RequiredItems.Add("FineWood", 20, true);
            portalStationDoor.RequiredItems.Add("GreydwarfEye", 10, true);
            portalStationDoor.Category.Set("Portal Stations");
            portalStationDoor.PlaceEffects.Add("vfx_Place_stone_wall_2x1");
            portalStationDoor.PlaceEffects.Add("sfx_build_hammer_wood");
            portalStationDoor.HitEffects.Add("vfx_RockHit");
            portalStationDoor.HitEffects.Add("sfx_rock_hit");
            portalStationDoor.DestroyedEffects.Add("vfx_RockHit");
            portalStationDoor.DestroyedEffects.Add("sfx_rock_destroyed");
            portalStationDoor.ClonePortalSFXFrom = "portal_wood";
            portalStationDoor.Crafting.Set(CraftingTable.Workbench);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(portalStationDoor.Prefab.transform, "model").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(portalStationDoor.Prefab.transform, "vanilla_effects").gameObject);
            portalStationDoor.Prefab.AddComponent<PortalStation>();
            Stations.Stations.PrefabsToSearch.Add(portalStationDoor.Prefab.name);

            PieceCloneManager.BuildClone PortalStationStone = new PieceCloneManager.BuildClone("portal_stone", "PortalStation_Stone");
            PortalStationStone.RequiredItems.Add("Stone", 20, true);
            PortalStationStone.RequiredItems.Add("SurtlingCore", 2, true);
            PortalStationStone.RequiredItems.Add("FineWood", 20, true);
            PortalStationStone.RequiredItems.Add("GreydwarfEye", 10, true);
            PortalStationStone.EnglishName = "Stone Portal Station";
            PortalStationStone.Category = BuildPieceCategory.Misc;
            PortalStationStone.CustomCategory = "Portal Stations";
            PortalStationStone.CraftTable = CraftingTable.Workbench;
            PortalStationStone.IsPortalStation = true;
            
            PieceCloneManager.BuildClone PortalStationWood = new PieceCloneManager.BuildClone("portal_wood", "PortalStation_Wood");
            PortalStationWood.RequiredItems.Add("Stone", 20, true);
            PortalStationWood.RequiredItems.Add("SurtlingCore", 2, true);
            PortalStationWood.RequiredItems.Add("FineWood", 20, true);
            PortalStationWood.RequiredItems.Add("GreydwarfEye", 10, true);
            PortalStationWood.EnglishName = "Wood Portal Station";
            PortalStationWood.Category = BuildPieceCategory.Misc;
            PortalStationWood.CustomCategory = "Portal Stations";
            PortalStationWood.CraftTable = CraftingTable.Workbench;
            PortalStationWood.IsPortalStation = true;

            PieceCloneManager.BuildClone BluePortalStation = new PieceCloneManager.BuildClone("portal", "PortalStation_Blue");
            BluePortalStation.RequiredItems.Add("Stone", 20, true);
            BluePortalStation.RequiredItems.Add("SurtlingCore", 2, true);
            BluePortalStation.RequiredItems.Add("FineWood", 20, true);
            BluePortalStation.RequiredItems.Add("GreydwarfEye", 10, true);
            BluePortalStation.EnglishName = "Legacy Portal Station";
            BluePortalStation.Category = BuildPieceCategory.Misc;
            BluePortalStation.CustomCategory = "Portal Stations";
            BluePortalStation.CraftTable = CraftingTable.Workbench;
            BluePortalStation.IsPortalStation = true;
        }
        private static void InitItems()
        {
            Item PersonalPortalDevice = new("portal_station_assets", "item_personalteleportationdevice");
            PersonalPortalDevice.Name.English("Portable Portal");
            PersonalPortalDevice.Description.English("Travel made easy");
            PersonalPortalDevice.Crafting.Add(ItemManager.CraftingTable.Forge, 2);
            PersonalPortalDevice.RequiredItems.Add("SurtlingCore", 3);
            PersonalPortalDevice.RequiredItems.Add("LeatherScraps", 30);
            PersonalPortalDevice.RequiredItems.Add("Iron", 5);
            PersonalPortalDevice.RequiredItems.Add("YmirRemains", 10);
            PersonalPortalDevice.CraftAmount = 1;
            PersonalPortalDevice.RequiredUpgradeItems.Add("SurtlingCore", 2);
            PersonalPortalDevice.RequiredUpgradeItems.Add("LeatherScraps", 5);
            PersonalPortalDevice.RequiredUpgradeItems.Add("Iron", 2);
            PersonalPortalDevice.RequiredUpgradeItems.Add("YmirRemains", 3);
            PersonalPortalDevice.RequiredUpgradeItems.Free = false;
            PersonalPortalDevice.MaximumRequiredStationLevel = 2;
            PersonalPortalDevice.Configurable = Configurability.Recipe;
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(PersonalPortalDevice.Prefab.transform, "SurtlingCores").gameObject);

        }
        private void Update() => PortalStationGUI.UpdateGUI();
        private void OnDestroy() => Config.Save();
        
        private static AssetBundle GetAssetBundle(string fileName)
        {
            Assembly execAssembly = Assembly.GetExecutingAssembly();
            string resourceName = execAssembly.GetManifestResourceNames().Single(str => str.EndsWith(fileName));
            using Stream? stream = execAssembly.GetManifestResourceStream(resourceName);
            return AssetBundle.LoadFromStream(stream);
        }
        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }
        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                PortalStationsLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                PortalStationsLogger.LogError($"There was an issue loading your {ConfigFileName}");
                PortalStationsLogger.LogError("Please check your config entries for spelling and format!");
            }
        }
        
        #region ConfigOptions

        private static ConfigEntry<Toggle> _serverConfigLocked = null!;

        public static ConfigEntry<Toggle> _TeleportAnything = null!;
        public static ConfigEntry<string> _DeviceFuel = null!;
        public static ConfigEntry<Toggle> _DeviceUseFuel = null!;
        public static ConfigEntry<int> _DevicePerFuelAmount = null!;
        public static ConfigEntry<int> _DeviceAdditionalDistancePerUpgrade = null!;
        public static ConfigEntry<Toggle> _PortalToPlayers = null!;

        public static ConfigEntry<Toggle> _PortalUseFuel = null!;
        public static ConfigEntry<int> _PortalPerFuelAmount = null!;

        public static ConfigEntry<Toggle> _UsePortalKeys = null!;

        public static ConfigEntry<string> _StationTitle = null!;
        public static ConfigEntry<string> _PortableStationTitle = null!;
        public static ConfigEntry<string> _StationDestinationText = null!;
        public static ConfigEntry<string> _StationCloseText = null!;
        public static ConfigEntry<string> _StationFilterText = null!;
        public static ConfigEntry<string> _StationSetNameText = null!;
        public static ConfigEntry<string> _StationUseText = null!;
        public static ConfigEntry<string> _StationRenameText = null!;
        public static ConfigEntry<string> _NotEnoughFuelText = null!;
        public static ConfigEntry<string> _PublicText = null!;
        public static ConfigEntry<string> _PrivateText = null!;

        public static ConfigEntry<Toggle> _OnlyAdminRename = null!;

        public static ConfigEntry<float> _PortalVolume = null!;
        public static ConfigEntry<float> _PersonalPortalDurabilityDrain = null!;

        public static ConfigEntry<Toggle> _OnlyAdminBuilds = null!;

        private void InitConfigs()
        {
            _TeleportAnything = config("Settings", "1 - Teleport Anything", Toggle.Off,
                "If on, portal station allows to teleport without restrictions");
            _DeviceUseFuel = config("Settings", "2 - Portable Portal Use Fuel", Toggle.On,
                "If on, personal teleportation device uses fuel");
            _DeviceFuel = config("Settings", "3 - Portable Portal Fuel", "Coins",
                "Set the prefab name of the fuel item required to teleport");
            _DevicePerFuelAmount = config("Settings", "4 - Portable Portal Fuel Distance", 1,
                new ConfigDescription("Fuel cost to travel, higher value increases range per fuel",
                    new AcceptableValueRange<int>(1, 50)));
            _DeviceAdditionalDistancePerUpgrade = config("Settings", "5 - Portable Portal Upgrade Boost", 1,
                new ConfigDescription("Cost reduction multiplier per item upgrade level",
                    new AcceptableValueRange<int>(1, 50)));
            _PortalToPlayers = config("Settings", "6 - Portal To Players", Toggle.Off,
                "If on, portal shows players as destination options");
            _OnlyAdminRename = config("Settings", "7 - Only Admin Renames", Toggle.Off,
                "If on, only admins with no cost cheat on can rename portals");

            _PortalUseFuel = config("Settings", "8 - Portal Use Fuel", Toggle.Off,
                "If on, static portals require fuel");
            _PortalPerFuelAmount = config("Settings", "9 - Portal Fuel Distance", 10,
                new ConfigDescription("Fuel cost per distance", new AcceptableValueRange<int>(1, 101)));

            _PortalVolume = config("Settings", "8 - Portal Volume", 0.8f,
                new ConfigDescription("Set the volume of the portal effects", new AcceptableValueRange<float>(0f, 1f)),
                false);
            _PersonalPortalDurabilityDrain = config("Settings", "9 - Portable Portal Durability Drain", 10.0f,
                new ConfigDescription("Set the durability drain per portable portal usage",
                    new AcceptableValueRange<float>(0f, 100f)));

            _UsePortalKeys = config("Teleport Keys", "0 - Use Keys", Toggle.Off,
                "If on, portal checks keys to portal player if carrying ores, dragon eggs, etc...");

            _StationTitle = config("Localization", "0 - Station Title", "Portal Station",
                "Station name on user interface", false);
            _PortableStationTitle = config("Localization", "1 - Portable Station Title", "Teleporter",
                "Portable Portal name on user interface", false);
            _StationDestinationText = config("Localization", "2 - Destination Text", "Destinations",
                "Text display for destinations on user interface", false);
            _StationCloseText = config("Localization", "3 - Close Text", "Close",
                "Text display for close button on user interface", false);
            _StationFilterText = config("Localization", "4 - Filter Text", "Filter",
                "Text display for filter on user interface", false);
            _StationRenameText = config("Localization", "5 - Rename Text", "Rename Portal",
                "Text display on pop up to rename portal", false);
            _StationUseText = config("Localization", "6 - Use Text", "Use portal",
                "Text display when hover over station to use portal", false);
            _StationSetNameText = config("Localization", "7 - Set Name Text", "Set Name",
                "Text display when hover over station to rename", false);
            _NotEnoughFuelText = config("Localization", "8 - Not Enough Fuel", "Not Enough Fuel",
                "Text that appears on portable portal GUI if user does not have enough fuel", false);
            _PublicText = config("Localization", "9 - Public Text", "Public", "Text display for public toggle", false);
            _PrivateText = config("Localization", "9 - Private Text", "Private", "Text display for private toggle",
                false);

            _OnlyAdminBuilds = config("General", "Only Admin Can Build", Toggle.Off,
                "Set visibility of portals in build menu");
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        public ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order = null!;
            [UsedImplicitly] public bool? Browsable = null!;
            [UsedImplicitly] public string? Category = null!;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer = null!;
        }

        #endregion
    }
}