namespace TankRoyale.Gameplay
{
    public static class TargetCactusSettings
    {
        public static bool BalanceModeEnabled;
        public static int HitsToDestroy = 5;
        public static float RespawnSeconds = 2f;

        public static void ResetDefaults()
        {
            BalanceModeEnabled = false;
            HitsToDestroy = 5;
            RespawnSeconds = 2f;
        }
    }
}
