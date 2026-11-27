using UnityEngine;

namespace MGS2Tanker
{
    /// <summary>MediKit: recupera vida. Si la vida está llena no se consume.</summary>
    [CreateAssetMenu(fileName = "Item_Medikit", menuName = "MGS2 Tanker/Items/Medikit")]
    public class MedikitItem : ItemData
    {
        public float healAmount = 50f;

        public MedikitItem()
        {
            displayName = "MEDIKIT";
            color = new Color(0.2f, 0.9f, 0.4f);
        }

        public override string PickupMessage => $"{displayName}  +{healAmount:0} LIFE";

        public override bool Apply(GameObject player)
        {
            var health = player.GetComponent<PlayerHealth>();
            return health != null && health.Heal(healAmount);
        }
    }
}
