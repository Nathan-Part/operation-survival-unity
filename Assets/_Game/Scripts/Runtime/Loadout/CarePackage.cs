using UnityEngine;
namespace Game.Runtime
{
    /// Caisse de ravitaillement (web CARE_PACKAGE_*): ramassage 1.8 m, récompense aléatoire.
    public class CarePackage : MonoBehaviour
    {
        const float PickupR = 1.8f; const int Money = 150;
        Transform player; MetaProgress meta; PlayerHealth health; int seed;
        public static CarePackage Spawn(Vector3 pos, Transform player, MetaProgress meta, PlayerHealth health, int seed)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "CarePackage";
            go.transform.position = pos + Vector3.up * 0.25f; go.transform.localScale = Vector3.one * 0.5f;
            foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;
            var p = go.AddComponent<CarePackage>(); p.player = player; p.meta = meta; p.health = health; p.seed = seed; return p;
        }
        void Update()
        {
            if (player == null) return;
            if (Vector3.Distance(transform.position, player.position) > PickupR) return;
            // récompense déterministe par seed (money par défaut ; les bundles grenades/medkit ajoutent au stock)
            int pick = Mathf.Abs(seed) % 3;
            if (pick == 0) { meta.Data.money += Money; }
            else if (pick == 1) { foreach (var k in new[] { "frag", "flashbang", "smoke" }) meta.Data.consumables[k] = Bundle(k) + 2; }
            else { if (health != null) health.Heal(30f); meta.Data.consumables["medkit"] = Bundle("medkit") + 2; }
            meta.Persist(); CalloutEvents.Raise("RAVITAILLEMENT");
            Destroy(gameObject);
        }
        int Bundle(string key) => meta.Data.consumables.TryGetValue(key, out int n) ? n : 0;
    }
}
