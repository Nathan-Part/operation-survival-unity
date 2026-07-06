using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    /// Tracers hitscan : lignes brèves en fondu (web: tracers vie 0.08 s, opacité 0.9).
    public class TracerPool : MonoBehaviour
    {
        const float Life = 0.08f;                                              // web: life 0.08
        static readonly Color TracerColor = new Color(1f, 0.945f, 0.66f, 0.9f); // web: 0xfff1a8

        class Tracer { public LineRenderer line; public float life; }

        readonly List<Tracer> active = new List<Tracer>();
        readonly Stack<LineRenderer> pool = new Stack<LineRenderer>();
        Material material;

        void Awake()
        {
            material = new Material(Shader.Find("Sprites/Default"));
        }

        public void Show(Vector3 from, Vector3 to)
        {
            var line = pool.Count > 0 ? pool.Pop() : CreateLine();
            line.gameObject.SetActive(true);
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            var c = TracerColor;
            line.startColor = c; line.endColor = c;
            active.Add(new Tracer { line = line, life = Life });
        }

        LineRenderer CreateLine()
        {
            var go = new GameObject("Tracer");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.startWidth = 0.02f;
            line.endWidth = 0.02f;
            line.sharedMaterial = material;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return line;
        }

        void Update()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var t = active[i];
                t.life -= Time.deltaTime;
                if (t.life <= 0f)
                {
                    t.line.gameObject.SetActive(false);
                    pool.Push(t.line);
                    active.RemoveAt(i);
                    continue;
                }
                var c = TracerColor;
                c.a = Mathf.Max(0f, t.life / Life) * 0.9f;
                t.line.startColor = c; t.line.endColor = c;
            }
        }
    }
}
