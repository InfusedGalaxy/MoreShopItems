using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Photon.Pun;
using BepInEx.Bootstrap;
using Unity.VisualScripting.Antlr3.Runtime.Misc;

namespace MoreShopItems;

// 39 Upgrade spots
// 24 Health pack spots
// 66 other items

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(REPOLib.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("bulletbot.moreupgrades", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin {
    private readonly Harmony _harmony = new Harmony("MoreShopItems");
    internal static Plugin Instance { get; private set; }
    internal static new ManualLogSource Logger { get; private set; }
    public static GameObject CustomSodaShelf;
    internal int itemConsumablesAmount;

    // Spawns
    internal ConfigEntry<int> itemtargetSpawnAmount;
    internal ConfigEntry<int> itemUpgradesAmount;
    internal ConfigEntry<int> itemHealthPacksAmount;

    // Items Purchasing
    internal ConfigEntry<int> customMaxAmountInShop;
    internal ConfigEntry<int> customMaxPurchaseAmount;
    internal ConfigEntry<bool> maxPurchaseOverride;

    // Upgrades
    internal ConfigEntry<int> upgradeMaxInShopAndAmount;
    internal ConfigEntry<bool> upgradeMaxPurchase;

    // Weapons
    internal ConfigEntry<int> weaponMaxInShopAndAmount;
    internal ConfigEntry<bool> weaponMaxPurchase;

    // Items
    internal ConfigEntry<int> itemMaxInShopAndAmount;
    internal ConfigEntry<bool> itemMaxPurchase;

    // Modded Upgrades
    internal ConfigEntry<int> moddedUpgradeMaxInShopAndAmount;
    internal ConfigEntry<bool> moddedUpgradeMaxPurchase;
    internal ConfigEntry<bool> moddedUpgradesOverride;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } // if

        Logger = base.Logger;

        // Spawning variables
        itemtargetSpawnAmount = Instance.Config.Bind<int>("General", "Total Shop Item Count", 129, new ConfigDescription("The maximum number of items the shop will spawn. (Recommended value = 129)", new AcceptableValueRange<int>(0, 129)));
        itemUpgradesAmount = Instance.Config.Bind<int>("General", "Upgrade Item Count", 39, new ConfigDescription("The maximum number of upgrades the shop will spawn. (Recommended value = 39)", new AcceptableValueRange<int>(0, 39)));
        itemHealthPacksAmount = Instance.Config.Bind<int>("General", "Health Pack Item Count", 24, new ConfigDescription("The maximum number of health pacls the shop will spawn. (Recommended value = 24)", new AcceptableValueRange<int>(0,24)));

        // Upgrade variables
        upgradeMaxInShopAndAmount = Instance.Config.Bind<int>("Upgrades", "Max Amount In Shop", 5, new ConfigDescription("Maximum amount able to spawn in shop. (Recommended value = 5)", new AcceptableValueRange<int>(0, 10)));
        upgradeMaxPurchase = Instance.Config.Bind<bool>("Upgrades", "Max Purchase", false, new ConfigDescription("Limits the amount of upgrades that can be bought. (Recommended value = False)"));

        // Weapon variables
        weaponMaxInShopAndAmount = Instance.Config.Bind<int>("Weapons", "Max Amount In Shop", 5, new ConfigDescription("Maximum amount able to spawn in shop. (Recommended value = 5)", new AcceptableValueRange<int>(0, 10)));
        weaponMaxPurchase = Instance.Config.Bind<bool>("Weapons", "Max Purchase", false, new ConfigDescription("Limits the amount of weapons that can be bought. (Recommended value = False)"));

        // Item variables
        itemMaxInShopAndAmount = Instance.Config.Bind<int>("Items", "Max Amount In Shop", 5, new ConfigDescription("Maximum amount able to spawn in shop. (Recommended value = 5)", new AcceptableValueRange<int>(0, 10)));
        itemMaxPurchase = Instance.Config.Bind<bool>("Items", "Max Purchase", false, new ConfigDescription("Limits the amount of weapons that can be bought. (Recommended value = False)"));

        // Modded variables
        moddedUpgradeMaxInShopAndAmount = Instance.Config.Bind<int>("Modded", "Max Amount In Shop", 5, new ConfigDescription("Maximum amount able to spawn in shop. (Recommended value = 5)", new AcceptableValueRange<int>(0, 10)));
        moddedUpgradeMaxPurchase = Instance.Config.Bind<bool>("Modded", "Max Purchase", false, new ConfigDescription("Limits the amount of upgrades that can be bought. (Recommended value = False)"));
        moddedUpgradesOverride = Instance.Config.Bind<bool>("Modded", "Modded Upgrade Override", true, new ConfigDescription("If the item is added through BULLETBOT's MoreUpgradesLib, this will override the amount to spawn in the shop if max purchase is false."));



        customMaxAmountInShop = Instance.Config.Bind<int>("Items", "Max Amount In Shop", 10, new ConfigDescription("Maximum amount able to spawn in shop.", new AcceptableValueRange<int>(0, 100)));
        customMaxPurchaseAmount = Instance.Config.Bind<int>("Items", "Max Purchase Amount", 100, new ConfigDescription("Maximum amount to be bought.", new AcceptableValueRange<int>(0, 100)));
        maxPurchaseOverride = Instance.Config.Bind<bool>("Items", "Max Purchase Override", true, new ConfigDescription("Enable to remove the maximum purchase amount. (Recommended value = True)"));


        itemConsumablesAmount = itemHealthPacksAmount.Value + itemUpgradesAmount.Value;

        AssetBundle assetBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Instance.Info.Location), "moreshopitems_assets.file"));
        CustomSodaShelf = assetBundle.LoadAsset<GameObject>("custom_soda_shelf");

        REPOLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(CustomSodaShelf);

        _harmony.PatchAll(typeof(ShopManagerPatch));   
        _harmony.PatchAll(typeof(CustomShelfPatch));

        Logger.LogInfo($"\n __ __  ___  ____  ___   ___  _   _  ___  ____   _____  _____  ___  __ __  ___ \n" +
                       $"|  V  || _ || __ || __| | __|| |_| || _ || __ | |_   _||_   _|| __||  V  || __|\n" +
                       $"| |V| || | ||   < | __| |__ ||  _  || | ||  __|  _| |_   | |  | __|| |V| ||__ |\n" +
                       $"|_| |_||___||_||_||___| |___||_| |_||___||_|    |_____|  |_|  |___||_| |_||___| v{MyPluginInfo.PLUGIN_VERSION}\n");
    } // Awake
} // Plugin

[HarmonyPatch(typeof(ShopManager), "Awake")]
internal static class ShopManagerPatch {
    private static readonly AccessTools.FieldRef<ShopManager, int> itemSpawnTargetAmount_ref = AccessTools.FieldRefAccess<ShopManager, int>("itemSpawnTargetAmount");
    private static readonly AccessTools.FieldRef<ShopManager, int> itemConsumablesAmount_ref = AccessTools.FieldRefAccess<ShopManager, int>("itemConsumablesAmount");
    private static readonly AccessTools.FieldRef<ShopManager, int> itemUpgradesAmount_ref = AccessTools.FieldRefAccess<ShopManager, int>("itemUpgradesAmount");
    private static readonly AccessTools.FieldRef<ShopManager, int> itemHealthPacksAmount_ref = AccessTools.FieldRefAccess<ShopManager, int>("itemHealthPacksAmount");

    private static void Postfix(ShopManager __instance) {

        ref int targetRef = ref itemSpawnTargetAmount_ref.Invoke(__instance);
        ref int consumablesRef = ref itemConsumablesAmount_ref.Invoke(__instance);
        ref int upgradesRef = ref itemUpgradesAmount_ref.Invoke(__instance);
        ref int healthPacksRef = ref itemHealthPacksAmount_ref.Invoke(__instance);

        targetRef = Plugin.Instance.itemtargetSpawnAmount.Value;
        consumablesRef = Plugin.Instance.itemConsumablesAmount;
        upgradesRef = Plugin.Instance.itemUpgradesAmount.Value;
        healthPacksRef = Plugin.Instance.itemHealthPacksAmount.Value; 
    }
} // ShopManagerPatch

[HarmonyPatch(typeof(ShopManager), "ShopInitialize")]
internal static class CustomShelfPatch {

    private static GameObject shelf;

    internal static bool isMoreUpgrades = false;

    static void Prefix() {
        AdjustItemStats();

        if (RunManager.instance.levelCurrent.ResourcePath == "Shop") {
            GameObject sodaShelf = GameObject.Find("Soda Shelf");
            GameObject moduleBottom = GameObject.Find("Module Switch BOT");
            
            if (sodaShelf != null && !moduleBottom.GetComponent<ModulePropSwitch>().ConnectedParent.activeSelf) {
                if (!SemiFunc.IsMultiplayer()) {
                    shelf = UnityEngine.Object.Instantiate<GameObject>(Plugin.CustomSodaShelf, sodaShelf.transform.position, sodaShelf.transform.rotation, moduleBottom.transform);
                } else {
                    // Check if the player is host to prevent every player from spawning the shelf.
                    if (!SemiFunc.IsMasterClient()) {
                        sodaShelf.SetActive(false);
                        return;
                    } // if
                    shelf = PhotonNetwork.Instantiate(((UnityEngine.Object)Plugin.CustomSodaShelf).name, sodaShelf.transform.position, sodaShelf.transform.rotation, (byte)0, (object[])null);
                    SetParent(moduleBottom.transform);
                } // if
                sodaShelf.SetActive(false);
            } else {
                GameObject magazineShelf = GameObject.Find("Shop Magazine Stand (1)");
                GameObject magazineShelf_other = GameObject.Find("Shop Magazine Stand");
                GameObject moduleTop = GameObject.Find("Module Switch (1) top");

                if (magazineShelf != null && !moduleTop.GetComponent<ModulePropSwitch>().ConnectedParent.activeSelf) {
                    if (!SemiFunc.IsMultiplayer()) {
                        shelf = UnityEngine.Object.Instantiate<GameObject>(Plugin.CustomSodaShelf, magazineShelf.transform.position, magazineShelf.transform.rotation * Quaternion.Euler(0,90,0), moduleTop.transform.parent);
                    } else {
                        // Check if the player is host to prevent every player from spawning the shelf.
                        if (!SemiFunc.IsMasterClient()) {
                            magazineShelf.SetActive(false);
                            if (magazineShelf_other != null)
                                magazineShelf_other.SetActive(false);
                            return;
                        } // if
                        shelf = PhotonNetwork.Instantiate(((UnityEngine.Object)Plugin.CustomSodaShelf).name, magazineShelf.transform.position, magazineShelf.transform.rotation * Quaternion.Euler(0,90,0), (byte)0, (object[])null);
                        SetParent(moduleTop.transform);
                    } // if           
                    magazineShelf.SetActive(false);
                    if (magazineShelf_other != null)
                        magazineShelf_other.SetActive(false);
                } else {
                    GameObject moduleLeft = GameObject.Find("Module Switch (2) left");
                    GameObject candyShelf = GameObject.Find("Candy Shelf");

                    if (moduleLeft != null && !moduleLeft.GetComponent<ModulePropSwitch>().ConnectedParent.activeSelf) {
                        if (!SemiFunc.IsMultiplayer()) {
                            shelf = UnityEngine.Object.Instantiate<GameObject>(Plugin.CustomSodaShelf, moduleLeft.transform.position + (moduleLeft.transform.right * 0.5f) - (moduleLeft.transform.forward * 0.8f), moduleLeft.transform.rotation * Quaternion.Euler(0,180,0), moduleTop.transform.parent);
                        } else {
                            // Check if the player is host to prevent every player from spawning the shelf.
                            if (!SemiFunc.IsMasterClient()) {
                                if (candyShelf != null)
                                    candyShelf.SetActive(false);
                                return;
                            } // if
                            shelf = PhotonNetwork.Instantiate(((UnityEngine.Object)Plugin.CustomSodaShelf).name, moduleLeft.transform.position + (moduleLeft.transform.right * 0.5f) - (moduleLeft.transform.forward * 0.8f), moduleLeft.transform.rotation * Quaternion.Euler(0,180,0), (byte)0, (object[])null);
                            SetParent(moduleLeft.transform);
                        } // if           
                        if (candyShelf != null)
                            candyShelf.SetActive(false);
                    } else {
                        Plugin.Logger.LogInfo("Edge case found. Temporarily preventing spawn of custom shelf.");
                        return;
                    } // if
                } // if
            } // if
        } else {
            return;
        } // if
      
        Plugin.Logger.LogInfo("Successfully added object(s)!");
    } // Prefix

    /// <summary>
    /// Adjusts the items' stats to allow more to spawn, buy, and use.
    /// (Does not change anything with the carts cuz idk how they work and spawn volumes with the truck)
    /// </summary>
    internal static void AdjustItemStats() {
        if (StatsManager.instance == null)
            return;

        if (!SemiFunc.IsMasterClient() && SemiFunc.IsMultiplayer())
            return;

        foreach(Item item in StatsManager.instance.itemDictionary.Values) {
            switch(item.itemType) {
                case SemiFunc.itemType.item_upgrade:
                    if (MoreUpgradesMOD.isLoaded()) {
                        // Check if item contains MoreUpgrades prefix AND if override is true AND if the item itself has no max purchase
                        if (item.itemAssetName.Contains("Modded Item Upgrade Player") && Plugin.Instance.moddedUpgradesOverride.Value && !item.maxPurchase) {
                            item.maxAmount = item.maxAmountInShop = Plugin.Instance.moddedUpgradeMaxInShopAndAmount.Value;
                        } // if
                        continue;
                    } // if

                    if (item.itemAssetName == "Item Upgrade Map Player Count") {
                        continue;
                    } // if

                    if (Plugin.Instance.upgradeMaxPurchase.Value) {
                        item.maxAmountInShop = Plugin.Instance.upgradeMaxInShopAndAmount.Value - SemiFunc.StatGetItemsPurchased(item.itemAssetName);
                    } else {
                        item.maxAmountInShop = Plugin.Instance.upgradeMaxInShopAndAmount.Value;
                    } // if

                    item.maxAmount = item.maxAmountInShop = Plugin.Instance.upgradeMaxInShopAndAmount.Value;

                    if (Plugin.Instance.upgradeMaxPurchase.Value) {

                    } else {
                        
                    }
                break;
            } // switch
            
        } // foreach


        foreach (Item obj in StatsManager.instance.itemDictionary.Values) {

            if (obj.itemType != SemiFunc.itemType.cart && obj.itemType != SemiFunc.itemType.pocket_cart)
                
                
                // Does this item have a max purchase limit
                obj.maxPurchase = !Plugin.Instance.maxPurchaseOverride.Value;
                obj.maxPurchaseAmount = Plugin.Instance.customMaxPurchaseAmount.Value;
                // Maximum number of items of this type
                obj.maxAmount = Plugin.Instance.customMaxAmountInShop.Value;
        } // foreach
    }
    

    internal static PluginInfo CheckForMod(string GUID) {
        if (Chainloader.PluginInfos.ContainsKey(GUID)) {
            return Chainloader.PluginInfos[GUID];
        } else {
            return null;
        } // if
    } // CheckForMod

    [PunRPC]
    public static void SetParent(Transform parent) {
        shelf.transform.SetParent(parent);
    } // SetParent

} // LeveleneratorPatch
internal class MoreUpgradesMOD {
    internal const string modGUID = "bulletbot.moreupgrades";

    internal static bool isLoaded() {
        return Chainloader.PluginInfos.ContainsKey(modGUID);
    } // isLoaded
}

