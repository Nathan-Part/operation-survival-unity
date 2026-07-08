using System.Collections;
using UnityEngine;
namespace Game.Runtime
{
    /// Frappe aérienne (web AIRSTRIKE_*): AoE 150 dégâts, rayon 5, sur le point sol visé (réticule).
    /// Simplification mobile assumée : le web ouvre une carte cliquable pour choisir la cible ;
    /// ici on cible au réticule (raycast écran centre). TODO Nathan : carte cliquable en phase 5.
    public static class Airstrike
    {
        public static void CallOnAim(MonoBehaviour host, Camera cam, WaveManager waves)
        {
            var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 target = Physics.Raycast(ray, out var hit, 200f) ? hit.point : cam.transform.position + cam.transform.forward * 20f;
            host.StartCoroutine(Strike(target, waves));
        }
        static IEnumerator Strike(Vector3 target, WaveManager waves)
        {
            CalloutEvents.Raise("FRAPPE AÉRIENNE");
            yield return new WaitForSeconds(1f);            // délai d'arrivée
            AreaDamage.Apply(waves.Alive, target, 5f, 150f); // web: AIRSTRIKE_TARGET_R=5, DMG=150
        }
    }
}
