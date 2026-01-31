using StardewValley;
using StardewValley.Characters;

namespace BenStardewValleyMod
{
    public class WildCat : Pet
    {
        public WildCat(int tileX, int tileY, string breed) : base(tileX,tileY,breed,type_cat)
        {
            // pass through constructor
        }

        public override void behaviorOnLocalFarmerLocationEntry(GameLocation location)
        {
            // override the farmer entry behavior so that wild cats do not warp to the house
        }

        public override void behaviorOnFarmerLocationEntry(GameLocation location, Farmer who)
        {
            // override the farmer entry behavior so that wild cats do not warp to the house
        }
    }
}