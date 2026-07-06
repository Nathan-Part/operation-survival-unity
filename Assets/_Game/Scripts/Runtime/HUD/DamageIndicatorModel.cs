using UnityEngine;

namespace Game.Runtime
{
    /// Indicateurs directionnels de dégâts (web #63) : 4 secteurs de 90°
    /// autour de l'angle attaquant relatif au regard.
    public static class DamageIndicatorModel
    {
        /// 0 = devant, 1 = droite, 2 = derrière, 3 = gauche.
        public static int SectorFor(float worldAngleDeg, float cameraYawDeg)
        {
            float rel = Mathf.DeltaAngle(cameraYawDeg, worldAngleDeg);
            if (rel > -45f && rel <= 45f) return 0;
            if (rel > 45f && rel <= 135f) return 1;
            if (rel > -135f && rel <= -45f) return 3;
            return 2;
        }

        /// Angle monde (degrés) de l'attaquant vu du joueur (web: atan2(dx, dz)).
        public static float WorldAngleDeg(Vector3 playerPos, Vector3 sourcePos) =>
            Mathf.Atan2(sourcePos.x - playerPos.x, sourcePos.z - playerPos.z) * Mathf.Rad2Deg;
    }
}
