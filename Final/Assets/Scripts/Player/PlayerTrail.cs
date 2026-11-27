using System;
using System.Collections.Generic;
using UnityEngine;

namespace MGS2Tanker
{
    /// <summary>
    /// Rastro de posiciones del jugador. Los soldados que lo persiguen y pierden la línea directa
    /// van al punto más reciente del rastro que puedan alcanzar en línea recta (así doblan esquinas
    /// sin NavMesh, usando sólo vectores).
    /// </summary>
    public class PlayerTrail : MonoBehaviour
    {
        public float spacing = 0.75f;
        public int maxPoints = 80;

        readonly List<Vector3> points = new List<Vector3>();
        public IReadOnlyList<Vector3> Points => points;

        void Update()
        {
            if (PauseManager.IsPaused) return;
            Vector3 p = transform.position;
            if (points.Count == 0 || (p - points[points.Count - 1]).sqrMagnitude > spacing * spacing)
            {
                points.Add(p);
                if (points.Count > maxPoints) points.RemoveAt(0);
            }
        }

        public bool TryGetChasePoint(Func<Vector3, bool> isReachable, out Vector3 point)
        {
            for (int i = points.Count - 1; i >= 0; i--)
            {
                if (isReachable(points[i])) { point = points[i]; return true; }
            }
            point = default;
            return false;
        }

        public void Clear() => points.Clear();

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.6f);
            for (int i = 1; i < points.Count; i++) Gizmos.DrawLine(points[i - 1] + Vector3.up * 0.1f, points[i] + Vector3.up * 0.1f);
        }
    }
}
