using UnityEngine;

namespace MGS2Tanker
{
    /// <summary>Todo lo que puede recibir disparos: jugador, soldados, cámaras de seguridad.</summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        /// <summary>Punto al que se apunta (pecho del soldado, cuerpo de la cámara...).</summary>
        Vector3 AimPoint { get; }
        void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection, GameObject source);
    }

    /// <summary>Utilidad: ¿un punto está dentro de un BoxCollider (respetando rotación y escala)?</summary>
    public static class BoxVolume
    {
        public static bool Contains(BoxCollider box, Vector3 worldPoint)
        {
            if (box == null) return false;
            Vector3 local = box.transform.InverseTransformPoint(worldPoint) - box.center;
            Vector3 half = box.size * 0.5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }
    }

    /// <summary>Materiales simples creados en runtime (láser, trazadoras).</summary>
    public static class RuntimeMaterials
    {
        static Shader unlit;

        public static Material CreateUnlit(Color color)
        {
            if (unlit == null)
            {
                string[] candidates = { "Sprites/Default", "Universal Render Pipeline/Unlit", "Unlit/Color", "Hidden/Internal-Colored" };
                foreach (var s in candidates)
                {
                    unlit = Shader.Find(s);
                    if (unlit != null) break;
                }
            }
            var m = new Material(unlit) { color = color };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            return m;
        }
    }
}
