using System;
using System.Collections.Generic;
using Game.Data;

namespace Game.Net
{
    /// Fabrique la config coop et les lookups à partir des données existantes (intermission,
    /// argent par kill, difficulté, hpMult par type d'ennemi) — sans dupliquer aucune valeur
    /// d'équilibrage. C'est LE seul fichier de Game.Net qui référence les ScriptableObjects :
    /// tout le reste du cœur reste engine-free (donc vérifiable hors moteur).
    public static class CoopConfigFactory
    {
        public static CoopSessionConfig FromDatabase(GameDatabase db)
        {
            var cfg = new CoopSessionConfig();
            if (db == null) return cfg;

            if (db.waveConfig != null)
                cfg.intermissionSeconds = db.waveConfig.intermissionSeconds;

            if (db.economyConfig != null)
            {
                cfg.perKillMoney = db.economyConfig.perKillMoney;
                cfg.baseEnemyHpMult = ResolveNormalHpMult(db);
            }

            return cfg;
        }

        /// typeId -> EnemyData.hpMult (1 par défaut si type inconnu), pour que le directeur de
        /// vagues calcule les PV exacts par type comme le solo, sans dépendre de Game.Data.
        public static Func<string, float> EnemyHpMultLookup(GameDatabase db)
        {
            var map = new Dictionary<string, float>();
            if (db != null && db.enemies != null)
                foreach (var e in db.enemies)
                    if (e != null && !string.IsNullOrEmpty(e.id))
                        map[e.id] = e.hpMult;
            return id => id != null && map.TryGetValue(id, out float m) ? m : 1f;
        }

        static float ResolveNormalHpMult(GameDatabase db)
        {
            var list = db.economyConfig != null ? db.economyConfig.difficulties : null;
            if (list != null)
                foreach (var d in list)
                    if (d != null && d.id == "normal")
                        return d.enemyHpMult;
            return 1f;
        }
    }
}
