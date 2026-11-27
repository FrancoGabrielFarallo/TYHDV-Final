using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MGS2Tanker
{
    /// <summary>
    /// Pistola de Snake con láser.
    ///  - Click derecho: apunta (láser visible, sin cambiar la cámara).
    ///  - Click izquierdo: dispara (30 de daño).
    ///  - R: recarga. No recarga si el cargador está completo o si no quedan cargadores.
    ///  - Con 0 balas no dispara hasta recargar.
    ///  - Empieza con 2 cargadores de 15 (uno ya puesto en el arma, otro en la reserva).
    ///  - Asistencia de apuntado: como en MGS2, si apuntás cerca de un enemigo el disparo corrige la altura
    ///    (así se le puede pegar a las cámaras que están en la pared).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerWeapon : MonoBehaviour
    {
        [Header("Arma")]
        public string weaponName = "USP";
        public float damage = 30f;
        public float range = 30f;
        public float fireCooldown = 0.2f;
        public float reloadTime = 1.0f;

        [Header("Cargadores")]
        public int magazineCapacity = 15;
        [Tooltip("Cargadores completos al empezar / reaparecer.")]
        public int startingMagazines = 2;
        [Tooltip("True: uno de los cargadores iniciales ya está puesto en el arma.")]
        public bool startWithMagazineLoaded = true;
        [Tooltip("Máximo de cargadores en reserva (0 = sin límite).")]
        public int maxMagazinesInPouch = 0;

        [Header("Disparo sin apuntar")]
        [Tooltip("En MGS2 se puede disparar sin apuntar (dispara hacia adelante y frena un instante).")]
        public bool allowHipFire = true;
        public float hipFireLockTime = 0.3f;

        [Header("Asistencia de apuntado")]
        public float autoAimAngle = 5f;

        [Header("Láser")]
        public Transform muzzle;
        public Color laserColor = new Color(1f, 0.05f, 0.05f, 0.9f);
        public float laserWidth = 0.015f;

        int bulletsInMagazine;
        readonly List<int> pouch = new List<int>();
        float cooldown, reloadTimer, lockTimer;

        LineRenderer laser;
        Transform laserDot;
        PlayerController controller;
        PlayerHealth health;

        public int BulletsInMagazine => bulletsInMagazine;
        public int MagazinesInPouch => pouch.Count;
        public bool IsReloading => reloadTimer > 0f;
        public bool MovementLocked => lockTimer > 0f;
        public bool HasAnyAmmo => bulletsInMagazine > 0 || pouch.Count > 0;

        public event Action OnAmmoChanged;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            health = GetComponent<PlayerHealth>();
            if (!muzzle)
            {
                muzzle = new GameObject("Muzzle").transform;
                muzzle.SetParent(transform, false);
                muzzle.localPosition = new Vector3(0.12f, 1.2f, 0.6f);
            }
            CreateLaser();
            ResetAmmo();
        }

        public void ResetAmmo()
        {
            pouch.Clear();
            int mags = startingMagazines;
            bulletsInMagazine = 0;
            if (startWithMagazineLoaded && mags > 0) { bulletsInMagazine = magazineCapacity; mags--; }
            for (int i = 0; i < mags; i++) pouch.Add(magazineCapacity);
            reloadTimer = 0f;
            cooldown = 0f;
            lockTimer = 0f;
            OnAmmoChanged?.Invoke();
        }

        public bool AddMagazines(int count)
        {
            int added = 0;
            for (int i = 0; i < count; i++)
            {
                if (maxMagazinesInPouch > 0 && pouch.Count >= maxMagazinesInPouch) break;
                pouch.Add(magazineCapacity);
                added++;
            }
            if (added > 0)
            {
                Debug.Log($"[Arma] +{added} cargador(es). Reserva: {pouch.Count}");
                OnAmmoChanged?.Invoke();
            }
            return added > 0;
        }

        void Update()
        {
            if (PauseManager.IsPaused || GameInput.Fire == null) return;
            float dt = Time.deltaTime;
            cooldown -= dt;
            lockTimer -= dt;

            if (reloadTimer > 0f)
            {
                reloadTimer -= dt;
                if (reloadTimer <= 0f) FinishReload();
            }

            bool alive = health == null || health.IsAlive;
            bool aiming = alive && controller.IsAiming;
            UpdateLaser(aiming);
            if (!alive) return;

            if (GameInput.Reload.WasPressedThisFrame()) TryReload();
            if (GameInput.Fire.WasPressedThisFrame() && (aiming || allowHipFire)) TryFire(aiming);
        }

        void TryReload()
        {
            if (IsReloading) return;
            if (bulletsInMagazine >= magazineCapacity)
            {
                Debug.Log("[Arma] El cargador está completo: no se puede recargar.");
                return;
            }
            if (pouch.Count == 0)
            {
                Debug.Log("[Arma] No quedan cargadores.");
                return;
            }
            reloadTimer = reloadTime;
            Debug.Log("[Arma] Recargando...");
        }

        void FinishReload()
        {
            reloadTimer = 0f;
            if (pouch.Count == 0) return;

            int best = 0;
            for (int i = 1; i < pouch.Count; i++) if (pouch[i] > pouch[best]) best = i;
            int newMag = pouch[best];
            pouch.RemoveAt(best);

            // El cargador a medio usar se guarda (no se pierden balas).
            if (bulletsInMagazine > 0) pouch.Add(bulletsInMagazine);
            bulletsInMagazine = newMag;

            Debug.Log($"[Arma] Recargado: {bulletsInMagazine}/{magazineCapacity}. Cargadores en reserva: {pouch.Count}");
            OnAmmoChanged?.Invoke();
        }

        void TryFire(bool aiming)
        {
            if (IsReloading || cooldown > 0f) return;
            cooldown = fireCooldown;

            if (bulletsInMagazine <= 0)
            {
                Debug.Log(pouch.Count > 0 ? "[Arma] *click* Cargador vacío: recargá con R." : "[Arma] *click* Sin balas ni cargadores.");
                return;
            }

            bulletsInMagazine--;
            if (!aiming) lockTimer = hipFireLockTime;

            Vector3 origin = RayOrigin;
            Vector3 dir = GetAimDirection();
            Vector3 end = origin + dir * range;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                var target = hit.collider.GetComponentInParent<IDamageable>();
                if (target != null && !(target is PlayerHealth))
                    target.TakeDamage(damage, hit.point, dir, gameObject);
            }

            Tracer.Spawn(muzzle.position, end, new Color(1f, 0.95f, 0.6f, 1f), 0.03f, 0.05f);
            OnAmmoChanged?.Invoke();
        }

        /// <summary>Dirección del disparo: hacia adelante, o hacia el enemigo más alineado (asistencia).</summary>
        /// <summary>
        /// Los raycasts salen del centro del cuerpo a la altura del arma (dentro de la propia cápsula,
        /// que el raycast ignora) para no atravesar paredes cuando Snake está pegado a una.
        /// </summary>
        Vector3 RayOrigin => new Vector3(transform.position.x, muzzle.position.y, transform.position.z);

        Vector3 GetAimDirection()
        {
            Vector3 origin = RayOrigin;
            Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();

            IDamageable best = null;
            float bestAngle = autoAimAngle;

            void Consider(IDamageable d)
            {
                if (!d.IsAlive) return;
                Vector3 to = d.AimPoint - origin;
                float dist = to.magnitude;
                if (dist > range || dist < 0.3f) return;
                Vector3 flat = to; flat.y = 0f;
                float angle = Vector3.Angle(fwd, flat);
                if (angle >= bestAngle) return;
                if (Physics.Raycast(origin, to / dist, out RaycastHit hit, dist + 0.5f, ~0, QueryTriggerInteraction.Ignore) &&
                    !ReferenceEquals(hit.collider.GetComponentInParent<IDamageable>(), d)) return;
                bestAngle = angle;
                best = d;
            }

            foreach (var s in SoldierController.All) Consider(s);
            foreach (var c in SurveillanceCameraController.All) Consider(c);

            return best != null ? (best.AimPoint - origin).normalized : fwd;
        }

        void CreateLaser()
        {
            var go = new GameObject("LaserSight");
            go.transform.SetParent(transform, false);
            laser = go.AddComponent<LineRenderer>();
            laser.useWorldSpace = true;
            laser.positionCount = 2;
            laser.startWidth = laser.endWidth = laserWidth;
            laser.material = RuntimeMaterials.CreateUnlit(laserColor);
            laser.startColor = laser.endColor = laserColor;
            laser.shadowCastingMode = ShadowCastingMode.Off;
            laser.receiveShadows = false;
            laser.enabled = false;

            var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dot.name = "LaserDot";
            Destroy(dot.GetComponent<Collider>());
            dot.transform.SetParent(go.transform, false);
            dot.transform.localScale = Vector3.one * 0.07f;
            var r = dot.GetComponent<Renderer>();
            r.material = RuntimeMaterials.CreateUnlit(laserColor);
            r.shadowCastingMode = ShadowCastingMode.Off;
            laserDot = dot.transform;
            dot.SetActive(false);
        }

        void UpdateLaser(bool aiming)
        {
            if (!aiming)
            {
                if (laser.enabled) { laser.enabled = false; laserDot.gameObject.SetActive(false); }
                return;
            }

            Vector3 origin = RayOrigin;
            Vector3 dir = GetAimDirection();
            Vector3 end = origin + dir * range;
            bool hitSomething = Physics.Raycast(origin, dir, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Ignore);
            if (hitSomething) end = hit.point;

            laser.enabled = true;
            laser.SetPosition(0, muzzle.position);
            laser.SetPosition(1, end);
            laserDot.gameObject.SetActive(hitSomething);
            if (hitSomething) laserDot.position = end - dir * 0.02f;
        }
    }
}
