// 职责：水源，交互补充口渴值
// 依赖：PlayerStats
using UnityEngine;

// A water pool: interact to refill thirst. Also the thing that makes the
// "water bar drains faster than hunger" design survivable.
public class WaterSource : MonoBehaviour
{
    public string displayName = "Water";
    public float refillAmount = 40f;

    public void Drink()
    {
        if (PlayerStats.Instance == null) return;
        if (PlayerStats.Instance.thirst >= PlayerStats.Instance.maxThirst - 0.5f)
        {
            Debug.Log("[Water] already full");
            return;
        }
        PlayerStats.Instance.Drink(refillAmount);
    }
}
