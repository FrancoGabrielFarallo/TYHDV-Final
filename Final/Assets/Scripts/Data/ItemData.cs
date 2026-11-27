using UnityEngine;

namespace MGS2Tanker
{
    /// <summary>Base de los ítems que el jugador puede recoger.</summary>
    public abstract class ItemData : ScriptableObject
    {
        public string displayName = "ITEM";
        public Color color = Color.white;

        /// <summary>Aplica el ítem. Devuelve true si se consumió (si no, queda en el piso).</summary>
        public abstract bool Apply(GameObject player);

        public virtual string PickupMessage => displayName;
    }
}
