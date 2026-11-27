using System.Collections.Generic;
using UnityEngine;

namespace MGS2Tanker
{
    /// <summary>
    /// Sala del nivel (Aft Deck, Deck-A Crew's Quarters, Crew's Lounge, Navigational Deck...).
    /// Se define con uno o más BoxCollider (trigger) en este objeto o en sus hijos.
    /// La alerta sólo afecta a los soldados que están en la misma sala que el jugador.
    /// </summary>
    public class Room : MonoBehaviour
    {
        public string displayName = "Room";
        public Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.15f);

        static readonly List<Room> all = new List<Room>();
        public static IReadOnlyList<Room> All => all;

        BoxCollider[] volumes;

        void Awake() => CacheVolumes();

        void CacheVolumes()
        {
            volumes = GetComponentsInChildren<BoxCollider>();
            foreach (var v in volumes) v.isTrigger = true;
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public bool Contains(Vector3 worldPoint)
        {
            if (volumes == null) CacheVolumes();
            foreach (var v in volumes)
                if (BoxVolume.Contains(v, worldPoint)) return true;
            return false;
        }

        /// <summary>Devuelve la sala que contiene el punto (o null).</summary>
        public static Room Find(Vector3 worldPoint)
        {
            foreach (var r in all)
                if (r.Contains(worldPoint)) return r;
            return null;
        }

        void OnDrawGizmos()
        {
            foreach (var v in GetComponentsInChildren<BoxCollider>())
            {
                Gizmos.matrix = v.transform.localToWorldMatrix;
                Gizmos.color = gizmoColor;
                Gizmos.DrawCube(v.center, v.size);
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.8f);
                Gizmos.DrawWireCube(v.center, v.size);
            }
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
