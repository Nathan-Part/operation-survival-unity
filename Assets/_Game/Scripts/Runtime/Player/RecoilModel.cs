using UnityEngine;

namespace Game.Runtime
{
    /// Recul visuel du viewmodel (parité web : weaponRecoil, poses VM_HIP/VM_ADS).
    public class RecoilModel
    {
        public static readonly Vector3 VmHip = new Vector3(0.12f, -0.11f, -0.34f); // web: VM_HIP
        public static readonly Vector3 VmAds = new Vector3(0.05f, -0.26f, -0.30f); // web: VM_ADS
        public const float DecayRate = 6f;                                          // web: weaponRecoil -= dt*6

        public float Current { get; private set; }

        public void Kick(float amount) => Current = amount; // web: weaponRecoil = curRecoil

        public void Tick(float dt) => Current = Mathf.Max(0f, Current - dt * DecayRate);

        public Vector3 ViewmodelOffset(float adsT)
        {
            var pos = Vector3.Lerp(VmHip, VmAds, adsT);
            pos.z += Current * 0.06f; // web: viewModel.position.z += weaponRecoil*0.06
            return pos;
        }

        /// Euler en radians (web: viewModel.rotation.set(0.05+recoil*0.18+0.25*adsT, PI+0.12*(1-adsT), 0)).
        public Vector3 ViewmodelEuler(float adsT) =>
            new Vector3(0.05f + Current * 0.18f + 0.25f * adsT, Mathf.PI + 0.12f * (1f - adsT), 0f);
    }
}
