using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MGS2Tanker
{
    /// <summary>
    /// Detección por matemática de vectores (cono de visión):
    ///
    ///  1) Vector al objetivo:      v = P_objetivo - P_ojo
    ///  2) Distancia:               |v|  ->  debe ser  &lt;= alcance (5 m)
    ///  3) Dirección normalizada:   v̂ = v / |v|
    ///  4) Producto punto:          f̂ · v̂ = cos(θ)   (f̂ = forward del ojo, normalizado)
    ///  5) Dentro del cono si:      cos(θ) &gt;= cos(apertura / 2)    (apertura = 40° -> semiángulo 20°)
    ///  6) Obstrucción:             Raycast ojo -> objetivo; si lo primero que golpea no es el jugador,
    ///                              una estructura bloquea la visión.
    ///
    /// Con flatCheck = true (soldados) el cálculo se hace en el plano XZ (se ignora la altura).
    /// Con flatCheck = false (cámaras de seguridad) el cono es 3D, porque la cámara mira hacia abajo.
    /// Los gizmos del cono, del alcance y de la línea al jugador se ven en la vista Scene.
    /// </summary>
    public class VisionSensor : MonoBehaviour
    {
        [Tooltip("Origen y dirección de la visión. Si es null se usa este transform a 1.6 m.")]
        public Transform eye;
        [Tooltip("Apertura TOTAL del cono en grados (40° = 20° a cada lado).")]
        [Range(1f, 180f)] public float viewAngle = 40f;
        [Tooltip("Alcance de la visión en metros.")]
        public float viewRange = 5f;
        [Tooltip("True: cono 2D en el plano XZ (soldados). False: cono 3D (cámaras).")]
        public bool flatCheck = true;
        [Tooltip("Capas que pueden bloquear la visión.")]
        public LayerMask obstacleMask = ~0;

        public bool PlayerVisible { get; private set; }
        public bool PlayerInCone { get; private set; }

        // Datos de debug para los gizmos
        Vector3 lastTargetPoint;
        bool hasTarget;
        float lastAngle, lastDistance;

        public Vector3 EyePosition => eye ? eye.position : transform.position + Vector3.up * 1.6f;
        public Vector3 Forward => eye ? eye.forward : transform.forward;
        public float HalfAngle => viewAngle * 0.5f;

        /// <summary>Evalúa si el jugador es visible este frame (pecho o cabeza).</summary>
        public bool Sense(PlayerHealth player)
        {
            PlayerVisible = false;
            PlayerInCone = false;
            if (!enabled || player == null) { hasTarget = false; return false; }

            hasTarget = true;
            lastTargetPoint = player.AimPoint;
            if (CheckPoint(player.AimPoint, player) || CheckPoint(player.HeadPoint, player))
                PlayerVisible = true;
            return PlayerVisible;
        }

        bool CheckPoint(Vector3 point, PlayerHealth player)
        {
            if (!IsInsideCone(point)) return false;
            PlayerInCone = true;
            return HasLineOfSight(point, player);
        }

        /// <summary>Pasos 1 a 5: distancia + producto punto.</summary>
        public bool IsInsideCone(Vector3 point)
        {
            Vector3 origin = EyePosition;
            Vector3 forward = Forward;
            Vector3 toTarget = point - origin;              // 1) vector al objetivo

            if (flatCheck) { toTarget.y = 0f; forward.y = 0f; }
            if (forward.sqrMagnitude < 1e-6f) return false;
            forward.Normalize();

            float sqrDist = toTarget.sqrMagnitude;
            lastDistance = Mathf.Sqrt(sqrDist);
            if (sqrDist > viewRange * viewRange) { lastAngle = 180f; return false; }   // 2) alcance
            if (sqrDist < 1e-4f) { lastAngle = 0f; return true; }

            Vector3 dir = toTarget / lastDistance;           // 3) normalizar
            float dot = Vector3.Dot(forward, dir);           // 4) cos(θ)
            lastAngle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;
            return dot >= Mathf.Cos(HalfAngle * Mathf.Deg2Rad);   // 5) dentro del cono
        }

        /// <summary>Paso 6: ¿hay una estructura entre el ojo y el punto?</summary>
        public bool HasLineOfSight(Vector3 point, PlayerHealth player)
        {
            Vector3 origin = EyePosition;
            Vector3 d = point - origin;
            float dist = d.magnitude;
            if (dist < 1e-3f) return true;
            if (Physics.Raycast(origin, d / dist, out RaycastHit hit, dist, obstacleMask, QueryTriggerInteraction.Ignore))
                return hit.collider.GetComponentInParent<PlayerHealth>() == player;
            return true;
        }

        void OnDrawGizmos()
        {
            if (Application.isPlaying && !enabled) return;

            Vector3 o = EyePosition;
            Vector3 fwd = Forward;
            Color col = !Application.isPlaying ? new Color(1f, 0.9f, 0.2f)
                : PlayerVisible ? Color.red
                : PlayerInCone ? new Color(1f, 0.5f, 0f)
                : new Color(1f, 0.9f, 0.2f);

            if (flatCheck)
            {
                fwd.y = 0f;
                if (fwd.sqrMagnitude < 1e-6f) return;
                fwd.Normalize();
                Vector3 left = Quaternion.AngleAxis(-HalfAngle, Vector3.up) * fwd;
                Vector3 right = Quaternion.AngleAxis(HalfAngle, Vector3.up) * fwd;

                Gizmos.color = col;
                Gizmos.DrawLine(o, o + left * viewRange);
                Gizmos.DrawLine(o, o + right * viewRange);
                Gizmos.color = new Color(col.r, col.g, col.b, 0.5f);
                Gizmos.DrawLine(o, o + fwd * viewRange);      // vector forward

                const int segments = 16;
                Vector3 prev = o + left * viewRange;
                for (int i = 1; i <= segments; i++)
                {
                    Vector3 d = Quaternion.AngleAxis(-HalfAngle + viewAngle * i / segments, Vector3.up) * fwd;
                    Vector3 p = o + d * viewRange;
                    Gizmos.color = col;
                    Gizmos.DrawLine(prev, p);
                    prev = p;
                }
#if UNITY_EDITOR
                Handles.color = new Color(col.r, col.g, col.b, 0.08f);
                Handles.DrawSolidArc(o, Vector3.up, left, viewAngle, viewRange);
#endif
            }
            else
            {
                if (fwd.sqrMagnitude < 1e-6f) return;
                fwd.Normalize();
                Vector3 perp = Vector3.Cross(fwd, Vector3.up);
                if (perp.sqrMagnitude < 1e-4f) perp = Vector3.Cross(fwd, Vector3.right);
                perp.Normalize();

                float ringDist = viewRange * Mathf.Cos(HalfAngle * Mathf.Deg2Rad);
                float ringRadius = viewRange * Mathf.Sin(HalfAngle * Mathf.Deg2Rad);
                Vector3 ringCenter = o + fwd * ringDist;

                const int segments = 16;
                Vector3 prev = Vector3.zero;
                for (int i = 0; i <= segments; i++)
                {
                    Vector3 offset = Quaternion.AngleAxis(360f * i / segments, fwd) * perp * ringRadius;
                    Vector3 p = ringCenter + offset;
                    Gizmos.color = col;
                    if (i > 0) Gizmos.DrawLine(prev, p);
                    if (i % 4 == 0) Gizmos.DrawLine(o, p);
                    prev = p;
                }
                Gizmos.color = new Color(col.r, col.g, col.b, 0.5f);
                Gizmos.DrawLine(o, o + fwd * viewRange);
            }

            if (Application.isPlaying && hasTarget)
            {
                Gizmos.color = PlayerVisible ? Color.red : PlayerInCone ? new Color(1f, 0.5f, 0f) : new Color(0.6f, 0.6f, 0.6f, 0.35f);
                Gizmos.DrawLine(o, lastTargetPoint);
#if UNITY_EDITOR
                Handles.color = Color.white;
                Handles.Label(o + Vector3.up * 0.4f, $"θ = {lastAngle:0}° (máx {HalfAngle:0}°)\nd = {lastDistance:0.0} m (máx {viewRange:0.#} m)");
#endif
            }
        }
    }
}
