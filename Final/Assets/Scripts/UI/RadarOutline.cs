using UnityEngine;

namespace MGS2Tanker
{
    /// <summary>
    /// Marca una pieza de geometría (con BoxCollider) para que se dibuje su contorno en el radar.
    /// Las piezas finas se dibujan como una sola línea.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class RadarOutline : MonoBehaviour { }
}
