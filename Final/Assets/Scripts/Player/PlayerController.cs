using UnityEngine;
using UnityEngine.InputSystem;

namespace MGS2Tanker
{
    /// <summary>
    /// Movimiento de Snake (cápsula). Movimiento relativo a la cámara, como en MGS2.
    ///  - Corre a runSpeed.
    ///  - Shift (o B/Círculo, o inclinar poco el stick) = caminar despacio: velocidad -30%.
    ///  - Apuntando (click derecho / LT): no se desplaza, sólo rota sobre su eje Y.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movimiento")]
        [Tooltip("Velocidad de carrera de Snake en m/s (aprox. MGS2).")]
        public float runSpeed = 5.0f;
        [Tooltip("Reducción de velocidad al caminar despacio (0.3 = 30%).")]
        [Range(0f, 1f)] public float slowWalkReduction = 0.30f;
        [Tooltip("Con gamepad, por debajo de esta inclinación del stick camina despacio.")]
        [Range(0f, 1f)] public float analogWalkThreshold = 0.5f;
        public float turnSpeed = 900f;
        public float gravity = -25f;

        [Header("Apuntado")]
        public float aimTurnSpeed = 1080f;
        [Tooltip("Altura del plano donde se proyecta el mouse para apuntar.")]
        public float aimPlaneHeight = 1.2f;
        public Camera viewCamera;

        public bool IsAiming { get; private set; }
        public bool IsSlowWalking { get; private set; }
        public float CurrentSpeed { get; private set; }

        CharacterController cc;
        PlayerHealth health;
        PlayerWeapon weapon;
        float verticalVelocity;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            health = GetComponent<PlayerHealth>();
            weapon = GetComponent<PlayerWeapon>();
        }

        void Start()
        {
            if (!viewCamera) viewCamera = Camera.main;
        }

        void Update()
        {
            GameInput.Tick();
            if (PauseManager.IsPaused || GameInput.Move == null) return;
            if (health && !health.IsAlive) { IsAiming = false; CurrentSpeed = 0f; return; }

            Vector2 move = Vector2.ClampMagnitude(GameInput.Move.ReadValue<Vector2>(), 1f);
            IsAiming = GameInput.Aim.IsPressed();

            Vector3 horizontal = Vector3.zero;
            bool locked = IsAiming || (weapon && weapon.MovementLocked);

            if (locked)
            {
                CurrentSpeed = 0f;
                if (IsAiming) UpdateAimRotation(move);
            }
            else
            {
                Vector3 wish = CameraRelative(move);
                if (wish.sqrMagnitude > 0.0001f)
                {
                    bool analogSlow = GameInput.UsingGamepad && move.magnitude < analogWalkThreshold;
                    IsSlowWalking = GameInput.Walk.IsPressed() || analogSlow;
                    float speed = runSpeed * (IsSlowWalking ? 1f - slowWalkReduction : 1f);
                    horizontal = wish.normalized * speed;
                    CurrentSpeed = speed;
                    RotateTowards(wish, turnSpeed);
                }
                else
                {
                    CurrentSpeed = 0f;
                    IsSlowWalking = false;
                }
            }

            if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            else verticalVelocity += gravity * Time.deltaTime;

            cc.Move((horizontal + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        void UpdateAimRotation(Vector2 move)
        {
            if (GameInput.UsingGamepad)
            {
                Vector2 look = GameInput.Look.ReadValue<Vector2>();
                Vector2 stick = look.sqrMagnitude > 0.04f ? look : move;
                if (stick.sqrMagnitude > 0.04f) RotateTowards(CameraRelative(stick), aimTurnSpeed);
                return;
            }

            if (Mouse.current == null || !viewCamera) return;
            Ray ray = viewCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            Plane plane = new Plane(Vector3.up, transform.position + Vector3.up * aimPlaneHeight);
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 dir = ray.GetPoint(enter) - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f) RotateTowards(dir, aimTurnSpeed);
            }
        }

        Vector3 CameraRelative(Vector2 input)
        {
            if (!viewCamera) return new Vector3(input.x, 0f, input.y);
            Vector3 f = viewCamera.transform.forward; f.y = 0f;
            if (f.sqrMagnitude < 0.01f) { f = viewCamera.transform.up; f.y = 0f; }
            f.Normalize();
            Vector3 r = viewCamera.transform.right; r.y = 0f; r.Normalize();
            return f * input.y + r * input.x;
        }

        void RotateTowards(Vector3 dir, float speed)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f) return;
            Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, speed * Time.deltaTime);
        }

        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            cc.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            cc.enabled = true;
            verticalVelocity = 0f;
            IsAiming = false;
        }
    }
}
