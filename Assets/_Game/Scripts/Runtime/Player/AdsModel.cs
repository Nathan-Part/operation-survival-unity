using UnityEngine;

namespace Game.Runtime
{
    /// Visée (Q) : progression ADS et FOV, valeurs du web.
    public class AdsModel
    {
        public const float FovBase = 55f;   // web: FOV_BASE
        public const float FovAds = 42f;    // web: FOV_ADS
        public const float FovScope = 20f;  // web: FOV_SCOPE
        public const float LerpRate = 10f;  // web: ADS_LERP

        public float AdsT { get; private set; }

        public void Tick(float dt, bool aiming)
        {
            float target = aiming ? 1f : 0f;
            AdsT += (target - AdsT) * Mathf.Min(1f, dt * LerpRate); // web: adsT += (aimNow-adsT)*min(1, dt*ADS_LERP)
        }

        public bool ScopeOn(bool isSniper) => isSniper && AdsT > 0.5f; // web: scopeOn

        public float Fov(bool isSniper)
        {
            float target = ScopeOn(isSniper) ? FovScope : FovAds;
            return FovBase + (target - FovBase) * AdsT;
        }
    }
}
