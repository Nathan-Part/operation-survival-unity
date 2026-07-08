using System;
using Game.Data.Save;

namespace Game.Data.Rules
{
    /// Règles pures du panneau Réglages (parité web `setSetting`/`renderSettings`, main.js:739-753,
    /// 771-777) : snapping/clamping des sliders (le web s'appuie sur le pas natif de <input
    /// type="range">, reproduit ici à la main), validation de l'id de difficulté, bascule vue et
    /// toggles booléens. Pure : mutations du SaveData uniquement (jamais d'UI Toolkit ici) — le
    /// MonoBehaviour (SettingsPanelController) appelle ces méthodes puis meta.Persist() + Render().
    public static class SettingsRules
    {
        public const float SensitivityMin = 0.2f;
        public const float SensitivityMax = 3f;
        public const float SensitivityStep = 0.1f;

        public const float VolumeMin = 0f;
        public const float VolumeMax = 1f;
        public const float VolumeStep = 0.05f;

        static readonly string[] ValidDifficulties = { "facile", "normal", "difficile" };

        public static void SetSensitivity(SaveData save, float value) =>
            save.settings.sensitivity = Snap(value, SensitivityMin, SensitivityMax, SensitivityStep);

        public static void SetVolume(SaveData save, float value) =>
            save.settings.volume = Snap(value, VolumeMin, VolumeMax, VolumeStep);

        public static void ToggleView(SaveData save) =>
            save.settings.defaultView = save.settings.defaultView == "first" ? "third" : "first"; // web l.774

        /// false si l'id n'est pas un des 3 modes connus (facile/normal/difficile) — save inchangée.
        public static bool SetDifficulty(SaveData save, string id)
        {
            if (Array.IndexOf(ValidDifficulties, id) < 0) return false;
            save.settings.difficulty = id;
            return true;
        }

        public static void ToggleHardcore(SaveData save) => save.settings.hardcore = !save.settings.hardcore; // #94
        public static void ToggleRain(SaveData save) => save.settings.rain = !save.settings.rain; // #93

        // Arithmétique en decimal : un slider UI Toolkit qui rapporte 1.05f pour une valeur
        // "attendue" 1.05 a en réalité 1.0499999523... en float32, ce qui fait basculer un
        // Math.Round(8.5) en float du mauvais côté (8 au lieu de 9). La conversion float->decimal
        // du CLR arrondit au nombre de chiffres significatifs du float32 (~7), ce qui redonne des
        // valeurs "propres" (1.05m exact) avant la division — élimine cette classe de résidus.
        static float Snap(float value, float min, float max, float step)
        {
            decimal clamped = Math.Min((decimal)max, Math.Max((decimal)min, (decimal)value));
            decimal dMin = (decimal)min;
            decimal steps = Math.Round((clamped - dMin) / (decimal)step, MidpointRounding.AwayFromZero);
            decimal snapped = dMin + steps * (decimal)step;
            return (float)snapped;
        }
    }
}
