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
using PieceManager;
using PortalStations.Stations;
using ServerSync;
using UnityEngine;
using CraftingTable = PieceManager.CraftingTable;

namespace PortalStations
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class PortalStationsPlugin : BaseUnityPlugin
    {
        internal const string ModName = "PortalStations";
        internal const string ModVersion = "1.0.0";
        internal const string Author = "RustyMods";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource PortalStationsLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
        private static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        public static PortalStationsPlugin _plugin = null!;
        public static AssetBundle _asset = null!;
        public enum Toggle { On = 1, Off = 0 }

        public void Awake()
        {
            _plugin = this;
            _asset = GetAssetBundle("portal_station_assets");
            
            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On,
                "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            BuildPiece PortalStation = new("portal_station_assets", "portalstation");
            PortalStation.Name.English("Ancient Portal");
            PortalStation.Description.English("Teleportation portal");
            PortalStation.RequiredItems.Add("Stone", 20, true);
            PortalStation.RequiredItems.Add("SurtlingCore", 2, true);
            PortalStation.RequiredItems.Add("FineWood", 20, true);
            PortalStation.RequiredItems.Add("GreydwarfEye", 10, true);
            PortalStation.Category.Set(BuildPieceCategory.Misc);
            PortalStation.Crafting.Set(CraftingTable.Workbench);
            MaterialReplacer.RegisterGameObjectForShaderSwap(PortalStation.Prefab.transform.Find("Visual Root").gameObject, MaterialReplacer.ShaderType.PieceShader);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(PortalStation.Prefab.transform, "vanilla_effects").gameObject);
            PortalStation.Prefab.AddComponent<PortalStation>();
            PieceEffectsSetter.PrefabsToSet.Add(PortalStation.Prefab);
            Stations.Stations.PrefabsToSearch.Add(PortalStation.Prefab.name);
            
            BuildPiece PortalStationOne = new("portal_station_assets", "portalStationOne");
            PortalStationOne.Name.English("Chained Portal");
            PortalStationOne.Description.English("Teleportation portal");
            PortalStationOne.RequiredItems.Add("Stone", 20, true);
            PortalStationOne.RequiredItems.Add("SurtlingCore", 2, true);
            PortalStationOne.RequiredItems.Add("FineWood", 20, true);
            PortalStationOne.RequiredItems.Add("GreydwarfEye", 10, true);
            PortalStationOne.Category.Set(BuildPieceCategory.Misc);
            PortalStationOne.Crafting.Set(CraftingTable.Workbench);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(PortalStationOne.Prefab.transform, "vanilla_effects").gameObject);
            PortalStationOne.Prefab.AddComponent<PortalStation>();
            PieceEffectsSetter.PrefabsToSet.Add(PortalStationOne.Prefab);
            Stations.Stations.PrefabsToSearch.Add(PortalStationOne.Prefab.name);
            
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
            
            Stations.Stations.InitCoroutine();
            InitConfigs();
            
            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private void Update()
        {
            PortalStationGUI.UpdateGUI();
            PersonalTeleportationGUI.UpdatePersonalGUI();
        }

        private void OnDestroy()
        {
            Config.Save();
        }

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

        private void InitConfigs()
        {
            _TeleportAnything = config("Settings", "1 - Teleport Anything", Toggle.Off, "If on, portal station allows to teleport without restrictions");
            _DeviceUseFuel = config("Settings", "2 - Portable Portal Use Fuel", Toggle.On, "If on, personal teleportation device uses fuel");
            _DeviceFuel = config("Settings", "3 - Portable Portal Fuel", "SurtlingCore", "Set the prefab name of the fuel item required to teleport");
            _DevicePerFuelAmount = config("Settings", "4 - Portable Portal Fuel Distance", 1, new ConfigDescription("Fuel cost to travel, higher value increases range per fuel", new AcceptableValueRange<int>(1, 50)));
            _DeviceAdditionalDistancePerUpgrade = config("Settings", "5 - Portable Portal Upgrade Boost", 1, new ConfigDescription("Cost reduction multiplier per item upgrade level", new AcceptableValueRange<int>(1, 50)));
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

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
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

        // class AcceptableShortcuts : AcceptableValueBase
        // {
        //     public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
        //     {
        //     }
        //
        //     public override object Clamp(object value) => value;
        //     public override bool IsValid(object value) => true;
        //
        //     public override string ToDescriptionString() =>
        //         "# Acceptable values: " + string.Join(", ", UnityInput.Current.SupportedKeyCodes);
        // }

        #endregion
    }
}