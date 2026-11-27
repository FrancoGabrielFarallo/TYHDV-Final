using System.Collections.Generic;
using UnityEngine;

namespace MGS2Tanker
{
    /// <summary>
    /// Ruta de patrulla: lista de puntos (Transforms). Si la lista está vacía se usan los hijos.
    /// El forward de cada punto indica hacia dónde mira el soldado mientras espera ahí.
    /// No la pongas como hija del soldado (se movería con él).
    /// </summary>
    public class PatrolRoute : MonoBehaviour
    {
        public enum Mode { Loop, PingPong }

        public Mode mode = Mode.Loop;
        [Tooltip("Espera en cada punto en segundos (-1 = usar la del Soldier).")]
        public float waitTime = -1f;
        public List<Transform> points = new List<Transform>();

        public int Count => points.Count;

        void Awake() => CollectChildrenIfEmpty();

        void CollectChildrenIfEmpty()
        {
            if (points.Count > 0) return;
            foreach (Transform child in transform) points.Add(child);
        }

        public Vector3 GetPoint(int i) => points[Mathf.Clamp(i, 0, points.Count - 1)].position;
        public Transform GetTransform(int i) => points[Mathf.Clamp(i, 0, points.Count - 1)];

        public int Next(int i, ref int direction)
        {
            if (points.Count <= 1) return 0;
            if (mode == Mode.Loop) return (i + 1) % points.Count;
            int n = i + direction;
            if (n >= points.Count || n < 0) { direction = -direction; n = i + direction; }
            return n;
        }

        public int ClosestIndex(Vector3 position)
        {
            int best = 0;
            float bestD = float.MaxValue;
            for (int i = 0; i < points.Count; i++)
            {
                float d = (points[i].position - position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        void OnDrawGizmos()
        {
            var list = points.Count > 0 ? points : null;
            if (list == null)
            {
                list = new List<Transform>();
                foreach (Transform c in transform) list.Add(c);
            }
            Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.9f);
            for (int i = 0; i < list.Count; i++)
            {
                if (!list[i]) continue;
                Vector3 p = list[i].position + Vector3.up * 0.05f;
                Gizmos.DrawWireSphere(p, 0.25f);
                Gizmos.DrawLine(p, p + list[i].forward * 0.6f);
                bool last = i == list.Count - 1;
                if (!last && list[i + 1]) Gizmos.DrawLine(p, list[i + 1].position + Vector3.up * 0.05f);
                else if (last && mode == Mode.Loop && list.Count > 2 && list[0]) Gizmos.DrawLine(p, list[0].position + Vector3.up * 0.05f);
            }
        }
    }
}
