using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using PieceManager;
using PortalStations.Stations;
using UnityEngine;

namespace PortalStations.Managers;

public static class PieceCloneManager
{
    private static readonly Dictionary<string, BuildPiece> ClonedPieces = new();
    private static readonly List<BuildClone> PiecesToClone = new();

    private static bool Initialized;

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    private static class ZNetScene_Awake_Patch
    {
        private static void Postfix()
        {
            foreach (var piece in PiecesToClone)
            {
                if (ZNetScene.instance.GetPrefab(piece.OriginalPrefab) is { } original && original.TryGetComponent(out Piece originalPiece))
                {
                    var clone = UnityEngine.Object.Instantiate(original, PortalStationsPlugin._root.transform, false);
                    clone.name = piece.PrefabName;
                    if (!clone.TryGetComponent(out Piece component)) continue;
                    component.m_name = $"$piece_{clone.name.ToLower()}";
                    component.m_description = $"$piece_{clone.name.ToLower()}_desc";
                        
                    if (piece.IsPortalStation) ConvertToStation(clone);

                    BuildPiece buildPiece = new BuildPiece(clone);
                    buildPiece.Name.English(piece.EnglishName);
                    buildPiece.Description.English(Localization.instance.Localize(originalPiece.m_description));
                    if (piece.CustomCategory.IsNullOrWhiteSpace()) buildPiece.Category.Set(piece.Category);
                    else buildPiece.Category.Set(piece.CustomCategory);
                    buildPiece.RequiredItems = piece.RequiredItems;
                    if (piece.CustomCraftTable.IsNullOrWhiteSpace()) buildPiece.Crafting.Set(piece.CraftTable);
                    else buildPiece.Crafting.Set(piece.CustomCraftTable);
                    ClonedPieces[clone.name] = buildPiece;
                    Register(clone);
                }
            }
                
            InitClonePieces();
            Stations.Stations.InitCoroutine();
        }

        private static void Register(GameObject prefab)
        {
            if (!ZNetScene.instance.m_prefabs.Contains(prefab)) ZNetScene.instance.m_prefabs.Add(prefab);
            ZNetScene.instance.m_namedPrefabs[prefab.name.GetStableHashCode()] = prefab;
        }

        private static void ConvertToStation(GameObject clone)
        {
            if (!clone.TryGetComponent(out TeleportWorld world)) return;
            PortalStation portal = clone.AddComponent<PortalStation>();
            
            portal.m_model = clone.GetComponentInChildren<MeshRenderer>();
            portal.m_baseColor = world.m_colorTargetfound;
            
            UnityEngine.Object.Destroy(world);
            if (clone.GetComponentInChildren<TeleportWorldTrigger>() is {} teleport) UnityEngine.Object.Destroy(teleport.gameObject);
            if (clone.GetComponentInChildren<EffectFade>() is {} fade) UnityEngine.Object.Destroy(fade);
            
            Stations.Stations.PrefabsToSearch.Add(clone.name);
        }
    }

    public class BuildClone
    {
        public readonly string OriginalPrefab;
        public readonly string PrefabName;
        public string EnglishName = "";
        public string CustomCategory = "";
        public BuildPieceCategory Category = BuildPieceCategory.Misc;
        public CraftingTable CraftTable = CraftingTable.None;
        public string CustomCraftTable = "";
        public readonly RequiredResourcesList RequiredItems = new();
        public bool IsPortalStation = false;
        
        public BuildClone(string originalPrefab, string newPrefab)
        {
            OriginalPrefab = originalPrefab;
            PrefabName = newPrefab;
            PiecesToClone.Add(this);
        }
    }
    
    
    private static void InitClonePieces()
    {
        if (Initialized) return;
        Assembly? bepinexConfigManager = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "ConfigurationManager");
        Type? configManagerType = bepinexConfigManager?.GetType("ConfigurationManager.ConfigurationManager");
        void ReloadConfigDisplay()
        {
            if (configManagerType?.GetProperty("DisplayingWindow")!.GetValue(BuildPiece.configManager) is true)
            {
                configManagerType.GetMethod("BuildSettingList")!.Invoke(BuildPiece.configManager, Array.Empty<object>());
            }
        }
        foreach (BuildPiece piece in ClonedPieces.Values)
        {
            piece.activeTools = piece.Tool.Tools.DefaultIfEmpty("Hammer").ToArray();
            if (piece.Category.Category != BuildPieceCategory.Custom)
            {
                piece.Prefab.GetComponent<Piece>().m_category = (Piece.PieceCategory)piece.Category.Category;
            }
            else
            {
                piece.Prefab.GetComponent<Piece>().m_category = PiecePrefabManager.GetCategory(piece.Category.custom);
            }
        }

        if (BuildPiece.ConfigurationEnabled)
        {
            bool SaveOnConfigSet = BuildPiece.plugin.Config.SaveOnConfigSet;
            BuildPiece.plugin.Config.SaveOnConfigSet = false;
            foreach (BuildPiece piece in ClonedPieces.Values)
            {
                if (piece.SpecialProperties.NoConfig) continue;
                BuildPiece.PieceConfig cfg = BuildPiece.pieceConfigs[piece] = new BuildPiece.PieceConfig();
                Piece piecePrefab = piece.Prefab.GetComponent<Piece>();
                string pieceName = piecePrefab.m_name;
                string englishName = new Regex(@"[=\n\t\\""\'\[\]]*").Replace(BuildPiece.english.Localize(pieceName), "").Trim();
                string localizedName = Localization.instance.Localize(pieceName).Trim();

                int order = 0;

                cfg.category = BuildPiece.config(englishName, "Build Table Category", piece.Category.Category,
                    new ConfigDescription($"Build Category where {localizedName} is available.", null,
                        new BuildPiece.ConfigurationManagerAttributes
                            { Order = --order, Category = localizedName }));
                BuildPiece.ConfigurationManagerAttributes customTableAttributes = new()
                {
                    Order = --order, Browsable = cfg.category.Value == BuildPieceCategory.Custom,
                    Category = localizedName,
                };
                cfg.customCategory = BuildPiece.config(englishName, "Custom Build Category", piece.Category.custom, new ConfigDescription("", null, customTableAttributes));

                void BuildTableConfigChanged(object o, EventArgs e)
                {
                    if (BuildPiece.registeredPieces.Count > 0)
                    {
                        if (cfg.category.Value is BuildPieceCategory.Custom)
                        {
                            piecePrefab.m_category = PiecePrefabManager.GetCategory(cfg.customCategory.Value);
                        }
                        else
                        {
                            piecePrefab.m_category = (Piece.PieceCategory)cfg.category.Value;
                        }

                        if (Hud.instance)
                        {
                            PiecePrefabManager.CategoryRefreshNeeded = true;
                            PiecePrefabManager.CreateCategoryTabs();
                        }
                    }

                    customTableAttributes.Browsable = cfg.category.Value == BuildPieceCategory.Custom;
                    ReloadConfigDisplay();
                }

                cfg.category.SettingChanged += BuildTableConfigChanged;
                cfg.customCategory.SettingChanged += BuildTableConfigChanged;

                if (cfg.category.Value is BuildPieceCategory.Custom)
                {
                    piecePrefab.m_category = PiecePrefabManager.GetCategory(cfg.customCategory.Value);
                }
                else
                {
                    piecePrefab.m_category = (Piece.PieceCategory)cfg.category.Value;
                }

                cfg.tools = BuildPiece.config(englishName, "Tools", string.Join(", ", piece.activeTools),
                    new ConfigDescription($"Comma separated list of tools where {localizedName} is available.", null,
                        customTableAttributes));
                piece.activeTools = cfg.tools.Value.Split(',').Select(s => s.Trim()).ToArray();
                cfg.tools.SettingChanged += (_, _) =>
                {
                    Inventory[] inventories = Player.s_players.Select(p => p.GetInventory())
                        .Concat(UnityEngine.Object.FindObjectsOfType<Container>().Select(c => c.GetInventory()))
                        .Where(c => c is not null).ToArray();
                    Dictionary<string, List<PieceTable>> tools = ObjectDB.instance.m_items
                        .Select(p => p.GetComponent<ItemDrop>()).Where(c => c && c.GetComponent<ZNetView>())
                        .Concat(ItemDrop.s_instances)
                        .Select(i =>
                            new KeyValuePair<string, ItemDrop.ItemData>(Utils.GetPrefabName(i.gameObject), i.m_itemData))
                        .Concat(inventories.SelectMany(i => i.GetAllItems()).Select(i =>
                            new KeyValuePair<string, ItemDrop.ItemData>(i.m_dropPrefab.name, i)))
                        .Where(kv => kv.Value.m_shared.m_buildPieces).GroupBy(kv => kv.Key).ToDictionary(g => g.Key,
                            g => g.Select(kv => kv.Value.m_shared.m_buildPieces).Distinct().ToList());

                    foreach (string tool in piece.activeTools)
                    {
                        if (tools.TryGetValue(tool, out List<PieceTable> existingTools))
                        {
                            foreach (PieceTable table in existingTools)
                            {
                                table.m_pieces.Remove(piece.Prefab);
                            }
                        }
                    }

                    piece.activeTools = cfg.tools.Value.Split(',').Select(s => s.Trim()).ToArray();
                    if (ObjectDB.instance)
                    {
                        foreach (string tool in piece.activeTools)
                        {
                            if (tools.TryGetValue(tool, out List<PieceTable> existingTools))
                            {
                                foreach (PieceTable table in existingTools)
                                {
                                    if (!table.m_pieces.Contains(piece.Prefab))
                                    {
                                        table.m_pieces.Add(piece.Prefab);
                                    }
                                }
                            }
                        }

                        if (Player.m_localPlayer && Player.m_localPlayer.m_buildPieces)
                        {
                            PiecePrefabManager.CategoryRefreshNeeded = true;
                            Player.m_localPlayer.SetPlaceMode(Player.m_localPlayer.m_buildPieces);
                        }
                    }
                };

                if (piece.Crafting.Stations.Count > 0)
                {
                    List<BuildPiece.ConfigurationManagerAttributes> hideWhenNoneAttributes = new();

                    cfg.table = BuildPiece.config(englishName, "Crafting Station", piece.Crafting.Stations.First().Table,
                        new ConfigDescription($"Crafting station where {localizedName} is available.", null,
                            new BuildPiece.ConfigurationManagerAttributes { Order = --order }));
                    cfg.customTable = BuildPiece.config(englishName, "Custom Crafting Station",
                        piece.Crafting.Stations.First().custom ?? "",
                        new ConfigDescription("", null, customTableAttributes));

                    void TableConfigChanged(object o, EventArgs e)
                    {
                        if (piece.RequiredItems.Requirements.Count > 0)
                        {
                            switch (cfg.table.Value)
                            {
                                case CraftingTable.None:
                                    piecePrefab.m_craftingStation = null;
                                    break;
                                case CraftingTable.Custom:
                                    piecePrefab.m_craftingStation = ZNetScene.instance.GetPrefab(cfg.customTable.Value)
                                        ?.GetComponent<CraftingStation>();
                                    break;
                                default:
                                    piecePrefab.m_craftingStation = ZNetScene.instance
                                        .GetPrefab(
                                            ((InternalName)typeof(CraftingTable).GetMember(cfg.table.Value.ToString())[0]
                                                .GetCustomAttributes(typeof(InternalName)).First()).internalName)
                                        .GetComponent<CraftingStation>();
                                    break;
                            }
                        }

                        customTableAttributes.Browsable = cfg.table.Value == CraftingTable.Custom;
                        foreach (BuildPiece.ConfigurationManagerAttributes attributes in hideWhenNoneAttributes)
                        {
                            attributes.Browsable = cfg.table.Value != CraftingTable.None;
                        }

                        ReloadConfigDisplay();
                        BuildPiece.plugin.Config.Save();
                    }

                    cfg.table.SettingChanged += TableConfigChanged;
                    cfg.customTable.SettingChanged += TableConfigChanged;

                    BuildPiece.ConfigurationManagerAttributes tableLevelAttributes = new()
                        { Order = --order, Browsable = cfg.table.Value != CraftingTable.None };
                    
                    hideWhenNoneAttributes.Add(tableLevelAttributes);
                }

                ConfigEntry<string> itemConfig(string name, string value, string desc)
                {
                    BuildPiece.ConfigurationManagerAttributes attributes = new() { CustomDrawer = BuildPiece.DrawConfigTable, Order = --order, Category = localizedName };
                    return BuildPiece.config(englishName, name, value, new ConfigDescription(desc, null, attributes));
                }

                cfg.craft = itemConfig("Crafting Costs", new BuildPiece.SerializedRequirements(piece.RequiredItems.Requirements).ToString(), $"Item costs to craft {localizedName}");
                cfg.craft.SettingChanged += (_, _) =>
                {
                    if (ObjectDB.instance && ObjectDB.instance.GetItemPrefab("YmirRemains") != null)
                    {
                        Piece.Requirement[] requirements = BuildPiece.SerializedRequirements.toPieceReqs(new BuildPiece.SerializedRequirements(cfg.craft.Value));
                        piecePrefab.m_resources = requirements;
                        foreach (Piece instantiatedPiece in UnityEngine.Object.FindObjectsOfType<Piece>())
                        {
                            if (instantiatedPiece.m_name == pieceName)
                            {
                                instantiatedPiece.m_resources = requirements;
                            }
                        }
                    }
                };
            }
            
            foreach (var piece in ClonedPieces.Values)
            {
                if (piece.RecipeIsActive is { } enabledCfg)
                {
                    Piece piecePrefab = piece.Prefab.GetComponent<Piece>();
                    void ConfigChanged(object? o, EventArgs? e) => piecePrefab.m_enabled = (int)enabledCfg.BoxedValue != 0;
                    ConfigChanged(null, null);
                    enabledCfg.GetType().GetEvent(nameof(ConfigEntry<int>.SettingChanged)).AddEventHandler(enabledCfg, new EventHandler(ConfigChanged));
                }

                piece.InitializeNewRegisteredPiece(piece);
            }

            foreach (var piece in ClonedPieces.Values)
            {
                if (!BuildPiece.pieceConfigs.TryGetValue(piece, out BuildPiece.PieceConfig? cfg)) continue;
                piece.Prefab.GetComponent<Piece>().m_resources = BuildPiece.SerializedRequirements.toPieceReqs(new BuildPiece.SerializedRequirements(cfg.craft.Value));

                foreach (CraftingStationConfig station in piece.Crafting.Stations)
                {
                    switch ((cfg == null || piece.Crafting.Stations.Count > 0 ? station.Table : cfg.table.Value))
                    {
                        case CraftingTable.None:
                            piece.Prefab.GetComponent<Piece>().m_craftingStation = null;
                            break;
                        case CraftingTable.Custom
                            when ZNetScene.instance.GetPrefab(cfg == null || piece.Crafting.Stations.Count > 0
                                ? station.custom
                                : cfg.customTable.Value) is { } craftingTable:
                            piece.Prefab.GetComponent<Piece>().m_craftingStation =
                                craftingTable.GetComponent<CraftingStation>();
                            break;
                        case CraftingTable.Custom:
                            Debug.LogWarning($"Custom crafting station '{(cfg == null || piece.Crafting.Stations.Count > 0 ? station.custom : cfg.customTable.Value)}' does not exist");
                            break;
                        default:
                        {
                            if (cfg is { table.Value: CraftingTable.None })
                            {
                                piece.Prefab.GetComponent<Piece>().m_craftingStation = null;
                            }
                            else
                            {
                                piece.Prefab.GetComponent<Piece>().m_craftingStation = ZNetScene.instance
                                    .GetPrefab(((InternalName)typeof(CraftingTable).GetMember(
                                        (cfg == null || piece.Crafting.Stations.Count > 0 ? station.Table : cfg.table.Value)
                                        .ToString())[0].GetCustomAttributes(typeof(InternalName)).First()).internalName)
                                    .GetComponent<CraftingStation>();
                            }

                            break;
                        }
                    }
                }
            }
            if (SaveOnConfigSet)
            {
                BuildPiece.plugin.Config.SaveOnConfigSet = true;
                BuildPiece.plugin.Config.Save();
            }
        }
        
        BuildPiece.registeredPieces.AddRange(ClonedPieces.Values);
        Initialized = true;
    }
}