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
using PortalStations.UI;
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
        internal const string ModVersion = "1.2.9";
        internal const string Author = "RustyMods";
        private const string ModGUID = Author + "." + ModName;
        private const string ConfigFileName = ModGUID + ".cfg";
        private static readonly string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource PortalStationsLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
        private static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        public static PortalStationsPlugin _plugin = null!;
        public static AssetBundle PrefabAssets = null!;
        public static AssetBundle UIAssets = null!;
        public static GameObject _root = null!;
        public enum Toggle { On = 1, Off = 0 }
        
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
        public static ConfigEntry<Toggle> _OnlyAdminRename = null!;
        public static ConfigEntry<float> _PortalVolume = null!;
        public static ConfigEntry<float> _PersonalPortalDurabilityDrain = null!;
        public static ConfigEntry<Toggle> _OnlyAdminBuilds = null!;
        public static ConfigEntry<FontManager.FontOptions> _Font = null!;
        public static ConfigEntry<Toggle> _DisplayBiome = null!;
        public static ConfigEntry<Toggle> _DisplayDistance = null!;

        private void InitConfigs()
        {
            _Font = config("User Interface", "Font", FontManager.FontOptions.AveriaSerifLibre, "Set font");
            _Font.SettingChanged += FontManager.OnFontChange;
            _DeviceUseFuel = config("Settings", "2 - Portable Portal Use Fuel", Toggle.On, "If on, personal teleportation device uses fuel");
            _DeviceFuel = config("Settings", "3 - Portable Portal Fuel", "Coins", "Set the prefab name of the fuel item required to teleport");
            _DevicePerFuelAmount = config("Settings", "4 - Portable Portal Fuel Distance", 1, new ConfigDescription("Fuel cost to travel, higher value increases range per fuel", new AcceptableValueRange<int>(1, 50)));
            _DeviceAdditionalDistancePerUpgrade = config("Settings", "5 - Portable Portal Upgrade Boost", 1, new ConfigDescription("Cost reduction multiplier per item upgrade level", new AcceptableValueRange<int>(1, 50)));
            _PortalToPlayers = config("Settings", "6 - Portal To Players", Toggle.Off, "If on, portal shows players as destination options");
            _OnlyAdminRename = config("Settings", "7 - Only Admin Renames", Toggle.Off, "If on, only admins with no cost cheat on can rename portals");
            _PortalUseFuel = config("Settings", "8 - Portal Use Fuel", Toggle.Off, "If on, static portals require fuel");
            _PortalPerFuelAmount = config("Settings", "9 - Portal Fuel Distance", 10, new ConfigDescription("Fuel cost per distance", new AcceptableValueRange<int>(1, 101)));
            _PortalVolume = config("Settings", "8 - Portal Volume", 0.8f, new ConfigDescription("Set the volume of the portal effects", new AcceptableValueRange<float>(0f, 1f)), false);
            _PersonalPortalDurabilityDrain = config("Settings", "9 - Portable Portal Durability Drain", 10.0f, new ConfigDescription("Set the durability drain per portable portal usage", new AcceptableValueRange<float>(0f, 100f)));
            _TeleportAnything = config("Settings", "1 - Teleport Anything", Toggle.Off, "If on, portal station allows to teleport without restrictions");
            _UsePortalKeys = config("Teleport Keys", "0 - Use Keys", Toggle.Off, "If on, portal checks keys to portal player if carrying ores, dragon eggs, etc...");
            _OnlyAdminBuilds = config("Settings", "Only Admin Can Build", Toggle.Off, "Set visibility of portals in build menu");
            _DisplayBiome = config("User Interface", "Show Biome", Toggle.Off, "If on, biome will be added to portal name");
            _DisplayDistance = config("User Interface", "Show Distance", Toggle.Off, "If on, distance will be added to portal name");
        }
        public void Awake()
        {
            _plugin = this;
            PrefabAssets = GetAssetBundle("portal_station_assets");
            UIAssets = GetAssetBundle("portalstationui");
            _root = new GameObject("root");
            DontDestroyOnLoad(_root);
            _root.SetActive(false);

            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On,
                "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            Localizer.Load();
            InitPieces();
            InitItems();
            InitConfigs();
            
            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private static void InitPieces()
        {
            BuildPiece PortalStation = new(PrefabAssets, "portalstation");
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
            MaterialReplacer.RegisterGameObjectForShaderSwap(PortalStation.Prefab.transform.Find("model").gameObject, MaterialReplacer.ShaderType.PieceShader);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(PortalStation.Prefab.transform, "vanilla_effects").gameObject);
            PortalStation.Prefab.AddComponent<PortalStation>();
            Stations.Stations.PrefabsToSearch.Add(PortalStation.Prefab.name);

            BuildPiece PortalStationOne = new(PrefabAssets, "portalStationOne");
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
            MaterialReplacer.RegisterGameObjectForShaderSwap(PortalStationOne.Prefab.transform.Find("VisualRoot").gameObject, MaterialReplacer.ShaderType.PieceShader);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(PortalStationOne.Prefab.transform, "vanilla_effects").gameObject);
            PortalStationOne.Prefab.AddComponent<PortalStation>();
            Stations.Stations.PrefabsToSearch.Add(PortalStationOne.Prefab.name);
            
            BuildPiece portalPlatform = new(PrefabAssets, "portalPlatform");
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

            MaterialReplacer.MaterialData StartPlatformMat = new MaterialReplacer.MaterialData(PrefabAssets, "_REPLACE_startplatform", MaterialReplacer.ShaderType.RockShader);
            StartPlatformMat.m_texProperties["_EmissiveTex"] = PrefabAssets.LoadAsset<Texture>("startstone_emissive");
            StartPlatformMat.m_floatProperties["_Glossiness"] = 0.216f;
            StartPlatformMat.m_texProperties["_MossTex"] = PrefabAssets.LoadAsset<Texture>("tex_stone_moss1");
            StartPlatformMat.m_floatProperties["_MossAlpha"] = 0f;
            StartPlatformMat.m_floatProperties["_MossBlend"] = 10f;
            StartPlatformMat.m_floatProperties["_MossGloss"] = 0f;
            StartPlatformMat.m_floatProperties["_MossNormal"] = 0.263f;
            StartPlatformMat.m_floatProperties["_MossTransition"] = 0.48f;
            StartPlatformMat.m_floatProperties["_AddSnow"] = 1f;
            StartPlatformMat.m_floatProperties["_AddRain"] = 1f;
            StartPlatformMat.m_texProperties["_EmissiveTex"] = PrefabAssets.LoadAsset<Texture>("startstone_emissive_bw1");
            StartPlatformMat.PrefabToModify = Utils.FindChild(portalPlatform.Prefab.transform, "model").gameObject;
            
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(portalPlatform.Prefab.transform, "vanilla_effects").gameObject);
            portalPlatform.Prefab.AddComponent<PortalStation>();
            Stations.Stations.PrefabsToSearch.Add(portalPlatform.Prefab.name);
            
            BuildPiece portalStationDoor = new(PrefabAssets, "portalStationDoor");
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

        private void Update()
        {
            if (!Player.m_localPlayer || !StationUI.m_instance) return;
            if (Input.GetKeyDown(KeyCode.Escape) && StationUI.IsVisible())
            {
                StationUI.m_instance.OnClose();
            }
        }
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
    }
}