using Game.Data;
using UnityEngine;

namespace Game.Runtime
{
    /// Arme en vue 1ʳᵉ personne : poses hanche/visée, recul, flash de bouche
    /// et kick caméra à chaque tir (valeurs web, cf. RecoilModel).
    public class ViewmodelController : MonoBehaviour
    {
        public readonly RecoilModel Recoil = new RecoilModel();

        GameObject held;
        MuzzleFlash muzzleFlash;
        Transform muzzleTip;
        PlayerViewController view;
        WeaponController weapon;
        PlayerLook look;
        TracerPool tracers;

        const float KickPitchDeg = 0.026f * Mathf.Rad2Deg; // web: KICK_PITCH (~1.5°)
        const float KickDuration = 0.08f;                  // web: KICK_DUR

        /// La référence croisée viewmodel↔view est posée après coup via BindView.
        public void BindView(PlayerViewController view) => this.view = view;

        public void Init(WeaponData data, Camera camera, WeaponController weapon, PlayerLook look, TracerPool tracers)
        {
            this.weapon = weapon;
            this.look = look;
            this.tracers = tracers;

            transform.SetParent(camera.transform, false);
            transform.localPosition = RecoilModel.VmHip;

            if (data != null && data.modelPrefab != null)
            {
                held = Instantiate(data.modelPrefab, transform);
                held.transform.localPosition = Vector3.zero;
                held.transform.localRotation = Quaternion.identity;
                foreach (var r in held.GetComponentsInChildren<Renderer>())
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                // bout du canon = z max des bounds locaux (web: weaponBarrelZ, fallback 0.45)
                float barrelZ = 0.45f;
                var renderers = held.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    var bounds = renderers[0].bounds;
                    foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                    barrelZ = Mathf.Max(0.1f, bounds.max.z - transform.position.z);
                }
                muzzleTip = new GameObject("MuzzleTip").transform;
                muzzleTip.SetParent(transform, false);
                muzzleTip.localPosition = new Vector3(0f, 0f, barrelZ);
                muzzleFlash = MuzzleFlash.Create(muzzleTip, Vector3.zero);
            }

            if (weapon != null)
            {
                weapon.Fired += OnFired;
                weapon.TracerRequested += OnTracer;
            }
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
        }

        void OnFired()
        {
            Recoil.Kick(weapon.Weapon != null ? weapon.EffectiveRecoil : 1f);
            if (muzzleFlash != null && gameObject.activeInHierarchy) muzzleFlash.Flash();
            float adsT = view != null ? view.Ads.AdsT : 0f;
            look?.Kick(KickPitchDeg * (adsT > 0.5f ? 0.5f : 1f), KickDuration); // web: cameraKick ×0.5 en visée
        }

        void OnTracer(Vector3 to)
        {
            if (tracers == null) return;
            Vector3 from = muzzleTip != null && gameObject.activeInHierarchy
                ? muzzleTip.position
                : transform.parent != null ? transform.parent.position : transform.position;
            tracers.Show(from, to);
        }

        void LateUpdate()
        {
            Recoil.Tick(Time.deltaTime);
            float adsT = view != null ? view.Ads.AdsT : 0f;
            transform.localPosition = Recoil.ViewmodelOffset(adsT);
            transform.localEulerAngles = Recoil.ViewmodelEuler(adsT) * Mathf.Rad2Deg;
        }

        void OnDestroy()
        {
            if (weapon != null)
            {
                weapon.Fired -= OnFired;
                weapon.TracerRequested -= OnTracer;
            }
        }
    }
}
