using UnityEngine;

namespace MGS2Tanker
{
    /// <summary>Ítem en el piso. Se recoge al pasar por encima (como en MGS2).</summary>
    public class ItemPickup : MonoBehaviour
    {
        public ItemData item;
        public float pickupRadius = 0.9f;
        public float spinSpeed = 90f;

        Transform visual;
        bool warnedFull;

        void Start()
        {
            visual = transform.childCount > 0 ? transform.GetChild(0) : transform;
            if (item)
            {
                foreach (var r in GetComponentsInChildren<Renderer>()) r.material.color = item.color;
                name = $"Item_{item.displayName}";
            }
        }

        void Update()
        {
            if (PauseManager.IsPaused) return;
            visual.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);

            var player = PlayerHealth.Current;
            if (!player || !player.IsAlive || !item) return;

            Vector3 d = player.transform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > pickupRadius * pickupRadius) { warnedFull = false; return; }

            if (item.Apply(player.gameObject))
            {
                Debug.Log($"[Item] Recogido: {item.PickupMessage}");
                if (HUD.Instance) HUD.Instance.ShowMessage(item.PickupMessage);
                gameObject.SetActive(false);
            }
            else if (!warnedFull)
            {
                warnedFull = true;
                if (HUD.Instance) HUD.Instance.ShowMessage($"{item.displayName}  (FULL)");
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = item ? item.color : Color.white;
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
    }
}
