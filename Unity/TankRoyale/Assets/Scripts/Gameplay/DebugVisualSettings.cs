namespace TankRoyale.Gameplay
{
    public static class DebugVisualSettings
    {
        public static bool ShowMenu;
        public static bool ShowColliderBounds;
        public static bool ShowRaycasts;
        public static bool ShowProjectileArc;
        public static bool ShowTrajectoryLine;
        public static bool ShowBounceNormals;
        public static bool Wireframe = true;
        public static bool DisableShadows;

        public static void ResetDefaults()
        {
            ShowMenu = false;
            ShowColliderBounds = false;
            ShowRaycasts = false;
            ShowProjectileArc = false;
            ShowTrajectoryLine = false;
            ShowBounceNormals = false;
            Wireframe = true;
            DisableShadows = false;
        }
    }
}
