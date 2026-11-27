using UnityEngine;

namespace MGS2Tanker
{
    /// <summary>Base de todos los tipos de enemigo (ScriptableObject).</summary>
    public abstract class EnemyType : ScriptableObject
    {
        public string displayName = "Enemy";
        [Header("Vida")]
        public float maxHealth = 100f;
        [Header("Visión")]
        [Tooltip("Apertura total del cono en grados.")]
        [Range(1f, 180f)] public float viewAngle = 40f;
        [Tooltip("Alcance en metros.")]
        public float viewRange = 5f;
    }
}
