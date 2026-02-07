namespace StrayCatsStardewValleyMod
{
    public sealed class ModConfig
    {
        public int FirstCatAppearTime24Hr { get; set; } = 0600;
        public int CatAppearIntervalInGameMinutes { get; set; } = 25;
        public bool TestMode { get; set; } = false;
    }
}