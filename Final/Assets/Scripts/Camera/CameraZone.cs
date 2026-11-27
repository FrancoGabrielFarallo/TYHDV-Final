using System.Collections.Generic;
using UnityEngine;

namespace MGS2Tanker
{
    /// <summary>
    /// Zona de cámara (como los cambios de plano de MGS2). Si el jugador está dentro, la cámara usa esta configuración.
    ///  - FollowOffset:  sigue al jugador con un offset fijo (ángulo fijo, la vista cenital clásica).
    ///  - FixedTracking: la cámara queda quieta en fixedPoint y gira para mirar al jugador (cámara de esquina).
    ///  - FixedStatic:   posición y rotación fijas de fixedPoint.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class CameraZone : MonoBehaviour
    {
        public enum Mode { FollowOffset, FixedTracking, FixedStatic }

        public Mode mode = Mode.FollowOffset;
        [Tooltip("Mayor prioridad gana si hay zonas superpuestas.")]
        public int priority = 1;
        public Vector3 followOffset = new Vector3(0f, 10f, -4f);
        public Transform fixedPoint;

        static readonly List<CameraZone> all = new List<CameraZone>();
        BoxCollider box;

        void Awake()
        {
            box = GetComponent<BoxCollider>();
            box.isTrigger = true;
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public bool Contains(Vector3 p) => BoxVolume.Contains(box ? box : GetComponent<BoxCollider>(), p);

        public static CameraZone FindBest(Vector3 p)
        {
            CameraZone best = null;
            foreach (var z in all)
                if (z.Contains(p) && (best == null || z.priority > best.priority)) best = z;
            return best;
        }

        void OnDrawGizmos()
        {
            var b = GetComponent<BoxCollider>();
            if (!b) return;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.4f, 1f, 0.6f);
            Gizmos.DrawWireCube(b.center, b.size);
            Gizmos.matrix = Matrix4x4.identity;
            if (fixedPoint)
            {
                Gizmos.DrawWireSphere(fixedPoint.position, 0.3f);
                Gizmos.DrawLine(fixedPoint.position, transform.TransformPoint(b.center));
            }
        }
    }
}
