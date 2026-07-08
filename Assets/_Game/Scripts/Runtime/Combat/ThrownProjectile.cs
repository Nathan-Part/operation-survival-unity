using System;
using UnityEngine;

namespace Game.Runtime
{
    /// Grenade/objet lancé : vol balistique (gravité), détone après la fusée (ou au repos pour
    /// molotov). web: spawnThrownProjectile — spawn 0.5 m devant la caméra, vy += arc.
    public class ThrownProjectile : MonoBehaviour
    {
        Vector3 velocity;
        float gravity;
        float detonateAt;
        Action<Vector3> onDetonate;
        Func<float> now = () => Time.time;

        public static ThrownProjectile Spawn(Vector3 origin, Vector3 dir, float speed, float gravity,
            float fuseSeconds, Action<Vector3> onDetonate, GameObject visual = null)
        {
            var go = visual != null ? Instantiate(visual) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Thrown";
            go.transform.position = origin;
            if (visual == null) go.transform.localScale = Vector3.one * 0.15f;
            foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false; // pas de collision physique
            var p = go.AddComponent<ThrownProjectile>();
            p.velocity = dir.normalized * speed + Vector3.up * 3f; // web: vel.y += 3 (arc)
            p.gravity = gravity;
            p.detonateAt = Time.time + fuseSeconds;
            p.onDetonate = onDetonate;
            return p;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            velocity += Vector3.down * gravity * dt;
            transform.position += velocity * dt;
            if (transform.position.y <= 0.05f) { velocity.y = Mathf.Abs(velocity.y) * 0.4f; transform.position = new Vector3(transform.position.x, 0.05f, transform.position.z); } // rebond web ×0.4
            if (now() >= detonateAt) { onDetonate?.Invoke(transform.position); Destroy(gameObject); }
        }
    }
}
