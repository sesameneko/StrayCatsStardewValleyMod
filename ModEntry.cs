using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.GameData.Pets;

namespace StrayCatsStardewValleyMod
{
    public class ModEntry : StardewModdingAPI.Mod
    {
        protected int catSpawnIntervalGameMinutes = 25;
        protected int lastCatSpawnTimeGameMinute = 0;
        protected List<NPC> temporaryCats = new List<NPC>();

        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += this.OnTick;
            helper.Events.GameLoop.DayEnding += this.OnDayEnd;
            helper.Events.GameLoop.DayStarted += this.OnDayStart;
            helper.ConsoleCommands.Add(
                "wildcat", 
                "spawns a cat outside the viewport", 
                SpawnCatConsoleCommand);
            
            Monitor.Log($"Night Cat Mod initialized");
        }

        private void SpawnCatConsoleCommand(string arg1, string[] arg2)
        {
            if (int.TryParse(arg1, out int count))
            {
                for (int i = 0; i < count; i++)
                {
                    SpawnRandomCatOutsideViewport(Game1.player);
                }
            }
            else
            {
                SpawnRandomCatOutsideViewport(Game1.player);
            }
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            // testing: print button presses to the console window
            //this.Monitor.Log($"Night Cat Mod says: {Game1.player.Name} pressed {e.Button}.", LogLevel.Debug);
        }

        private void OnTick(object? sender, UpdateTickedEventArgs updateTickedEventArgs)
        {
            if (!Game1.IsMasterGame)
                return;
            PollNightCats();
        }

        private void PollNightCats()
        {
            if (!Game1.IsMasterGame)
                return;
            if (Game1.timeOfDay < lastCatSpawnTimeGameMinute)
            {
                lastCatSpawnTimeGameMinute = 0;
            }
            bool isNight = Game1.timeOfDay > 1400;
            bool shouldSpawn = isNight && Game1.timeOfDay - lastCatSpawnTimeGameMinute > catSpawnIntervalGameMinutes;
            if (shouldSpawn)
            {
                List<Farmer> farmers = Game1.getAllFarmers().ToList();
                if (farmers.Count > 0)
                {
                    var farmer = farmers[Random.Shared.Next(0, farmers.Count)];
                    SpawnRandomCatOutsideViewport(farmer);
                }
                else
                {
                    Monitor.Log("Failed to spawn catL list of farmers is empty", LogLevel.Warn);
                    lastCatSpawnTimeGameMinute = Game1.timeOfDay;
                }
            }
        }

        private void SpawnRandomCatOutsideViewport(Farmer farmer)
        {
            if (farmer.currentLocation.IsOutdoors == false) return;
            var location = farmer.currentLocation;
            for (int index = 0; index < 15; ++index) // try 15 times
            {
                var randomTile = location.getRandomTile();
                if (Utility.isOnScreen(Utility.Vector2ToPoint(randomTile), 64, (GameLocation)farmer.currentLocation))
                    randomTile.X -= (float)(Game1.viewport.Width / 64);
                if (location.CanItemBePlacedHere(randomTile) && location.CanSpawnCharacterHere(randomTile))
                {
                    SpawnRandomCat(location,randomTile.ToPoint().X,randomTile.ToPoint().Y);
                    break;
                }
            }
        }

        private void SpawnRandomCat(GameLocation location, int tileX, int tileY)
        {
            if (!Game1.IsMasterGame) 
                return;
            if (!Pet.TryGetData(Pet.type_cat, out PetData catsData)) 
                return;
            int catBreedsCount = catsData.Breeds.Count;
            var spawnedCat = new Pet(tileX, tileY,
                catsData.Breeds[Random.Shared.Next(0, catBreedsCount)].Id, Pet.type_cat)
            {
                Name = "Wild Cat",
                CurrentBehavior = Random.Shared.Next(0,2) > 0 ? Pet.behavior_Walk : Pet.behavior_SitDown,
                isSleeping = { false },
                hideFromAnimalSocialMenu = { true },
                currentLocation = location,
                modData = { { "isStrayCat", "true" } }
            };
            location.addCharacter(spawnedCat);
            spawnedCat.setTilePosition(new Point(tileX, tileY));
            //location.characters.Add(spawnedCat); should be handled by constructor
            temporaryCats.Add(spawnedCat);
            spawnedCat.OnNewBehavior();

            lastCatSpawnTimeGameMinute = Game1.timeOfDay;
            
            // debug
            Monitor.Log($"Spawned {spawnedCat.petType}, breed:{spawnedCat.whichBreed}, name:{spawnedCat.Name} in:{location.Name}",
                LogLevel.Debug);
        }


        private void OnDayStart(object? sender, DayStartedEventArgs e)
        {
            RemoveTemporaryCats();
        }

        private void OnDayEnd(object? sender, DayEndingEventArgs e)
        {
            RemoveTemporaryCats();
        }

        protected override void Dispose(bool disposing)
        {
            RemoveTemporaryCats();
        }

        private void RemoveTemporaryCats()
        {
            if (!Game1.IsMasterGame)
                return;
            
            Monitor.Log("Removing temporary cats...");

            void RemoveIfTemporaryCat(NPC npc, int index)
            {
                if (!npc.modData.ContainsKey("isStrayCat")) 
                    return;
                
                npc.currentLocation.characters.RemoveAt(index);
                npc.Removed();
                Monitor.Log($"Removing cat: {npc.Name} <{npc.id}>");
            }

            void RemoveAllTemporaryCats(GameLocation gameLocation)
            {
                for (int i = gameLocation.characters.Count - 1; i >= 0; i--)
                {
                    NPC npc = gameLocation.characters[i];
                    RemoveIfTemporaryCat(npc, i);
                }
            }

            foreach (GameLocation gameLocation in Game1.locations)
            {
                RemoveAllTemporaryCats(gameLocation);
            }
            
            temporaryCats.Clear();
        }
        
        
    }
}