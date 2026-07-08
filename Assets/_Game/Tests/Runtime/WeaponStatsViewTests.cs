using Game.Data;
using NUnit.Framework;
using UnityEngine;

namespace Game.Runtime.Tests
{
    public class WeaponStatsViewTests
    {
        static WeaponData Make(float mag, float cooldown, float dmgMult, float recoil, float range)
        {
            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.mag = (int)mag;
            w.cooldown = cooldown;
            w.dmgMult = dmgMult;
            w.recoil = recoil;
            w.range = range;
            return w;
        }

        // ---------- Bornes 0..1 ----------

        [Test]
        public void Damage_BoundedZeroOne_EvenForHighDmgMult()
        {
            // web: sniper legendaire (phantom_sr) dmgMult=4.5 -> dmg=270 -> 270/200=1.35 avant clamp
            var w = Make(mag: 4, cooldown: 1.1f, dmgMult: 4.5f, recoil: 2.8f, range: 200f);
            float v = WeaponStatsView.Damage(w, 0);
            Assert.GreaterOrEqual(v, 0f);
            Assert.LessOrEqual(v, 1f);
            Assert.AreEqual(1f, v, 0.0001f); // doit clamper à 1, pas déborder
        }

        [Test]
        public void Damage_BoundedZeroOne_ForLowDmgMult()
        {
            var w = Make(mag: 32, cooldown: 0.055f, dmgMult: 0.1f, recoil: 0.5f, range: 30f);
            float v = WeaponStatsView.Damage(w, 0);
            Assert.GreaterOrEqual(v, 0f);
            Assert.LessOrEqual(v, 1f);
        }

        [Test]
        public void FireRate_BoundedZeroOne()
        {
            // cooldown très petit -> rpm énorme -> doit clamper à 1
            var w = Make(mag: 30, cooldown: 0.01f, dmgMult: 1f, recoil: 1f, range: 60f);
            float v = WeaponStatsView.FireRate(w, 0);
            Assert.GreaterOrEqual(v, 0f);
            Assert.LessOrEqual(v, 1f);
        }

        [Test]
        public void MagSize_BoundedZeroOne()
        {
            // LMG mag=50, avec upgrade -> encore plus haut -> doit clamper à 1
            var w = Make(mag: 50, cooldown: 0.09f, dmgMult: 0.75f, recoil: 1.4f, range: 75f);
            float v = WeaponStatsView.MagSize(w, 5);
            Assert.GreaterOrEqual(v, 0f);
            Assert.LessOrEqual(v, 1f);
        }

        [Test]
        public void Stability_BoundedZeroOne_EvenForHighRecoil()
        {
            // recoil > 3 (hypothétique) -> (3-recoil) négatif -> doit clamper à 0, jamais négatif
            var w = Make(mag: 5, cooldown: 0.85f, dmgMult: 3f, recoil: 4f, range: 160f);
            float v = WeaponStatsView.Stability(w, 0);
            Assert.GreaterOrEqual(v, 0f);
            Assert.LessOrEqual(v, 1f);
            Assert.AreEqual(0f, v, 0.0001f);
        }

        [Test]
        public void Range_BoundedZeroOne_AndFallsBackTo80WhenUnset()
        {
            // web: s.range ?? SHOT_RANGE (80) — armes de mêlée n'ont pas de "range" dans le JSON,
            // WeaponData.range vaut alors 0 (valeur par défaut float) : le repli doit s'appliquer.
            var melee = Make(mag: 1, cooldown: 0.55f, dmgMult: 3f, recoil: 0f, range: 0f);
            float v = WeaponStatsView.Range(melee);
            Assert.GreaterOrEqual(v, 0f);
            Assert.LessOrEqual(v, 1f);
            Assert.AreEqual(80f / 200f, v, 0.0001f);

            // portée énorme (sniper légendaire, 200m) -> doit clamper à 1
            var sniper = Make(mag: 4, cooldown: 1.1f, dmgMult: 4.5f, recoil: 2.8f, range: 200f);
            Assert.AreEqual(1f, WeaponStatsView.Range(sniper), 0.0001f);
        }

        // ---------- Monotonie avec les niveaux d'amélioration ----------

        [Test]
        public void Damage_IncreasesOrStaysWithUpgradeLevel()
        {
            var w = Make(mag: 12, cooldown: 0.12f, dmgMult: 1f, recoil: 1f, range: 60f);
            float prev = WeaponStatsView.Damage(w, 0);
            for (int level = 1; level <= 5; level++)
            {
                float v = WeaponStatsView.Damage(w, level);
                Assert.GreaterOrEqual(v, prev, $"niveau {level}");
                prev = v;
            }
        }

        [Test]
        public void FireRate_IncreasesOrStaysWithUpgradeLevel()
        {
            var w = Make(mag: 12, cooldown: 0.2f, dmgMult: 1f, recoil: 1f, range: 60f);
            float prev = WeaponStatsView.FireRate(w, 0);
            for (int level = 1; level <= 5; level++)
            {
                float v = WeaponStatsView.FireRate(w, level);
                Assert.GreaterOrEqual(v, prev, $"niveau {level}");
                prev = v;
            }
        }

        [Test]
        public void MagSize_IncreasesOrStaysWithUpgradeLevel()
        {
            var w = Make(mag: 12, cooldown: 0.12f, dmgMult: 1f, recoil: 1f, range: 60f);
            float prev = WeaponStatsView.MagSize(w, 0);
            for (int level = 1; level <= 5; level++)
            {
                float v = WeaponStatsView.MagSize(w, level);
                Assert.GreaterOrEqual(v, prev, $"niveau {level}");
                prev = v;
            }
        }

        [Test]
        public void Stability_IncreasesOrStaysWithUpgradeLevel()
        {
            var w = Make(mag: 12, cooldown: 0.12f, dmgMult: 1f, recoil: 1f, range: 60f);
            float prev = WeaponStatsView.Stability(w, 0);
            for (int level = 1; level <= 5; level++)
            {
                float v = WeaponStatsView.Stability(w, level);
                Assert.GreaterOrEqual(v, prev, $"niveau {level}");
                prev = v;
            }
        }

        // ---------- Valeurs affichées (Raw) : vérifie les formules exactes, pas juste les bornes ----------

        [Test]
        public void DamageRaw_MatchesWebFormula()
        {
            // web l.1483 : dmg = round(60 * dmgMult) ; arme par défaut dmgMult=1 -> 60
            var w = Make(mag: 12, cooldown: 0.12f, dmgMult: 1f, recoil: 1f, range: 60f);
            Assert.AreEqual(60, WeaponStatsView.DamageRaw(w, 0));
        }

        [Test]
        public void FireRateRaw_MatchesWebFormula()
        {
            // web l.1483 : rpm = round(60 / cooldown) ; cooldown=0.12 -> 500
            var w = Make(mag: 12, cooldown: 0.12f, dmgMult: 1f, recoil: 1f, range: 60f);
            Assert.AreEqual(500, WeaponStatsView.FireRateRaw(w, 0));
        }
    }
}
