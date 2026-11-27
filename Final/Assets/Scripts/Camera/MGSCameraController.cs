using UnityEngine;

namespace MGS2Tanker
{
    /// <summary>
    /// Cámara estilo MGS2: vista alta con ángulo fijo que sigue a Snake sin rotar,
    /// y cambios de plano por zonas (CameraZone). Apuntar NO cambia la cámara.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MGSCameraController : MonoBehaviour
    {
        public Transform target;
        [Tooltip("Offset por defecto (cubierta exterior). ~62° de inclinación.")]
        public Vector3 defaultOffset = new Vector3(0f, 13f, -7f);
        public float lookHeight = 1f;
        public float positionSharpness = 6f;
        public float rotationSharpness = 6f;
        public float fieldOfView = 45f;

        void Start()
        {
            if (!target && PlayerHealth.Current) target = PlayerHealth.Current.transform;
            GetComponent<Camera>().fieldOfView = fieldOfView;
            Snap();
        }

        void LateUpdate()
        {
            if (!target) return;
            Compute(out Vector3 pos, out Quaternion rot);
            float dt = Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, pos, 1f - Mathf.Exp(-positionSharpness * dt));
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 1f - Mathf.Exp(-rotationSharpness * dt));
        }

        public void Snap()
        {
            if (!target) return;
            Compute(out Vector3 pos, out Quaternion rot);
            transform.SetPositionAndRotation(pos, rot);
        }

        void Compute(out Vector3 pos, out Quaternion rot)
        {
            Vector3 focus = target.position + Vector3.up * lookHeight;
            CameraZone zone = CameraZone.FindBest(target.position);

            if (zone == null || zone.mode == CameraZone.Mode.FollowOffset || zone.fixedPoint == null)
            {
                Vector3 offset = zone ? zone.followOffset : defaultOffset;
                pos = target.position + offset;
                rot = Quaternion.LookRotation(focus - pos, Vector3.up);
            }
            else if (zone.mode == CameraZone.Mode.FixedTracking)
            {
                pos = zone.fixedPoint.position;
                rot = Quaternion.LookRotation(focus - pos, Vector3.up);
            }
            else
            {
                pos = zone.fixedPoint.position;
                rot = zone.fixedPoint.rotation;
            }
        }
    }
}
