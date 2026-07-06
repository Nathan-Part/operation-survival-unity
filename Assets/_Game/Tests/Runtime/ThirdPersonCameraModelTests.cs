using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Game.Runtime.Tests
{
    public class ThirdPersonCameraModelTests
    {
        [Test]
        public void Desired_YawZeroPitchZero_PlacesCameraBehind()
        {
            var (pos, lookAt) = ThirdPersonCameraModel.Desired(Vector3.zero, 0f, 0f, 5.5f, false);
            // look = +Z : caméra derrière le joueur (z négatif), à hauteur de cible
            Assert.AreEqual(-5.5f, pos.z, 1e-3f);
            Assert.AreEqual(1.5f, pos.y, 1e-3f);
            Assert.AreEqual(1.5f, lookAt.y, 1e-3f);
        }

        [Test]
        public void Desired_AimingAddsShoulderOffset()
        {
            var (posNoAim, _) = ThirdPersonCameraModel.Desired(Vector3.zero, 0f, 0f, 3f, false);
            var (posAim, lookAim) = ThirdPersonCameraModel.Desired(Vector3.zero, 0f, 0f, 3f, true);
            // yaw 0 : right = +X, épaule 0.6 décale caméra ET point visé en x
            Assert.AreEqual(posNoAim.x + 0.6f, posAim.x, 1e-3f);
            Assert.AreEqual(0.6f, lookAim.x, 1e-3f);
        }

        [Test]
        public void NextDist_ConvergesTowardsAimDist()
        {
            float d = ThirdPersonCameraModel.CamDist;
            for (int i = 0; i < 120; i++) d = ThirdPersonCameraModel.NextDist(d, true, 1f / 60f);
            Assert.AreEqual(ThirdPersonCameraModel.AimDist, d, 0.05f);
        }
    }
}
