using System.Collections;
using Game.Data;
using UnityEngine;

namespace Game.Runtime
{
    public enum ViewMode { First, Third }

    /// Vue commutable 1ʳᵉ/3ᵉ personne (V) + visée (Q) : FOV, caméra orbitale 3ᵉ,
    /// visibilité avatar/viewmodel, séquence de mort. Formules et valeurs du web.
    public class PlayerViewController : MonoBehaviour
    {
        public Camera playerCamera;
        public Transform cameraPivot;
        public PlayerLook look;
        public PlayerAvatar avatar;
        public ViewmodelController viewmodel;
        public WeaponController weapon;
        public PlayerHealth health;
        public PlayerMotor motor;
        public PlayerInputBridge inputBridge;
        public HudController hud;

        /// Posé chaque frame par PlayerInputBridge (Q maintenu).
        [System.NonSerialized] public bool AimHeld;

        public readonly AdsModel Ads = new AdsModel();
        public ViewMode Mode { get; private set; } = ViewMode.First;
        public bool IsDead { get; private set; }
        public bool IsSniper => weapon != null && weapon.Weapon != null && weapon.Weapon.category == WeaponCategory.Sniper;
        public bool ScopeOn => Ads.ScopeOn(IsSniper);

        float camDist = ThirdPersonCameraModel.CamDist;
        const float DeathPanelDelay = 1.6f; // le web coupe direct au game over ; on laisse l'anim de mort se voir

        void Start()
        {
            if (health != null) health.Died += OnDied;
            ApplyVisibility();
        }

        public void ToggleView() => SetMode(Mode == ViewMode.First ? ViewMode.Third : ViewMode.First);

        public void SetMode(ViewMode mode)
        {
            Mode = mode;
            ApplyVisibility();
            if (Mode == ViewMode.First && playerCamera != null)
            {
                // recale la caméra sur le pivot (elle a été déplacée en monde par la 3ᵉ personne)
                playerCamera.transform.localPosition = Vector3.zero;
                playerCamera.transform.localRotation = Quaternion.identity;
            }
        }

        void ApplyVisibility()
        {
            if (avatar != null) avatar.SetVisible(Mode == ViewMode.Third || IsDead);
            if (viewmodel != null) viewmodel.SetVisible(Mode == ViewMode.First && !ScopeOn && !IsDead);
        }

        void LateUpdate()
        {
            if (playerCamera == null) return;
            bool aiming = AimHeld && !IsDead;
            Ads.Tick(Time.deltaTime, aiming);
            playerCamera.fieldOfView = Ads.Fov(IsSniper);

            if (Mode == ViewMode.Third)
            {
                camDist = ThirdPersonCameraModel.NextDist(camDist, aiming, Time.deltaTime);
                float yawRad = transform.eulerAngles.y * Mathf.Deg2Rad;
                float pitchRad = look != null ? -look.Pitch * Mathf.Deg2Rad : 0f; // web: pitch positif = regarder en haut
                var (desired, lookAt) = ThirdPersonCameraModel.Desired(transform.position, yawRad, pitchRad, camDist, aiming);
                var cam = playerCamera.transform;
                cam.position = Vector3.Lerp(cam.position, desired, Mathf.Min(1f, Time.deltaTime * ThirdPersonCameraModel.PosLerpRate));
                cam.LookAt(lookAt);
            }
            ApplyVisibility(); // le scope peut basculer d'une frame à l'autre
        }

        void OnDied()
        {
            if (IsDead) return;
            IsDead = true;
            SetMode(ViewMode.Third);      // la mort se regarde en 3ᵉ personne (l'avatar joue le clip)
            if (avatar != null) avatar.PlayDeath();
            if (inputBridge != null) inputBridge.enabled = false;
            if (motor != null) motor.enabled = false;
            if (weapon != null) weapon.enabled = false;
            StartCoroutine(ShowDeathScreenAfterDelay());
        }

        IEnumerator ShowDeathScreenAfterDelay()
        {
            yield return new WaitForSecondsRealtime(DeathPanelDelay);
            if (hud != null) hud.ShowDeathScreen();
        }
    }
}
