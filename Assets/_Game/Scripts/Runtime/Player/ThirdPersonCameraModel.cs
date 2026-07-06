using UnityEngine;

namespace Game.Runtime
{
    /// Caméra 3ᵉ personne : formule du web updateCamera (orbite derrière le joueur,
    /// épaule décalée en visée). Pur : le PlayerViewController applique le résultat.
    public static class ThirdPersonCameraModel
    {
        public const float CamDist = 5.5f;      // web: CAM_DIST
        public const float AimDist = 3f;        // web: AIM_DIST
        public const float Shoulder = 0.6f;     // web: SHOULDER
        public const float TargetHeight = 1.5f; // web: ty = p.y + 1.5
        public const float DistLerpRate = 10f;  // web: camDist lerp dt*10
        public const float PosLerpRate = 8f;    // web: camera.position.lerp dt*8

        /// yaw/pitch en radians (convention web : look = (sin yaw·cos pitch, sin pitch, cos yaw·cos pitch)).
        public static (Vector3 pos, Vector3 lookAt) Desired(Vector3 playerPos, float yawRad, float pitchRad, float dist, bool aiming)
        {
            var look = new Vector3(
                Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
                Mathf.Sin(pitchRad),
                Mathf.Cos(yawRad) * Mathf.Cos(pitchRad));
            var right = new Vector3(Mathf.Cos(yawRad), 0f, -Mathf.Sin(yawRad));
            float sh = aiming ? Shoulder : 0f;
            float ty = playerPos.y + TargetHeight;
            var pos = new Vector3(
                playerPos.x - look.x * dist + right.x * sh,
                ty + look.y * dist,
                playerPos.z - look.z * dist + right.z * sh);
            var lookAt = new Vector3(playerPos.x + right.x * sh, ty, playerPos.z + right.z * sh);
            return (pos, lookAt);
        }

        public static float NextDist(float current, bool aiming, float dt)
        {
            float target = aiming ? AimDist : CamDist;
            return current + (target - current) * Mathf.Min(1f, dt * DistLerpRate);
        }
    }
}
