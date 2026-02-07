using HarmonyLib;
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
        public static ModEntry Instance { get; private set; } = null!;

        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (Instance == null) return;
            if (!Instance.debugLogging) return;
            Instance.Monitor.Log(message, level);
        }
        
        private bool debugLogging = false;
        protected int firstCatAppearTime = 1400;
        protected int catSpawnIntervalGameMinutes = 25;
        protected bool testMode = false;
        protected int lastCatSpawnTimeGameMinute = 0;
        protected List<NPC> temporaryCats = new List<NPC>();

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += this.OnTick;
            helper.Events.GameLoop.SaveCreating += this.OnSave;
            helper.Events.GameLoop.DayEnding += this.OnDayEnd;
            helper.Events.GameLoop.DayStarted += this.OnDayStart;
            helper.ConsoleCommands.Add(
                "wildcat", 
                "spawns a cat", 
                SpawnCatConsoleCommand);
            
            var config = helper.ReadConfig<ModConfig>();
            firstCatAppearTime = config.FirstCatAppearTime24Hr;
            catSpawnIntervalGameMinutes = config.CatAppearIntervalInGameMinutes;
            testMode = config.TestMode;
            debugLogging |= testMode;
            
            ApplyPatches();
            
            Log($"Stray Cats Mod initialized");
            Log($"Stray Cats Mod Config: firstCatAppearTime={firstCatAppearTime}, catSpawnIntervalGameMinutes={catSpawnIntervalGameMinutes}, testMode={testMode}");
        }

        private void OnSave(object? sender, SaveCreatingEventArgs e)
        {
            // prevent temporary cats from being saved
            RemoveTemporaryCats();
        }

        private void ApplyPatches()
        {
            // patch to fix bug where stray cats teleport into the house
            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.Patch(
                original: AccessTools.Method(typeof(Pet), nameof(Pet.warpToFarmHouse)),
                prefix: new HarmonyMethod(typeof(PetOverrides), nameof(PetOverrides.Prefix_warpToFarmHouse))
            );
        }

        private void SpawnCatConsoleCommand(string arg1, string[] arg2)
        {
            
            if (arg2.Length > 0 && int.TryParse(arg2[0], out int count))
            {
                // spawn many cats by passing in a number
                for (int i = 0; i < count; i++) 
                    SpawnRandomCatOutsideViewport(Game1.player);
            }
            else
            {
                // spawn cat next to player
                SpawnRandomCat(Game1.player.currentLocation,Game1.player.TilePoint.X+1,Game1.player.TilePoint.Y);
            }
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;
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
            bool isTimeForCats = Game1.timeOfDay > firstCatAppearTime;
            bool shouldSpawn = isTimeForCats && Game1.timeOfDay - lastCatSpawnTimeGameMinute > catSpawnIntervalGameMinutes;
            if (shouldSpawn)
            {
                List<Farmer> farmers = Game1.getAllFarmers().ToList();
                if (farmers.Count > 0)
                {
                    // select random farmer
                    var farmer = farmers[Random.Shared.Next(0, farmers.Count)];
                    // spawn a cat near that farmer
                    SpawnRandomCatOutsideViewport(farmer);
                }
                else
                {
                    Log("Failed to spawn cat: list of farmers is empty", LogLevel.Warn);
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
                var randomTile = testMode ? farmer.Tile + Vector2.UnitX * 2 : location.getRandomTile();
                bool isOnScreen = Utility.isOnScreen(Utility.Vector2ToPoint(randomTile), 64, farmer.currentLocation); 
                if (!testMode && isOnScreen)
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
                modData = { { Constants.ModDataKey, "true" } }
            };
            location.addCharacter(spawnedCat);
            spawnedCat.setTilePosition(new Point(tileX, tileY));
            //location.characters.Add(spawnedCat); should be handled by constructor
            temporaryCats.Add(spawnedCat);
            spawnedCat.OnNewBehavior();

            lastCatSpawnTimeGameMinute = Game1.timeOfDay;
            
            // debug
            Log($"Spawned {spawnedCat.petType}, breed:{spawnedCat.whichBreed}, name:{spawnedCat.Name} in:{location.Name}",
                LogLevel.Debug);
        }


        private void OnDayStart(object? sender, DayStartedEventArgs e)
        {
            // extra protection against leftover
            RemoveTemporaryCats();
        }

        private void OnDayEnd(object? sender, DayEndingEventArgs e)
        {
            // extra protection against leftover
            RemoveTemporaryCats();
        }

        private void RemoveTemporaryCats()
        {
            if (!Game1.IsMasterGame)
                return;
            
            Log("Removing temporary cats...");

            void RemoveIfTemporaryCat(NPC npc, int index)
            {
                if (!npc.modData.ContainsKey(Constants.ModDataKey)) 
                    return;
                
                npc.currentLocation.characters.RemoveAt(index);
                Log($"Removing cat: {npc.Name} <{npc.id}>");
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