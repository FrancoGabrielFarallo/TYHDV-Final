using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MGS2Tanker
{
    /// <summary>
    /// Guardia "Soldier".
    /// Estados: Patrol -> (ve al jugador) -> Chase + ALERTA de la sala.
    ///          Si le disparan: Chase inmediato; si no muere en 3 s, da la ALERTA.
    ///          ALERT termina -> Search (EVASION) -> Patrol.
    /// Sólo dispara en Chase (lo detectó, o le dispararon).
    /// Todo el desplazamiento es por vectores: dirección = (destino - posición).normalized; velocidad = dirección * rapidez.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(VisionSensor))]
    public class SoldierController : MonoBehaviour, IDamageable
    {
        public enum State { Patrol, Chase, Search, Dead }

        static readonly List<SoldierController> all = new List<SoldierController>();
        public static IReadOnlyList<SoldierController> All => all;

        [Header("Configuración")]
        public Soldier type;
        public PatrolRoute route;
        public Transform muzzle;

        [Header("Debug (solo lectura)")]
        [SerializeField] State state = State.Patrol;
        [SerializeField] float health;
        [SerializeField] bool alerted;

        public State CurrentState => state;
        public bool IsAlerted => alerted;
        public bool IsAlive => state != State.Dead;
        public float Health => health;
        public Vector3 AimPoint => transform.position + Vector3.up * 1.25f;
        public VisionSensor Vision => sensor;

        CharacterController cc;
        VisionSensor sensor;
        Renderer[] renderers;
        Color[] baseColors;
        PlayerTrail cachedTrail;

        int patrolIndex;
        int patrolDir = 1;
        bool waiting;
        float waitTimer;

        float verticalVelocity;
        Vector3 frameVelocity;

        Vector3 lastKnownPos;
        float timeSinceSeen;
        float fireTimer;
        int shotsInBurst;
        float shotAlertTimer = -1f;
        Vector3 chaseGoal;
        float repathTimer;

        Vector3 searchPoint;
        float searchTimer;
        bool searchIsEvasion;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            sensor = GetComponent<VisionSensor>();
            if (type == null)
            {
                Debug.LogWarning($"[{name}] No tiene asignado un Soldier (ScriptableObject). Se usan valores por defecto.");
                type = ScriptableObject.CreateInstance<Soldier>();
            }
            health = type.maxHealth;
            sensor.viewAngle = type.viewAngle;
            sensor.viewRange = type.viewRange;
            sensor.flatCheck = true;
            if (!muzzle) muzzle = transform;

            var visual = transform.Find("Visual");
            renderers = (visual ? visual : transform).GetComponentsInChildren<Renderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].material.color;
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        void Start()
        {
            if (route && route.Count > 0) patrolIndex = route.ClosestIndex(transform.position);
        }

        void Update()
        {
            if (state == State.Dead || PauseManager.IsPaused) return;
            float dt = Time.deltaTime;
            frameVelocity = Vector3.zero;

            var player = PlayerHealth.Current;
            bool playerAlive = player != null && player.IsAlive;
            bool seen = playerAlive && sensor.Sense(player);

            // Herido: si sobrevive 3 s, pide refuerzos -> ALERTA
            if (shotAlertTimer > 0f)
            {
                shotAlertTimer -= dt;
                if (shotAlertTimer <= 0f)
                {
                    shotAlertTimer = -1f;
                    if (playerAlive)
                    {
                        Debug.Log($"[{name}] Sobrevivió {type.alertAfterShotDelay:0} s al disparo: ¡pide refuerzos!");
                        RaiseAlert(player.transform.position);
                    }
                }
            }

            switch (state)
            {
                case State.Patrol:
                    if (seen) OnPlayerDetected(player); else UpdatePatrol(dt);
                    break;
                case State.Search:
                    if (seen) OnPlayerDetected(player); else UpdateSearch(dt);
                    break;
                case State.Chase:
                    UpdateChase(player, playerAlive, seen, dt);
                    break;
            }

            ApplyMovement(dt);
        }

        // ---------------------------------------------------------------- Estados

        void UpdatePatrol(float dt)
        {
            if (route == null || route.Count == 0) return;   // guardia fijo

            if (waiting)
            {
                waitTimer -= dt;
                if (type.faceWaypointDirection) FaceDirection(route.GetTransform(patrolIndex).forward, dt);
                if (waitTimer <= 0f)
                {
                    waiting = false;
                    patrolIndex = route.Next(patrolIndex, ref patrolDir);
                }
                return;
            }

            if (MoveTowards(route.GetPoint(patrolIndex), type.patrolSpeed, type.waypointReachDistance))
            {
                float wait = route.waitTime >= 0f ? route.waitTime : type.waitAtWaypoint;
                if (wait > 0f) { waiting = true; waitTimer = wait; }
                else patrolIndex = route.Next(patrolIndex, ref patrolDir);
            }
        }

        void UpdateChase(PlayerHealth player, bool playerAlive, bool seenInCone, float dt)
        {
            if (!playerAlive) return;   // el jugador murió: se queda quieto vigilando

            Vector3 playerPos = player.transform.position;
            float dist = FlatDistance(playerPos);
            bool los = dist <= type.chaseSightRange && (seenInCone || sensor.HasLineOfSight(player.AimPoint, player));

            if (los)
            {
                lastKnownPos = playerPos;
                timeSinceSeen = 0f;
                if (alerted)
                {
                    if (AlertManager.Instance) AlertManager.Instance.ReportPlayerSeen(playerPos);
                }
                else if (seenInCone && shotAlertTimer < 0f)
                {
                    // Lo descubrió con su cono de visión (y no está esperando los 3 s del disparo) -> ALERTA
                    RaiseAlert(playerPos);
                }
            }
            else
            {
                timeSinceSeen += dt;
                if (alerted && AlertManager.Instance) lastKnownPos = AlertManager.Instance.LastKnownPlayerPos;
            }

            if (los)
            {
                if (dist > type.stoppingDistance)
                {
                    if (HasClearPath(playerPos)) MoveTowards(playerPos, type.chaseSpeed, type.stoppingDistance);
                    else FollowTrail(player, dt);
                }
                FaceTowards(playerPos, dt);
                TryShoot(player, dist, dt);
            }
            else
            {
                fireTimer = Mathf.Max(fireTimer, type.reactionTime * 0.5f);
                bool reached = false;
                if (HasClearPath(lastKnownPos)) reached = MoveTowards(lastKnownPos, type.chaseSpeed, 0.4f);
                else FollowTrail(player, dt);
                if (reached) LookAround(dt);

                if (!alerted && timeSinceSeen > type.loseSightTime)
                {
                    Debug.Log($"[{name}] Perdió de vista al jugador. Buscando...");
                    StartSearch(lastKnownPos, false);
                }
            }
        }

        void UpdateSearch(float dt)
        {
            if (MoveTowards(searchPoint, type.searchSpeed, 0.5f)) LookAround(dt);
            if (searchIsEvasion) return;   // en EVASION termina cuando lo dice el AlertManager
            searchTimer -= dt;
            if (searchTimer <= 0f)
            {
                Debug.Log($"[{name}] No encontró nada. Vuelve a patrullar.");
                ReturnToPatrol();
            }
        }

        // ---------------------------------------------------------------- Transiciones (llamadas también por AlertManager)

        void OnPlayerDetected(PlayerHealth player)
        {
            Debug.Log($"[{name}] ¡Intruso a la vista! (dentro de {type.viewAngle:0}° y {type.viewRange:0.#} m)");
            lastKnownPos = player.transform.position;
            EnterChase();
            RaiseAlert(lastKnownPos);
        }

        void RaiseAlert(Vector3 playerPos)
        {
            if (AlertManager.Instance) AlertManager.Instance.RaiseAlert(playerPos, name);
            else EnterAlert(playerPos);
        }

        void EnterChase()
        {
            if (state == State.Chase) return;
            state = State.Chase;
            waiting = false;
            timeSinceSeen = 0f;
            fireTimer = type.reactionTime;
            shotsInBurst = 0;
            repathTimer = 0f;
        }

        public void EnterAlert(Vector3 playerPos)
        {
            if (!IsAlive) return;
            alerted = true;
            shotAlertTimer = -1f;
            lastKnownPos = playerPos;
            EnterChase();
        }

        public void EnterEvasion(Vector3 lastKnown)
        {
            if (!IsAlive) return;
            alerted = false;
            StartSearch(lastKnown, true);
        }

        public void OnEvasionEnded()
        {
            if (state == State.Search && searchIsEvasion) ReturnToPatrol();
        }

        void StartSearch(Vector3 point, bool evasion)
        {
            state = State.Search;
            searchPoint = point;
            searchTimer = type.searchDuration;
            searchIsEvasion = evasion;
        }

        public void ReturnToPatrol()
        {
            if (!IsAlive) return;
            alerted = false;
            shotAlertTimer = -1f;
            state = State.Patrol;
            waiting = false;
            if (route && route.Count > 0) patrolIndex = route.ClosestIndex(transform.position);
        }

        // ---------------------------------------------------------------- Movimiento por vectores

        /// <summary>Calcula la velocidad hacia un punto. Devuelve true si ya llegó.</summary>
        bool MoveTowards(Vector3 point, float speed, float stopDistance)
        {
            Vector3 toTarget = point - transform.position;   // vector posición -> destino
            toTarget.y = 0f;
            float distance = toTarget.magnitude;             // módulo = distancia
            if (distance <= stopDistance) return true;

            Vector3 direction = toTarget / distance;         // vector unitario
            float maxSpeed = (distance - stopDistance) / Mathf.Max(Time.deltaTime, 1e-5f);
            frameVelocity = direction * Mathf.Min(speed, maxSpeed);   // velocidad = dirección * rapidez
            FaceDirection(direction, Time.deltaTime);
            return false;
        }

        void FollowTrail(PlayerHealth player, float dt)
        {
            repathTimer -= dt;
            if (repathTimer <= 0f)
            {
                repathTimer = 0.25f;
                if (!cachedTrail) cachedTrail = player.GetComponent<PlayerTrail>();
                if (cachedTrail && cachedTrail.TryGetChasePoint(HasClearPath, out Vector3 p)) chaseGoal = p;
                else chaseGoal = lastKnownPos;
            }
            MoveTowards(chaseGoal, type.chaseSpeed, 0.3f);
        }

        bool HasClearPath(Vector3 target)
        {
            Vector3 origin = transform.position + Vector3.up * 0.9f;
            Vector3 to = target - transform.position;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist < 0.1f) return true;
            if (Physics.SphereCast(origin, 0.25f, to / dist, out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore))
                return hit.collider.GetComponentInParent<PlayerHealth>() != null;
            return true;
        }

        void ApplyMovement(float dt)
        {
            if (!cc.enabled) return;
            if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            else verticalVelocity += -25f * dt;
            cc.Move((frameVelocity + Vector3.up * verticalVelocity) * dt);
        }

        void FaceTowards(Vector3 point, float dt) => FaceDirection(point - transform.position, dt);

        void FaceDirection(Vector3 dir, float dt)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f) return;
            Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, type.turnSpeed * dt);
        }

        void LookAround(float dt) => transform.Rotate(0f, 90f * dt, 0f);

        float FlatDistance(Vector3 p)
        {
            Vector3 d = p - transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        // ---------------------------------------------------------------- Ataque

        void TryShoot(PlayerHealth player, float dist, float dt)
        {
            if (dist > type.attackRange) return;
            Vector3 to = player.transform.position - transform.position;
            to.y = 0f;
            if (Vector3.Angle(transform.forward, to) > 15f) return;

            fireTimer -= dt;
            if (fireTimer > 0f) return;

            FireShot(player);
            shotsInBurst++;
            if (shotsInBurst >= type.burstCount) { shotsInBurst = 0; fireTimer = type.burstCooldown; }
            else fireTimer = type.timeBetweenShots;
        }

        void FireShot(PlayerHealth player)
        {
            // El raycast sale del centro del cuerpo (ignorado por estar dentro de su propia cápsula).
            Vector3 origin = transform.position + Vector3.up * muzzle.localPosition.y;
            Vector3 aim = player.AimPoint;
            if (Random.value > type.accuracy)
            {
                Vector3 side = Vector3.Cross(Vector3.up, (aim - origin).normalized);
                aim += side * Random.Range(0.6f, 1.2f) * (Random.value < 0.5f ? -1f : 1f) + Vector3.up * Random.Range(-0.3f, 0.5f);
            }

            Vector3 dir = (aim - origin).normalized;
            float maxDist = type.attackRange + 4f;
            Vector3 end = origin + dir * maxDist;
            if (Physics.Raycast(origin, dir, out RaycastHit hit, maxDist, ~0, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                var ph = hit.collider.GetComponentInParent<PlayerHealth>();
                if (ph) ph.TakeDamage(type.damagePerShot, hit.point, dir, gameObject);
            }
            Tracer.Spawn(muzzle.position, end, new Color(1f, 0.6f, 0.15f, 1f), 0.025f, 0.05f);
        }

        // ---------------------------------------------------------------- Daño

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection, GameObject source)
        {
            if (!IsAlive) return;
            health = Mathf.Max(0f, health - amount);
            Debug.Log($"[{name}] Impacto: -{amount:0.#} de vida ({health:0.#}/{type.maxHealth:0}).");
            StartCoroutine(Flash());

            if (health <= 0f) { Die(); return; }

            var shooter = source ? source.GetComponentInParent<PlayerHealth>() : null;
            if (!shooter) return;

            lastKnownPos = shooter.transform.position;
            Vector3 back = -hitDirection;
            back.y = 0f;
            if (back.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(back.normalized, Vector3.up);
            EnterChase();

            bool alertActive = AlertManager.Instance && AlertManager.Instance.Phase == AlertPhase.Alert;
            if (!alerted && !alertActive && shotAlertTimer < 0f)
            {
                shotAlertTimer = type.alertAfterShotDelay;
                Debug.Log($"[{name}] Herido. Si no muere en {type.alertAfterShotDelay:0} s, dará la ALERTA.");
            }
        }

        void Die()
        {
            state = State.Dead;
            alerted = false;
            shotAlertTimer = -1f;
            cc.enabled = false;
            sensor.enabled = false;

            var visual = transform.Find("Visual");
            if (visual)
            {
                visual.localRotation = Quaternion.Euler(90f, 0f, 0f);
                visual.localPosition = new Vector3(0f, 0.35f, 0f);
            }
            for (int i = 0; i < renderers.Length; i++) baseColors[i] *= 0.4f;
            Debug.Log($"[{name}] Neutralizado.");
        }

        IEnumerator Flash()
        {
            for (int i = 0; i < renderers.Length; i++) renderers[i].material.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            for (int i = 0; i < renderers.Length; i++) renderers[i].material.color = baseColors[i];
        }
    }
}
