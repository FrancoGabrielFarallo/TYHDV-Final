using System;
using System.Collections;
using UnityEngine;

namespace MGS2Tanker
{
    /// <summary>Vida del jugador. Muestra en consola cada daño recibido.</summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        public static PlayerHealth Current { get; private set; }

        public float maxHealth = 100f;
        [SerializeField] float currentHealth;

        public float CurrentHealth => currentHealth;
        public bool IsAlive => currentHealth > 0f;
        public Vector3 AimPoint => transform.position + Vector3.up * 1.2f;
        public Vector3 HeadPoint => transform.position + Vector3.up * 1.6f;

        public event Action OnDied;
        public event Action<float> OnDamaged;

        Transform visual;
        Vector3 visualPos;
        Quaternion visualRot;
        Renderer[] renderers;
        Color[] baseColors;

        void Awake()
        {
            Current = this;
            currentHealth = maxHealth;
            visual = transform.Find("Visual");
            if (visual) { visualPos = visual.localPosition; visualRot = visual.localRotation; }
            renderers = GetComponentsInChildren<Renderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].material.color;
        }

        void OnEnable() { Current = this; }
        void OnDisable() { if (Current == this) Current = null; }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection, GameObject source)
        {
            if (!IsAlive || amount <= 0f) return;
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            Debug.Log($"[Jugador] Recibió {amount:0.#} de daño de {(source ? source.name : "desconocido")}. Vida: {currentHealth:0.#}/{maxHealth:0}");
            OnDamaged?.Invoke(amount);
            StopAllCoroutines();
            StartCoroutine(Flash());
            if (currentHealth <= 0f) Die();
        }

        public bool Heal(float amount)
        {
            if (!IsAlive || currentHealth >= maxHealth) return false;
            float before = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            Debug.Log($"[Jugador] Recuperó {currentHealth - before:0.#} de vida. Vida: {currentHealth:0.#}/{maxHealth:0}");
            return true;
        }

        void Die()
        {
            Debug.Log("[Jugador] ¡Snake murió! GAME OVER  ->  F1: reaparecer  |  F2: reiniciar escena");
            if (visual)
            {
                visual.localRotation = Quaternion.Euler(90f, 0f, 0f);
                visual.localPosition = new Vector3(0f, 0.35f, 0f);
            }
            OnDied?.Invoke();
        }

        /// <summary>Vuelve a los valores iniciales.</summary>
        public void ResetState()
        {
            StopAllCoroutines();
            currentHealth = maxHealth;
            if (visual) { visual.localPosition = visualPos; visual.localRotation = visualRot; }
            for (int i = 0; i < renderers.Length; i++) renderers[i].material.color = baseColors[i];
        }

        IEnumerator Flash()
        {
            for (int i = 0; i < renderers.Length; i++) renderers[i].material.color = Color.red;
            yield return new WaitForSeconds(0.08f);
            for (int i = 0; i < renderers.Length; i++) renderers[i].material.color = baseColors[i];
        }
    }
}
