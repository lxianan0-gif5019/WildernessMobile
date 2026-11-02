using UnityEngine;

// The four survival stats. Single owner of the numbers; everything else reads
// them or calls Eat/Drink/Damage.
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;

    [Header("Health")]
    public float maxHealth = 100f;
    public float health = 100f;

    [Header("Hunger")]
    public float maxHunger = 100f;
    public float hunger = 100f;
    public float hungerDrainPerSecond = 0.18f;      // ~9 min from full

    [Header("Thirst")]
    public float maxThirst = 100f;
    public float thirst = 100f;
    public float thirstDrainPerSecond = 0.28f;      // ~6 min from full (faster than hunger)

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float stamina = 100f;
    public float staminaDrainPerSecond = 24f;       // sprinting
    public float staminaRegenPerSecond = 16f;       // idle / walking

    [Header("Consequences")]
    public float starvationDamage = 1.5f;
    public float dehydrationDamage = 2.5f;
    public float regenPerSecond = 0.6f;
    public float respawnDelay = 1.5f;

    public bool IsDead { get; private set; }

    Vector3 spawnPosition;
    CharacterController cc;

    void Awake()
    {
        Instance = this;
        cc = GetComponent<CharacterController>();
    }

    void Start()
    {
        spawnPosition = transform.position;
    }

    void Update()
    {
        if (IsDead) return;

        hunger = Mathf.Max(0f, hunger - hungerDrainPerSecond * Time.deltaTime);
        thirst = Mathf.Max(0f, thirst - thirstDrainPerSecond * Time.deltaTime);

        bool sprinting = InputHub.SprintHeld && InputHub.Move.sqrMagnitude > 0.05f;
        if (sprinting) stamina = Mathf.Max(0f, stamina - staminaDrainPerSecond * Time.deltaTime);
        else stamina = Mathf.Min(maxStamina, stamina + staminaRegenPerSecond * Time.deltaTime);

        float damage = 0f;
        if (hunger <= 0f) damage += starvationDamage;
        if (thirst <= 0f) damage += dehydrationDamage;

        if (damage > 0f)
            health = Mathf.Max(0f, health - damage * Time.deltaTime);
        else if (hunger > 40f && thirst > 40f)
            health = Mathf.Min(maxHealth, health + regenPerSecond * Time.deltaTime);   // slow natural recovery

        if (health <= 0f) Die();
    }

    public bool CanSprint()
    {
        return stamina > 1f;
    }

    public void Eat(float food, float water)
    {
        hunger = Mathf.Min(maxHunger, hunger + food);
        thirst = Mathf.Min(maxThirst, thirst + water);
        Debug.Log("[Stats] ate: hunger=" + hunger.ToString("F0") + " thirst=" + thirst.ToString("F0"));
    }

    public void Drink(float water)
    {
        thirst = Mathf.Min(maxThirst, thirst + water);
        Debug.Log("[Stats] drank: thirst=" + thirst.ToString("F0"));
    }

    public void Damage(float amount)
    {
        if (IsDead) return;
        health = Mathf.Max(0f, health - amount);
        if (AudioManager.Instance != null) AudioManager.Instance.Play(AudioManager.Sfx.Hurt);
        var cam = Camera.main != null ? Camera.main.GetComponent<ThirdPersonCamera>() : null;
        if (cam != null) cam.Shake(Mathf.Clamp(amount / 12f, 0.25f, 0.8f));
        if (health <= 0f) Die();
    }

    void Die()
    {
        if (IsDead) return;
        IsDead = true;
        Debug.Log("[Stats] died - respawning in " + respawnDelay + "s");
        Invoke(nameof(Respawn), respawnDelay);
    }

    void Respawn()
    {
        if (cc != null) cc.enabled = false;
        transform.position = spawnPosition;
        if (cc != null) cc.enabled = true;

        health = maxHealth;
        hunger = maxHunger;
        thirst = maxThirst;
        stamina = maxStamina;
        IsDead = false;
        Debug.Log("[Stats] respawned at spawn point");
    }

    public void SetSpawn(Vector3 position)
    {
        spawnPosition = position;
    }
}
