using Game.Data;
using UnityEngine;

namespace Game.Runtime
{
    /// Orbe de power-up (web #34+, PICKUP_R=1.6m) : lâché par WaveManager.OnEnemyDied (12% de chance),
    /// ramassage au contact avec le joueur → active l'effet sur le PowerUpRuntime unique de la partie
    /// (composant posé sur le joueur, cf. SliceBootstrap) puis se détruit. Thin : aucune logique de jeu ici.
    public class PowerUpPickup : MonoBehaviour
    {
        PowerUpData data;
        PowerUpRuntime powerups;

        public static PowerUpPickup Spawn(Vector3 pos, PowerUpData data)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"PowerUp_{data.id}";
            go.transform.position = pos + Vector3.up * 0.5f;
            go.transform.localScale = Vector3.one * 0.4f;
            foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false; // ramassage par distance, pas par trigger

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && ColorUtility.TryParseHtmlString(data.colorHex, out Color color))
            {
                if (renderer.material.HasProperty("_BaseColor")) renderer.material.SetColor("_BaseColor", color);
                else renderer.material.color = color;
            }

            var p = go.AddComponent<PowerUpPickup>();
            p.data = data;
            return p;
        }

        void Update()
        {
            if (data == null) return;
            if (powerups == null) powerups = FindAnyObjectByType<PowerUpRuntime>();
            if (powerups == null) return;
            if (Vector3.Distance(transform.position, powerups.transform.position) > PowerUpRuntime.PickupR) return;

            powerups.Activate(data.id, data.duration, data.isInstant);
            CalloutEvents.Raise($"POWER-UP : {data.displayName}");
            Destroy(gameObject);
        }
    }
}
