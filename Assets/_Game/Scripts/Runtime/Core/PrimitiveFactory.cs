using UnityEngine;

namespace Game.Runtime
{
    /// Visuels de secours quand ni le modèle ni la PlaceholderLibrary ne fournissent d'asset.
    public static class PrimitiveFactory
    {
        public static GameObject CreateCapsule(string name, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>()); // visuel pur : le collider appartient au parent
            var renderer = go.GetComponent<Renderer>();
            renderer.material.color = color;
            return go;
        }
    }
}
