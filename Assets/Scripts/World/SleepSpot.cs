using UnityEngine;

// A place to sleep: a hay nest or a wooden bed. Sleeping skips to morning, but
// only at night and only when no wolf is close.
public class SleepSpot : MonoBehaviour
{
    public string kind = "hay_bed";
    public float safeRadius = 18f;

    public bool CanSleep(out string reason)
    {
        if (DayNightCycle.Instance == null) { reason = "no day cycle"; return false; }
        if (!DayNightCycle.Instance.IsNight) { reason = "you can only sleep at night"; return false; }

        var wolves = FindObjectsOfType<Wolf>();
        for (int i = 0; i < wolves.Length; i++)
        {
            if (wolves[i] == null || wolves[i].Dead) continue;
            if (Vector3.Distance(wolves[i].transform.position, transform.position) < safeRadius)
            {
                reason = "wolves are too close to sleep";
                return false;
            }
        }

        reason = "";
        return true;
    }

    public bool Sleep(out string reason)
    {
        if (!CanSleep(out reason)) return false;

        var cycle = DayNightCycle.Instance;
        cycle.timeOfDay = 0.30f;          // just after sunrise
        cycle.dayNumber++;
        cycle.Apply(cycle.timeOfDay);

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.stamina = PlayerStats.Instance.maxStamina;
            PlayerStats.Instance.health = Mathf.Min(PlayerStats.Instance.maxHealth, PlayerStats.Instance.health + 25f);
        }

        // chasing wolves give up when you are asleep
        var wolves = FindObjectsOfType<Wolf>();
        for (int i = 0; i < wolves.Length; i++)
            if (wolves[i] != null) Destroy(wolves[i].gameObject);

        Debug.Log("[Sleep] slept until morning - day " + cycle.dayNumber);
        return true;
    }
}
