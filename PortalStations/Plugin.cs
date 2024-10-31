using System;
using System.Collections.Generic;
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
    [BepInDependency("org.bepinex.plugins.groups", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("org.bepinex.plugins.guilds", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class PortalStationsPlugin : BaseUnityPlugin
    {
        internal const string ModName = "PortalStations";
        internal const string ModVersion = "1.2.3";
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

        public static Texture PlatformMoss = null!;
        public static Texture PlatformNormal = null!;

        public void Awake()
        {
            _plugin = this;
            _asset = GetAssetBundle("portal_station_assets");
            _root = new GameObject("root");
            DontDestroyOnLoad(_root);
            _root.SetActive(false);

            PlatformMoss = _asset.LoadAsset<Texture>("tex_stone_moss");
            PlatformNormal = _asset.LoadAsset<Texture>("normal_portal_platform");

            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On,
                "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            InitPieces();
            InitItems();
            InitConfigs();
            
            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();

            _harmony.Patch(AccessTools.DeclaredMethod(typeof(ZNetScene), nameof(ZNetScene.Awake)),
                postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(PortalStationsPlugin),
                    nameof(ClonedPieces))));
        }

        private void InitPieces()
        {
            BuildPiece PortalStation = new("portal_station_assets", "portalstation");
            PortalStation.Name.English("Ancient Portal");
            PortalStation.Description.English("Teleportation portal");
            PortalStation.RequiredItems.Add("Stone", 20, true);
            PortalStation.RequiredItems.Add("SurtlingCore", 2, true);
            PortalStation.RequiredItems.Add("FineWood", 20, true);
            PortalStation.RequiredItems.Add("GreydwarfEye", 10, true);
            PortalStation.Category.Set(BuildPieceCategory.Misc);
            PortalStation.Crafting.Set(CraftingTable.Workbench);
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
            
            BuildPiece portalPlatform = new("portal_station_assets", "portalPlatform");
            portalPlatform.Name.English("Platform Portal");
            portalPlatform.Description.English("Teleportation portal");
            portalPlatform.RequiredItems.Add("Stone", 20, true);
            portalPlatform.RequiredItems.Add("SurtlingCore", 2, true);
            portalPlatform.RequiredItems.Add("FineWood", 20, true);
            portalPlatform.RequiredItems.Add("GreydwarfEye", 10, true);
            portalPlatform.Category.Set(BuildPieceCategory.Misc);
            portalPlatform.Crafting.Set(CraftingTable.Workbench);
            MaterialReplacer.RegisterGameObjectForShaderSwap(Utils.FindChild(portalPlatform.Prefab.transform, "model").gameObject, MaterialReplacer.ShaderType.RockShader);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(portalPlatform.Prefab.transform, "vanilla_effects").gameObject);
            portalPlatform.Prefab.AddComponent<PortalStation>();
            PieceEffectsSetter.PrefabsToSet.Add(portalPlatform.Prefab);
            Stations.Stations.PrefabsToSearch.Add(portalPlatform.Prefab.name);
            
            BuildPiece portalStationDoor = new("portal_station_assets", "portalStationDoor");
            portalStationDoor.Name.English("Gate Portal");
            portalStationDoor.Description.English("Teleportation portal");
            portalStationDoor.RequiredItems.Add("Stone", 20, true);
            portalStationDoor.RequiredItems.Add("SurtlingCore", 2, true);
            portalStationDoor.RequiredItems.Add("FineWood", 20, true);
            portalStationDoor.RequiredItems.Add("GreydwarfEye", 10, true);
            portalStationDoor.Category.Set(BuildPieceCategory.Misc);
            portalStationDoor.Crafting.Set(CraftingTable.Workbench);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(portalStationDoor.Prefab.transform, "model").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(portalStationDoor.Prefab.transform, "vanilla_effects").gameObject);
            portalStationDoor.Prefab.AddComponent<PortalStation>();
            PieceEffectsSetter.PrefabsToSet.Add(portalStationDoor.Prefab);
            Stations.Stations.PrefabsToSearch.Add(portalStationDoor.Prefab.name);
        }

        public static void ClonedPieces(ZNetScene __instance)
        {
            if (!__instance) return;
            Clone(__instance, "portal_stone", "PortalStation_Stone", "Stone Portal Station");
            Clone(__instance, "portal_wood", "PortalStation_Wood", "Wood Portal Station");
            Clone(__instance, "portal", "PortalStation_Blue", "Portal Station Stone");
            Stations.Stations.InitCoroutine();
        }

        private static void Clone(ZNetScene __instance, string prefabName, string newName, string displayName)
        {
            GameObject prefab = __instance.GetPrefab(prefabName);
            if (!prefab) return;

            GameObject? clone = Instantiate(prefab, _root.transform, false);
            clone.name = newName;

            TeleportWorld world = clone.GetComponent<TeleportWorld>();
            PortalStation portal = clone.AddComponent<PortalStation>();
            portal.m_model = clone.GetComponentInChildren<MeshRenderer>();
            portal.m_baseColor = world.m_colorTargetfound;
            Destroy(world);
            TeleportWorldTrigger? teleport = clone.GetComponentInChildren<TeleportWorldTrigger>();
            if (teleport) Destroy(teleport.gameObject);
            EffectFade fade = clone.GetComponentInChildren<EffectFade>();
            if (fade) Destroy(fade);

            if (!clone.TryGetComponent(out Piece piece)) return;

            ConfigEntry<string> display = _plugin.config(newName, "Display Name", displayName, "Set display name");
            piece.m_name = display.Value;
            display.SettingChanged += (sender, args) => piece.m_name = display.Value;
            ConfigEntry<string> recipe = _plugin.config(newName, "Recipe", "GreydwarfEye:10,FineWood:20,SurtlingCore:2", "Set recipe");
            piece.m_resources = GetRequirements(recipe.Value).ToArray();
            recipe.SettingChanged += (sender, args) => piece.m_resources = GetRequirements(recipe.Value).ToArray();
            Stations.Stations.PrefabsToSearch.Add(clone.name);
            
            RegisterToScene(clone);
            RegisterToHammer(clone);
        }

        private static List<Piece.Requirement> GetRequirements(string config)
        {
            List<Piece.Requirement> output = new();
            foreach (var input in config.Split(','))
            {
                var info = input.Split(':');
                if (info.Length != 2) continue;
                GameObject prefab = ZNetScene.instance.GetPrefab(info[0]);
                if (!prefab) continue;
                if (!prefab.TryGetComponent(out ItemDrop item)) continue;
                output.Add(new Piece.Requirement()
                {
                    m_resItem = item,
                    m_amount = int.TryParse(info[1], out int amount) ? amount : 1,
                    m_recover = true,
                    m_amountPerLevel = 1,
                    m_extraAmountOnlyOneIngredient = 1
                });
            }

            return output;
        }

        private static void RegisterToHammer(GameObject prefab)
        {
            GameObject Hammer = ZNetScene.instance.GetPrefab("Hammer");
            if (!Hammer) return;
            if (!Hammer.TryGetComponent(out ItemDrop component)) return;
            if (!component.m_itemData.m_shared.m_buildPieces.m_pieces.Contains(prefab))
            {
                component.m_itemData.m_shared.m_buildPieces.m_pieces.Add(prefab);
            }
        }

        private static void RegisterToScene(GameObject prefab)
        {
            if (!ZNetScene.instance.m_prefabs.Contains(prefab))
            {
                ZNetScene.instance.m_prefabs.Add(prefab);
            }

            ZNetScene.instance.m_namedPrefabs[prefab.name.GetStableHashCode()] = prefab;
        }
        private void InitItems()
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
            _TeleportAnything = config("Settings", "1 - Teleport Anything", Toggle.Off, "If on, portal station allows to teleport without restrictions");
            _DeviceUseFuel = config("Settings", "2 - Portable Portal Use Fuel", Toggle.On, "If on, personal teleportation device uses fuel");
            _DeviceFuel = config("Settings", "3 - Portable Portal Fuel", "Coins", "Set the prefab name of the fuel item required to teleport");
            _DevicePerFuelAmount = config("Settings", "4 - Portable Portal Fuel Distance", 1, new ConfigDescription("Fuel cost to travel, higher value increases range per fuel", new AcceptableValueRange<int>(1, 50)));
            _DeviceAdditionalDistancePerUpgrade = config("Settings", "5 - Portable Portal Upgrade Boost", 1, new ConfigDescription("Cost reduction multiplier per item upgrade level", new AcceptableValueRange<int>(1, 50)));
            _PortalToPlayers = config("Settings", "6 - Portal To Players", Toggle.Off, "If on, portal shows players as destination options");
            _OnlyAdminRename = config("Settings", "7 - Only Admin Renames", Toggle.Off,
                "If on, only admins with no cost cheat on can rename portals");

            _PortalUseFuel = config("Settings", "8 - Portal Use Fuel", Toggle.Off,
                "If on, static portals require fuel");
            _PortalPerFuelAmount = config("Settings", "9 - Portal Fuel Distance", 10,
                new ConfigDescription("Fuel cost per distance", new AcceptableValueRange<int>(1, 101)));

            _PortalVolume = config("Settings", "8 - Portal Volume", 0.8f, new ConfigDescription("Set the volume of the portal effects", new AcceptableValueRange<float>(0f, 1f)),false);
            _PersonalPortalDurabilityDrain = config("Settings", "9 - Portable Portal Durability Drain", 10.0f,
                new ConfigDescription("Set the durability drain per portable portal usage",
                    new AcceptableValueRange<float>(0f, 100f)));

            _UsePortalKeys = config("Teleport Keys", "0 - Use Keys", Toggle.Off, "If on, portal checks keys to portal player if carrying ores, dragon eggs, etc...");

            _StationTitle = config("Localization", "0 - Station Title", "Portal Station", "Station name on user interface", false);
            _PortableStationTitle = config("Localization", "1 - Portable Station Title", "Teleporter", "Portable Portal name on user interface", false);
            _StationDestinationText = config("Localization", "2 - Destination Text", "Destinations", "Text display for destinations on user interface", false);
            _StationCloseText = config("Localization", "3 - Close Text", "Close", "Text display for close button on user interface", false);
            _StationFilterText = config("Localization", "4 - Filter Text", "Filter", "Text display for filter on user interface", false);
            _StationRenameText = config("Localization", "5 - Rename Text", "Rename Portal", "Text display on pop up to rename portal", false);
            _StationUseText = config("Localization", "6 - Use Text", "Use portal", "Text display when hover over station to use portal", false);
            _StationSetNameText = config("Localization", "7 - Set Name Text", "Set Name", "Text display when hover over station to rename", false);
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