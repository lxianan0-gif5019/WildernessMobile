using UnityEngine;

// A structure the player placed. Can be picked back up into the inventory.
public class PlacedStructure : MonoBehaviour
{
    public string itemId = "wall";
    public string displayName = "Wall";

    public void PickUp()
    {
        if (Inventory.Instance != null) Inventory.Instance.Add(itemId, 1);
        Debug.Log("[Build] picked up " + displayName);
        Destroy(gameObject);
    }
}
