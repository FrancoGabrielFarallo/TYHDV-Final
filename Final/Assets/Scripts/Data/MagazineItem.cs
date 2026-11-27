using UnityEngine;

namespace MGS2Tanker
{
    /// <summary>Cargador: se suma a los cargadores que tiene el jugador.</summary>
    [CreateAssetMenu(fileName = "Item_Magazine", menuName = "MGS2 Tanker/Items/Magazine")]
    public class MagazineItem : ItemData
    {
        [Min(1)] public int magazines = 1;

        public MagazineItem()
        {
            displayName = "MAGAZINE";
            color = new Color(0.95f, 0.8f, 0.2f);
        }

        public override string PickupMessage => $"{displayName}  +{magazines}";

        public override bool Apply(GameObject player)
        {
            var weapon = player.GetComponent<PlayerWeapon>();
            return weapon != null && weapon.AddMagazines(magazines);
        }
    }
}
