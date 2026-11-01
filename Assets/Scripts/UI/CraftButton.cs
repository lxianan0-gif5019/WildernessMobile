using UnityEngine;
using UnityEngine.EventSystems;

// One crafting row. Costs are hard-coded for the prototype.
public class CraftButton : MonoBehaviour, IPointerDownHandler
{
    public string recipeName;
    public int craftRepeat = 1;

    public void OnPointerDown(PointerEventData eventData)
    {
        var inv = Inventory.Instance;
        if (inv == null) return;

        for (int i = 0; i < craftRepeat; i++)
        {
            switch (recipeName)
            {
                case "Plank":
                    if (!inv.Spend("wood", 3)) return;
                    inv.Add("plank", 2);
                    break;
                case "Wall":
                    if (!inv.Spend("plank", 2)) return;
                    inv.Add("wall", 1);
                    break;
                default:
                    return;
            }
        }
    }
}
