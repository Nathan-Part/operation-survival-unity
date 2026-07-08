using UnityEngine;
namespace Game.Runtime
{
    /// Claymore (web #, MINE_*): s'arme après 1 s, explose si un ennemi entre dans DETECT_R.
    public class ProximityMine : MonoBehaviour
    {
        const float DetectR = 2f, ExplodeR = 4f, Damage = 200f, ArmSeconds = 1f; // web: MINE_DETECT_R/EXPLODE_R/DAMAGE, armement 1s
        WaveManager waves; float armedAt;
        public static ProximityMine Place(Vector3 pos, WaveManager waves)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "Claymore";
            go.transform.position = pos; go.transform.localScale = Vector3.one * 0.3f;
            foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;
            var m = go.AddComponent<ProximityMine>(); m.waves = waves; m.armedAt = Time.time + ArmSeconds; return m;
        }
        void Update()
        {
            if (Time.time < armedAt) return;
            foreach (var e in waves.Alive)
                if (e != null && !e.IsDead && Vector3.Distance(transform.position, e.transform.position) <= DetectR)
                { AreaDamage.Apply(waves.Alive, transform.position, ExplodeR, Damage); Destroy(gameObject); return; }
        }
    }
}
