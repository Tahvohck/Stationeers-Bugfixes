using BepInEx;
using HarmonyLib;
using Objects.Electrical;
using System;
using System.Reflection;

namespace Tahvohck_Bugfixes
{
    [BepInPlugin("THVBugfixes", "Tahvohck's Bugfixes", "0.1")]
    public class Plugin : BaseUnityPlugin
    {
        private static int _itemsToCreateFertilizer;
        public static int Composter_itemsToCreateFertilizer
        {
            get { return _itemsToCreateFertilizer; }
        }

        internal static BepInEx.Logging.ManualLogSource PluginLogger;

        private void Awake()
        {
            PluginLogger = Logger;
            new Harmony(nameof(Tahvohck_Bugfixes)).PatchAll(typeof(Patches));
            _itemsToCreateFertilizer = (int)typeof(AdvancedComposter)
                .GetField("_itemsToCreateFertilizer", BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(null);
        }
    }

    
    public class Patches
    {

        [HarmonyPatch(typeof(AdvancedComposter), "CreateOutput")]
        [HarmonyPostfix]
        internal static void ClearExcessMaterialsAfterCreate(AdvancedComposter __instance)
        {
            float sum = __instance.BiomassQuantity + __instance.DecayFoodQuantity + __instance.NormalFoodQuantity;
            if (sum <= __instance.UnprocessedAmount) return;

            float overflowRatio = (float)__instance.UnprocessedAmount / sum;
            Plugin.PluginLogger.LogInfo(string.Format("Fixing up composter state. Ratio: {0:F4}", overflowRatio));
            __instance.BiomassQuantity      = (int)Math.Round(__instance.BiomassQuantity    * overflowRatio);
            __instance.DecayFoodQuantity    = (int)Math.Round(__instance.DecayFoodQuantity  * overflowRatio);
            __instance.NormalFoodQuantity   = (int)Math.Round(__instance.NormalFoodQuantity * overflowRatio);
        }

        [HarmonyPatch(typeof(AdvancedComposter), nameof(AdvancedComposter.RemoveQuantity))]
        [HarmonyPrefix]
        internal static bool FixComposterConsumptionRate(ref int quantity, float ratio)
        {
            float value = Plugin.Composter_itemsToCreateFertilizer * ratio;
            quantity -= (int)Math.Round(value);
            quantity = Math.Max(0, quantity);
            return false;
        }
    }
}
