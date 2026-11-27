using UnityEngine;
using UnityEngine.Rendering;

namespace MGS2Tanker
{
    /// <summary>Línea de bala que se desvanece (feedback visual de los disparos).</summary>
    public class Tracer : MonoBehaviour
    {
        LineRenderer line;
        float life, t;
        Color color;

        public static void Spawn(Vector3 from, Vector3 to, Color color, float width = 0.03f, float duration = 0.06f)
        {
            var go = new GameObject("Tracer");
            var tracer = go.AddComponent<Tracer>();
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.startWidth = lr.endWidth = width;
            lr.material = RuntimeMaterials.CreateUnlit(Color.white);
            lr.startColor = lr.endColor = color;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            tracer.line = lr;
            tracer.life = Mathf.Max(0.01f, duration);
            tracer.color = color;
        }

        void Update()
        {
            t += Time.deltaTime;
            Color c = color;
            c.a *= Mathf.Clamp01(1f - t / life);
            line.startColor = line.endColor = c;
            if (t >= life)
            {
                Destroy(line.material);
                Destroy(gameObject);
            }
        }
    }
}
