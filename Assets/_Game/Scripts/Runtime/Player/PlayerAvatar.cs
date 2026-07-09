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

        public void Init(GameObject modelPrefab, AnimationLibrary library, PlayerMotor motor, GameObject weaponModel, float weaponModelScale = 1f)
        {
            this.motor = motor;
            if (modelPrefab == null) return;

            visual = Instantiate(modelPrefab, transform);
            visual.transform.localPosition = Vector3.zero; // pivot GLB aux pieds (hypothèse — corrigée ci-dessous si fausse)
            visual.transform.localRotation = Quaternion.identity;

            // Playtest Phase 5.1 (2026-07-09), bug D3 (avatar "flotte" en 3e personne) : diagnostiqué
            // via execute_code sur blue-soldier.glb (le même rig que le bug d'arme géante, Phase 4
            // Task 2) — le pivot du GLB n'est PAS exactement aux pieds : les bones LeftFoot/RightFoot
            // sont mesurés ~0.11 m AU-DESSUS de l'origine du modèle en pose de bind. Plutôt que de
            // corriger cette valeur en dur (spécifique à ce rig, pas garanti pour un futur modèle),
            // on mesure la position des pieds à l'instanciation et on translate le visuel pour que
            // les pieds touchent bien y=0 — mesuré AVANT le premier Update() (donc en pose de bind,
            // pas au milieu d'un clip d'idle qui pourrait fausser la lecture).
            float groundOffset = ComputeGroundOffset(visual.transform);
            if (Mathf.Abs(groundOffset) > 0.001f)
                visual.transform.localPosition -= new Vector3(0f, groundOffset, 0f);

            if (library != null)
            {
                var animator = visual.GetComponent<Animator>();
                if (animator == null) animator = visual.AddComponent<Animator>();
                driver = visual.AddComponent<AnimationDriver>();
                driver.Bind(animator, library);
            }

            AttachWeapon(weaponModel, weaponModelScale);
        }

        // Grip main droite. Offsets exprimés dans l'espace du bone, robustes à l'échelle du rig
        // (le GLB peut être importé à une échelle ≠ 1). web: attachWeaponToHand.
        static readonly Vector3 GripLocalPos = new Vector3(0.02f, 0.03f, 0.06f); // TODO(Nathan): affiner à l'œil
        static readonly Vector3 GripLocalEuler = new Vector3(0f, 90f, 0f);       // TODO(Nathan): affiner à l'œil

        void AttachWeapon(GameObject weaponModel, float weaponModelScale)
        {
            if (visual == null || weaponModel == null) return; // placeholder déjà résolu en amont : on ne skippe plus sur modelPrefab null
            var hand = FindHandBone(visual.transform);
            if (hand == null) { Debug.LogWarning("PlayerAvatar: bone main droite introuvable — arme non attachée."); return; }
            var held = Instantiate(weaponModel, hand);
            var templateScale = held.transform.localScale; // échelle propre du prefab d'arme (généralement 1, mais à préserver si non)
            held.transform.localPosition = GripLocalPos;
            held.transform.localRotation = Quaternion.Euler(GripLocalEuler);
            // Neutralise l'échelle monde du bone : le rig du perso porte souvent une correction
            // d'échelle intentionnelle sur son Armature (ex. blue-soldier.glb -> RightHand.lossyScale
            // = 0.01, nécessaire pour que LE PERSO rende à la bonne taille), qui n'a rien à voir avec
            // l'arme et ne doit pas s'appliquer à elle (sinon l'arme hérite d'une échelle x100 et
            // devient géante). weaponModelScale corrige en plus un éventuel défaut d'export du
            // modèle d'arme lui-même (voir WeaponData.modelScale).
            var s = hand.lossyScale;
            var handCompensation = new Vector3(
                s.x > 1e-4f ? 1f / s.x : 1f, s.y > 1e-4f ? 1f / s.y : 1f, s.z > 1e-4f ? 1f / s.z : 1f);
            held.transform.localScale = Vector3.Scale(templateScale, handCompensation) * weaponModelScale;
        }

        /// Mesure de combien (en Y local, espace du GameObject `visual`) les pieds du rig
        /// dépassent au-dessus (ou en dessous) de l'origine du modèle en pose de bind — voir le
        /// commentaire d'appel dans Init(). Renvoie 0 si aucun bone de pied n'est trouvé (aucune
        /// correction appliquée plutôt qu'une supposition).
        static float ComputeGroundOffset(Transform visualRoot)
        {
            Transform left = null, right = null;
            foreach (var t in visualRoot.GetComponentsInChildren<Transform>())
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(t.name, "left.?foot|foot.?l|lfoot", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    left = t;
                else if (System.Text.RegularExpressions.Regex.IsMatch(t.name, "right.?foot|foot.?r|rfoot", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    right = t;
            }
            if (left == null && right == null) return 0f;

            float sum = 0f; int count = 0;
            if (left != null) { sum += visualRoot.InverseTransformPoint(left.position).y; count++; }
            if (right != null) { sum += visualRoot.InverseTransformPoint(right.position).y; count++; }
            return sum / count;
        }

        public static Transform FindHandBone(Transform root)
        {
            Transform fallback = null;
            foreach (var t in root.GetComponentsInChildren<Transform>())
            {
                // Noms exacts prioritaires (Mixamo/GLB), puis repli regex.
                if (t.name == "mixamorig:RightHand" || t.name == "RightHand" || t.name == "Hand_R") return t;
                if (fallback == null && System.Text.RegularExpressions.Regex.IsMatch(
                        t.name, "right.?hand|hand.?r|rhand", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
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
