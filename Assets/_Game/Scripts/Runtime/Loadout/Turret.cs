using UnityEngine;
namespace Game.Runtime
{
    /// Tourelle auto (web TURRET_*): tire sur l'ennemi le plus proche dans 15 m toutes les 0.5 s, 12 dégâts, 20 s de vie.
    public class Turret : MonoBehaviour
    {
        const float Range = 15f, FireCd = 0.5f, Dmg = 12f, Duration = 20f; // web: TURRET_RANGE/FIRE_CD/DMG/DURATION
        WaveManager waves; float diesAt, nextFire;
        public static Turret Deploy(Vector3 pos, WaveManager waves)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name = "Turret";
            go.transform.position = pos + Vector3.up * 0.5f; go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;
            var t = go.AddComponent<Turret>(); t.waves = waves; t.diesAt = Time.time + Duration; return t;
        }
        void Update()
        {
            if (Time.time >= diesAt) { Destroy(gameObject); return; }
            if (Time.time < nextFire) return;
            EnemyHealth best = null; float bestD = Range;
            foreach (var e in waves.Alive)
            { if (e == null || e.IsDead) continue; float d = Vector3.Distance(transform.position, e.transform.position); if (d <= bestD) { bestD = d; best = e; } }
            if (best != null) { best.ApplyDamage(Mathf.RoundToInt(Dmg)); nextFire = Time.time + FireCd; }
        }
    }
}
