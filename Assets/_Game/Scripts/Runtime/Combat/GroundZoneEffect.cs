using System;
using UnityEngine;

namespace Game.Runtime
{
    /// Zone au sol temporaire (poison du toxic #141, gel du cryo #146) :
    /// disque semi-transparent, tick optionnel tant que le joueur est dans le rayon.
    public class GroundZoneEffect : MonoBehaviour
    {
        const float VerticalGuard = 1.6f; // web #204 : garde verticale du poison

        float radius;
        float dieAt;
        float tickInterval;
        float nextTickAt;
        Transform player;
        Action onPlayerTick;      // poison : dégâts par tick
        Action<bool> onPlayerInside; // gel : entrée/sortie (true/false)
        bool playerWasInside;

        public static GroundZoneEffect Create(Vector3 position, Color color, float radius, float duration,
            Transform player, float tickInterval = 0f, Action onPlayerTick = null, Action<bool> onPlayerInside = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "GroundZone";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = position + Vector3.up * 0.03f;
            go.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { color = color };
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var zone = go.AddComponent<GroundZoneEffect>();
            zone.radius = radius;
            zone.dieAt = Time.time + duration;
            zone.tickInterval = tickInterval;
            zone.nextTickAt = Time.time + tickInterval;
            zone.player = player;
            zone.onPlayerTick = onPlayerTick;
            zone.onPlayerInside = onPlayerInside;
            return zone;
        }

        bool PlayerInside()
        {
            if (player == null) return false;
            var delta = player.position - transform.position;
            if (Mathf.Abs(delta.y) > VerticalGuard) return false;
            delta.y = 0f;
            return delta.sqrMagnitude <= radius * radius;
        }

        void Update()
        {
            bool inside = PlayerInside();
            if (onPlayerInside != null && inside != playerWasInside)
            {
                onPlayerInside(inside);
                playerWasInside = inside;
            }
            if (onPlayerTick != null && tickInterval > 0f && Time.time >= nextTickAt)
            {
                nextTickAt = Time.time + tickInterval;
                if (inside) onPlayerTick();
            }
            if (Time.time >= dieAt)
            {
                if (playerWasInside && onPlayerInside != null) onPlayerInside(false); // ne pas laisser le joueur « gelé »
                Destroy(gameObject);
            }
        }
    }
}
