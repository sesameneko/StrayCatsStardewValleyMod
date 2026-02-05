//
// Copyright Imangi Studios, LLC, Copyright 2026. All rights reserved
//

using HarmonyLib;
using StardewValley;
using StardewValley.Characters;

namespace StrayCatsStardewValleyMod
{
    [HarmonyPatch(typeof(Pet), nameof(Pet.behaviorOnFarmerLocationEntry), typeof(GameLocation))]
    public static class BehaviorOnFarmerEntryPatch
    {
        public static bool Prefix(Pet __instance, GameLocation location)
        {
            if (__instance.modData.ContainsKey(Constants.ModDataKey))
            {
                return false; // modded cat, use custom behavior
            }
            return true; // default behavior
        }
    }

    [HarmonyPatch(typeof(Pet), nameof(Pet.behaviorOnFarmerLocationEntry), typeof(GameLocation), typeof(Farmer))]
    public static class BehaviorOnFarmerEntryPatch2
    {
        public static bool Prefix(Pet __instance, GameLocation location, Farmer who)
        {
            if (__instance.modData.ContainsKey(Constants.ModDataKey))
            {
                return false; // modded cat, use custom behavior
            }
            return true; // default behavior
        }
    }
}