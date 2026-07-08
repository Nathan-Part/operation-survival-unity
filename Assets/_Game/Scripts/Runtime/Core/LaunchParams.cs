namespace Game.Runtime
{
    /// Paramètres de lancement one-shot posés par l'accueil (phase 5), lus puis reset par SliceBootstrap.
    public static class LaunchParams
    {
        public static bool Training;
        public static void Reset() { Training = false; }
    }
}
