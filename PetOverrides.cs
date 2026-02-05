//
// Copyright Imangi Studios, LLC, Copyright 2026. All rights reserved
//

using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;

namespace StrayCatsStardewValleyMod
{
    public static class PetOverrides
    {
        public static bool Prefix_warpToFarmHouse(Pet __instance, Farmer who)
        {
            // ModEntry.Log("[StrayCatsMod] warp to farmhouse prefix invoked");
            if (__instance.modData.ContainsKey(Constants.ModDataKey))
            {
                // ModEntry.Log("[StrayCatsMod] abort WarpToFarmHouse, stray cats do not go in the house");
                return false;
            }
            return true;
        }
    }
}