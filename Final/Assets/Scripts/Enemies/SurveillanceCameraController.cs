using System.Collections.Generic;
using UnityEngine;

namespace MGS2Tanker
{
    /// <summary>
    /// Cámara de seguridad. Barre de lado a lado; si ve al jugador dispara la ALERTA de la sala.
    /// Tiene 10 de vida: un disparo la destruye.
    /// Jerarquía esperada: Root (en la pared) -> yawPivot (gira) -> head (inclinada hacia abajo, con el ojo).
    /// </summary>
    [RequireComponent(typeof(VisionSensor))]
    public class SurveillanceCameraController : MonoBehaviour, IDamageable
    {
        static readonly List<SurveillanceCameraController> all = new List<SurveillanceCameraController>();
        public static IReadOnlyList<SurveillanceCameraController> All => all;

        public SurveillanceCamera type;
        public Transform yawPivot;
        public Transform head;

        [Header("Debug (solo lectura)")]
        [SerializeField] float health;
        [SerializeField] bool destroyed;

        VisionSensor sensor;
        Renderer[] renderers;
        float baseYaw, offset, pauseTimer;
        int sweepDir = 1;
        bool spotted;

        public bool IsAlive => !destroyed;
        public Vector3 AimPoint => head ? head.position : transform.position;
        public VisionSensor Vision => sensor;

        void Awake()
        {
            sensor = GetComponent<VisionSensor>();
            if (type == null)
            {
                Debug.LogWarning($"[{name}] No tiene asignado un SurveillanceCamera (ScriptableObject). Se usan valores por defecto.");
                type = ScriptableObject.CreateInstance<SurveillanceCamera>();
            }
            health = type.maxHealth;
            sensor.viewAngle = type.viewAngle;
            sensor.viewRange = type.viewRange;
            sensor.flatCheck = false;
            if (!yawPivot) yawPivot = transform;
            if (!head) head = yawPivot;
            baseYaw = yawPivot.localEulerAngles.y;
            renderers = GetComponentsInChildren<Renderer>();
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        void Update()
        {
            if (destroyed || PauseManager.IsPaused) return;
            float dt = Time.deltaTime;

            var player = PlayerHealth.Current;
            bool seen = player != null && player.IsAlive && sensor.Sense(player);

            if (seen)
            {
                if (!spotted) Debug.Log($"[{name}] ¡La cámara de seguridad detectó al intruso!");
                spotted = true;
                if (AlertManager.Instance) AlertManager.Instance.RaiseAlert(player.transform.position, name);
                if (type.trackPlayerWhenSpotted) Track(player.transform.position, dt);
            }
            else
            {
                spotted = false;
                Sweep(dt);
            }
        }

        void Sweep(float dt)
        {
            if (pauseTimer > 0f) { pauseTimer -= dt; return; }
            float half = type.sweepAngle * 0.5f;
            offset += sweepDir * type.sweepSpeed * dt;
            if (offset >= half) { offset = half; sweepDir = -1; pauseTimer = type.pauseAtEnds; }
            else if (offset <= -half) { offset = -half; sweepDir = 1; pauseTimer = type.pauseAtEnds; }
            ApplyYaw();
        }

        void Track(Vector3 target, float dt)
        {
            Vector3 to = target - yawPivot.position;
            if (yawPivot.parent) to = yawPivot.parent.InverseTransformDirection(to);
            float targetYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            float desired = Mathf.Clamp(Mathf.DeltaAngle(baseYaw, targetYaw), -type.sweepAngle * 0.5f, type.sweepAngle * 0.5f);
            offset = Mathf.MoveTowards(offset, desired, type.trackSpeed * dt);
            ApplyYaw();
        }

        void ApplyYaw() => yawPivot.localRotation = Quaternion.Euler(0f, baseYaw + offset, 0f);

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection, GameObject source)
        {
            if (destroyed) return;
            health = Mathf.Max(0f, health - amount);
            Debug.Log($"[{name}] Impacto: -{amount:0.#} ({health:0.#}/{type.maxHealth:0}).");
            if (health > 0f) return;

            destroyed = true;
            sensor.enabled = false;
            if (head) head.localRotation *= Quaternion.Euler(35f, 0f, 15f);
            foreach (var r in renderers) r.material.color = new Color(0.12f, 0.12f, 0.12f);
            Debug.Log($"[{name}] Cámara de seguridad destruida.");
        }
    }
}
