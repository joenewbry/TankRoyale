namespace TankRoyale.Gameplay
{
    public static class DebugVisualSettings
    {
        public static bool ShowMenu;
        public static bool ShowColliderBounds;
        public static bool ShowRaycasts;
        public static bool ShowProjectileArc = true;
        public static bool ShowTrajectoryLine = true;
        public static bool ShowBounceNormals = true;
        public static bool Wireframe;
        public static bool DisableShadows;

        public static void ResetDefaults()
        {
            ShowMenu = false;
            ShowColliderBounds = false;
            ShowRaycasts = false;
            ShowProjectileArc = true;
            ShowTrajectoryLine = true;
            ShowBounceNormals = true;
            Wireframe = false;
            DisableShadows = false;
        }
    }
}
