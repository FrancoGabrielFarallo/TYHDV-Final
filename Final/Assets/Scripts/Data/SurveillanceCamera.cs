using UnityEngine;

namespace MGS2Tanker
{
    /// <summary>Tipo de enemigo "Surveillance Camera" (cámara de seguridad).</summary>
    [CreateAssetMenu(fileName = "SurveillanceCamera", menuName = "MGS2 Tanker/Enemigos/Surveillance Camera")]
    public class SurveillanceCamera : EnemyType
    {
        [Header("Barrido")]
        [Tooltip("Barrido total en grados (de lado a lado).")]
        public float sweepAngle = 90f;
        public float sweepSpeed = 25f;
        public float pauseAtEnds = 1.5f;
        [Tooltip("Al detectar al jugador lo sigue con la cabeza.")]
        public bool trackPlayerWhenSpotted = true;
        public float trackSpeed = 60f;

        public SurveillanceCamera()
        {
            displayName = "Surveillance Camera";
            maxHealth = 10f;
            viewAngle = 40f;
            viewRange = 5f;
        }
    }
}
