using UnityEngine;

namespace Game.Runtime
{
    /// Flash de bouche : quad jaune bref (web: makeMuzzleFlash 0.3 m, visible 0.05 s).
    public class MuzzleFlash : MonoBehaviour
    {
        const float Duration = 0.05f; // web: muzzleFlashUntil = now + 0.05
        static readonly Color FlashColor = new Color(1f, 0.815f, 0.375f, 0.95f); // web: 0xffd060

        Renderer quad;
        float hideAt = -1f;

        public static MuzzleFlash Create(Transform parent, Vector3 localPosition, float size = 0.3f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "MuzzleFlash";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = Vector3.one * size;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { color = FlashColor };
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.SetActive(false);
            var flash = go.AddComponent<MuzzleFlash>();
            flash.quad = renderer;
            return flash;
        }

        public void Flash()
        {
            gameObject.SetActive(true);
            if (Camera.main != null) transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
            hideAt = Time.time + Duration;
            enabled = true;
        }

        void Update()
        {
            if (hideAt >= 0f && Time.time >= hideAt)
            {
                gameObject.SetActive(false);
                hideAt = -1f;
            }
        }
    }
}
