using UnityEngine;

namespace MGS2Tanker
{
    /// <summary>Tipo de enemigo "Soldier" (guardia del Tanker).</summary>
    [CreateAssetMenu(fileName = "Soldier", menuName = "MGS2 Tanker/Enemigos/Soldier")]
    public class Soldier : EnemyType
    {
        [Header("Movimiento (m/s, grados/s)")]
        public float patrolSpeed = 1.5f;
        public float chaseSpeed = 4.0f;
        public float searchSpeed = 2.2f;
        public float turnSpeed = 320f;
        public float waypointReachDistance = 0.2f;
        [Tooltip("Segundos que espera en cada punto de patrulla.")]
        public float waitAtWaypoint = 2f;
        [Tooltip("Al esperar, mira hacia el forward del punto de patrulla.")]
        public bool faceWaypointDirection = true;

        [Header("Persecución")]
        [Tooltip("Distancia a la que mantiene línea de vista mientras persigue (fuera del cono).")]
        public float chaseSightRange = 12f;
        public float stoppingDistance = 4f;
        [Tooltip("Persecución sin alerta: segundos sin ver al jugador antes de buscarlo.")]
        public float loseSightTime = 6f;
        public float searchDuration = 8f;

        [Header("Arma")]
        public float damagePerShot = 6f;
        public float attackRange = 8f;
        public int burstCount = 3;
        public float timeBetweenShots = 0.12f;
        public float burstCooldown = 1.1f;
        [Range(0f, 1f)] public float accuracy = 0.55f;
        [Tooltip("Demora antes del primer disparo al empezar la persecución.")]
        public float reactionTime = 0.5f;

        [Header("Alerta")]
        [Tooltip("Si se le dispara y no muere en este tiempo, da la alerta.")]
        public float alertAfterShotDelay = 3f;

        public Soldier()
        {
            displayName = "Soldier";
            maxHealth = 100f;
            viewAngle = 40f;
            viewRange = 5f;
        }
    }
}
