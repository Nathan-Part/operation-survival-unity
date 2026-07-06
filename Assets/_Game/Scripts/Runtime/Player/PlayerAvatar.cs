using Game.Data;
using UnityEngine;

namespace Game.Runtime
{
    /// Modèle 3D du joueur (visible en 3ᵉ personne) : locomotion animée depuis le
    /// PlayerMotor, arme clonée dans la main droite (parité web attachWeaponToHand).
    public class PlayerAvatar : MonoBehaviour
    {
        GameObject visual;
        AnimationDriver driver;
        PlayerMotor motor;
        AnimRole current = AnimRole.Idle;
        bool dying;
        bool started;

        const float LocoFade = 0.2f;  // web: crossfade loco
        const float DeathFade = 0.18f;

        public void Init(GameObject modelPrefab, AnimationLibrary library, PlayerMotor motor, WeaponData weapon)
        {
            this.motor = motor;
            if (modelPrefab == null) return;

            visual = Instantiate(modelPrefab, transform);
            visual.transform.localPosition = Vector3.zero; // pivot GLB aux pieds
            visual.transform.localRotation = Quaternion.identity;

            if (library != null)
            {
                var animator = visual.GetComponent<Animator>();
                if (animator == null) animator = visual.AddComponent<Animator>();
                driver = visual.AddComponent<AnimationDriver>();
                driver.Bind(animator, library);
            }

            AttachWeapon(weapon);
        }

        /// Arme dans la main droite (web: RightHand, pos (0,0.02,0.08), rot (90°,0,0)).
        void AttachWeapon(WeaponData weapon)
        {
            if (visual == null || weapon == null || weapon.modelPrefab == null) return;
            var hand = FindHandBone(visual.transform);
            if (hand == null) return;
            var held = Instantiate(weapon.modelPrefab, hand);
            held.transform.localPosition = new Vector3(0f, 0.02f, 0.08f);
            held.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            float ws = hand.lossyScale.x;
            if (ws > 0.0001f) held.transform.localScale = Vector3.one / ws; // garde la taille monde du template
        }

        public static Transform FindHandBone(Transform root)
        {
            Transform fallback = null;
            foreach (var t in root.GetComponentsInChildren<Transform>())
            {
                if (t.name == "RightHand") return t;
                if (fallback == null && System.Text.RegularExpressions.Regex.IsMatch(t.name, "righthand|hand_r|rhand", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    fallback = t;
            }
            return fallback;
        }

        public void SetVisible(bool visible)
        {
            if (visual != null && visual.activeSelf != visible) visual.SetActive(visible);
        }

        public void PlayDeath()
        {
            dying = true;
            driver?.Play(AnimRole.Dead, DeathFade, loop: false);
        }

        void Update()
        {
            if (driver == null || motor == null || dying) return;
            var role = !motor.IsMoving ? AnimRole.Idle : motor.IsRunning ? AnimRole.Run : AnimRole.Walk;
            if (!started || role != current)
            {
                driver.Play(role, LocoFade);
                current = role;
                started = true;
            }
        }
    }
}
