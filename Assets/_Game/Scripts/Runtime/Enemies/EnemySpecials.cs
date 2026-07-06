using Game.Data;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Runtime
{
    /// Comportements spéciaux par type (web #37/#107/#119/#120/#128/#129/#141/#146) :
    /// aiguillés par EnemyData.id, toutes les valeurs dans SpecialRules.
    public class EnemySpecials : MonoBehaviour
    {
        static int commandersAlive; // aura du commander (web #151), partagée

        public static float CurrentCommanderMult => SpecialRules.CommanderMult(commandersAlive > 0);

        EnemyData data;
        EnemyHealth health;
        NavMeshAgent agent;
        WaveManager waves;
        Transform player;
        PlayerHealth playerHealth;
        PlayerStatusEffects playerStatus;

        float medicNextTick;
        float grenadeNextThrow;
        bool frenzyUsed;
        float frenzyUntil = -1f;
        float baseAgentSpeed;
        bool isCommander;

        public void Init(EnemyData data, EnemyHealth health, WaveManager waves, Transform player)
        {
            this.data = data;
            this.health = health;
            this.waves = waves;
            this.player = player;
            agent = GetComponent<NavMeshAgent>();
            baseAgentSpeed = agent != null ? agent.speed : 0f;
            playerHealth = player != null ? player.GetComponentInParent<PlayerHealth>() : null;
            playerStatus = player != null ? player.GetComponentInParent<PlayerStatusEffects>() : null;

            switch (data.id)
            {
                case "ghost":
                    ApplyOpacity(data.opacity); // web #107 : semi-transparent
                    break;
                case "shielder":
                    health.BlockCheck = shooterPos => !health.IsDead
                        && SpecialRules.ShieldBlocks(transform.position, transform.forward, shooterPos);
                    break;
                case "bomber":
                case "toxic":
                case "cryo":
                    health.Died += OnDiedSpecial;
                    break;
                case "berserker":
                    health.Damaged += TryFrenzy;
                    break;
                case "commander":
                    isCommander = true;
                    commandersAlive++;
                    health.Died += _ => ReleaseCommander();
                    break;
            }
        }

        void ApplyOpacity(float opacity)
        {
            if (opacity >= 1f) return;
            foreach (var renderer in GetComponentsInChildren<Renderer>())
                foreach (var material in renderer.materials)
                {
                    // matériaux glTFast (URP Lit) : bascule en transparent
                    if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    string prop = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                    if (!material.HasProperty(prop)) continue;
                    var c = material.GetColor(prop);
                    c.a = opacity;
                    material.SetColor(prop, c);
                }
        }

        void OnDiedSpecial(EnemyHealth _)
        {
            switch (data.id)
            {
                case "bomber": Explode(); break;
                case "toxic": // web #141 : nuage de poison à la mort
                    GroundZoneEffect.Create(transform.position, new Color(0.2f, 0.8f, 0.2f, 0.35f),
                        3f, 4f, player, 0.5f, onPlayerTick: () => playerHealth?.TakeDamage(8f, transform.position)); // web: POISON_*
                    break;
                case "cryo": // web #146 : zone de gel à la mort
                    GroundZoneEffect.Create(transform.position, new Color(0.4f, 0.8f, 1f, 0.35f),
                        3f, 4f, player, onPlayerInside: inside =>
                        { if (playerStatus == null) return; if (inside) playerStatus.EnterFrost(); else playerStatus.ExitFrost(); }); // web: FROST_*
                    break;
            }
        }

        /// Explosion du bomber (contact OU mort — web #37).
        void Explode()
        {
            if (playerHealth != null && player != null)
            {
                var delta = player.position - transform.position;
                delta.y = 0f;
                int dmg = SpecialRules.ExplosionDamage(delta.magnitude, SpecialRules.BomberRadius, SpecialRules.BomberDamage);
                if (dmg > 0) playerHealth.TakeDamage(dmg, transform.position);
            }
            SpawnBlastVisual(transform.position, SpecialRules.BomberRadius);
        }

        static void SpawnBlastVisual(Vector3 position, float radius)
        {
            var blast = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            blast.name = "Explosion";
            Destroy(blast.GetComponent<Collider>());
            blast.transform.position = position + Vector3.up;
            blast.transform.localScale = Vector3.one * radius * 1.4f;
            var renderer = blast.GetComponent<Renderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { color = new Color(1f, 0.55f, 0.1f, 0.6f) };
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Destroy(blast, 0.2f);
        }

        void TryFrenzy(EnemyHealth h)
        {
            if (!SpecialRules.FrenzyShouldTrigger(h.Health, h.MaxHealth, frenzyUsed)) return;
            frenzyUsed = true;
            frenzyUntil = Time.time + SpecialRules.FrenzyDuration;
            if (agent != null && agent.enabled) agent.speed = baseAgentSpeed * SpecialRules.FrenzySpeedMult;
            Tint(new Color(1f, 0.15f, 0.15f));
            CalloutEvents.Raise("FRENZY!"); // web #129
        }

        void Tint(Color color)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>())
                foreach (var material in renderer.materials)
                {
                    string prop = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                    if (material.HasProperty(prop)) material.SetColor(prop, color);
                }
        }

        void ReleaseCommander()
        {
            if (!isCommander) return;
            isCommander = false;
            commandersAlive = Mathf.Max(0, commandersAlive - 1);
        }

        void Update()
        {
            if (health == null || health.IsDead) return;
            float now = Time.time;

            if (frenzyUntil > 0f && now >= frenzyUntil) // fin du frenzy : vitesse/teinte restaurées (web #129)
            {
                frenzyUntil = -1f;
                if (agent != null && agent.enabled) agent.speed = baseAgentSpeed;
                Tint(Color.white);
            }

            if (data.id == "medic" && now >= medicNextTick) // web #120
            {
                medicNextTick = now + SpecialRules.MedicTick;
                if (waves != null)
                {
                    int healAmount = Mathf.RoundToInt(SpecialRules.MedicHealRate * SpecialRules.MedicTick);
                    foreach (var ally in waves.Alive)
                    {
                        if (ally == null || ally == health || ally.IsDead) continue;
                        if ((ally.transform.position - transform.position).sqrMagnitude > SpecialRules.MedicRange * SpecialRules.MedicRange) continue;
                        ally.Heal(healAmount);
                    }
                }
            }

            if (data.id == "grenadier" && player != null && playerHealth != null) // web #128
            {
                var delta = player.position - transform.position;
                delta.y = 0f;
                if (now >= grenadeNextThrow)
                {
                    if (delta.magnitude < SpecialRules.GrenadeMinRange) grenadeNextThrow = now + 1f; // trop proche, réessaie (web)
                    else
                    {
                        grenadeNextThrow = now + SpecialRules.GrenadeCooldown;
                        StartCoroutine(ThrowGrenade(player.position));
                    }
                }
            }
        }

        System.Collections.IEnumerator ThrowGrenade(Vector3 target)
        {
            const float flight = 1f; // vol en cloche ~1 s vers la position du joueur au lancer
            var grenade = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            grenade.name = "Grenade";
            Destroy(grenade.GetComponent<Collider>());
            grenade.transform.localScale = Vector3.one * 0.25f;
            var start = transform.position + Vector3.up * 1.5f;
            float t = 0f;
            while (t < flight && grenade != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / flight);
                var pos = Vector3.Lerp(start, target, k);
                pos.y += 4f * k * (1f - k) * 3f; // cloche (hauteur max 3 m)
                grenade.transform.position = pos;
                yield return null;
            }
            if (grenade != null) Destroy(grenade);
            if (playerHealth != null && player != null)
            {
                var delta = player.position - target;
                delta.y = 0f;
                int dmg = delta.magnitude < SpecialRules.GrenadeRadius
                    ? Mathf.RoundToInt(SpecialRules.GrenadeDamage) : 0; // web: dégâts pleins dans le rayon
                if (dmg > 0) playerHealth.TakeDamage(dmg, target);
            }
            SpawnBlastVisual(target, SpecialRules.GrenadeRadius);
        }

        void OnDestroy() => ReleaseCommander();
    }
}
