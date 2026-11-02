using UnityEngine;

// Night predator. Circles the player, charges in, bites, and backs off when
// wounded or when a campfire is close. Vanishes at dawn.
public class Wolf : MonoBehaviour
{
    public float maxHealth = 65f;
    public float walkSpeed = 3.2f;
    public float runSpeed = 7.6f;
    public float stalkRange = 16f;
    public float chargeRange = 11f;
    public float biteRange = 2.3f;
    public float biteDamage = 8f;
    public float biteInterval = 1.6f;
    public float daylightDespawnDelay = 6f;

    enum State { Stalk, Charge, Bite, Retreat, Leave }

    State state = State.Stalk;
    CharacterController cc;
    Transform player;
    float health;
    float verticalVelocity;
    float nextBiteAt;
    float stateUntil;
    float circleDir = 1f;
    float spawnTime;

    public bool Dead { get; private set; }

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        health = maxHealth;
        spawnTime = Time.time;
    }

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
        circleDir = Random.value < 0.5f ? -1f : 1f;
        stateUntil = Time.time + Random.Range(1.5f, 3f);
    }

    void Update()
    {
        if (Dead || player == null) return;

        bool daylight = DayNightCycle.Instance == null || !DayNightCycle.Instance.IsNight;
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        // Daylight or too close to a fire: leave.
        var fire = Campfire.Nearest(transform.position);
        bool nearFire = fire != null && Vector3.Distance(fire.transform.position, transform.position) < fire.repelRadius;

        if (daylight || nearFire)
        {
            state = State.Leave;
            Vector3 away = nearFire && fire != null
                ? transform.position - fire.transform.position
                : (player.position - transform.position);
            away.y = 0f;
            Move(away.normalized, runSpeed);
            if (daylight && Time.time - spawnTime > daylightDespawnDelay && distance > 40f)
                Destroy(gameObject);
            return;
        }

        switch (state)
        {
            case State.Stalk:
                // circle at range, waiting for an opening
                if (distance > stalkRange) Move(toPlayer.normalized, runSpeed);
                else
                {
                    Vector3 tangent = Vector3.Cross(Vector3.up, toPlayer.normalized) * circleDir;
                    Move((tangent * 0.85f + toPlayer.normalized * 0.15f).normalized, walkSpeed);
                }
                if (Time.time >= stateUntil || distance < chargeRange) state = State.Charge;
                break;

            case State.Charge:
                if (distance > biteRange) Move(toPlayer.normalized, runSpeed);
                else state = State.Bite;
                break;

            case State.Bite:
                Face(toPlayer);
                if (Time.time >= nextBiteAt && distance <= biteRange + 0.6f)
                {
                    nextBiteAt = Time.time + biteInterval;
                    if (PlayerStats.Instance != null) PlayerStats.Instance.Damage(biteDamage);
                    if (AudioManager.Instance != null) AudioManager.Instance.Play(AudioManager.Sfx.WolfBite);
                    Debug.Log("[Wolf] bit the player for " + biteDamage);
                }
                if (distance > biteRange + 1.5f) state = State.Charge;
                break;
        }

        StayGrounded();
    }

    void Move(Vector3 dir, float speed)
    {
        if (dir.sqrMagnitude < 0.001f) return;
        dir.Normalize();

        if (cc != null && cc.enabled)
        {
            if (cc.isGrounded) verticalVelocity = -1f;
            else verticalVelocity += -22f * Time.deltaTime;
            cc.Move((dir * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
        }
        else transform.position += dir * speed * Time.deltaTime;

        Face(dir);
    }

    void Face(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir.normalized), 10f * Time.deltaTime);
    }

    void StayGrounded()
    {
        var t = Terrain.activeTerrain;
        if (t == null) return;
        float ground = t.SampleHeight(transform.position) + t.transform.position.y;
        if (transform.position.y < ground) transform.position = new Vector3(transform.position.x, ground, transform.position.z);
    }

    public void TakeDamage(float amount)
    {
        if (Dead) return;
        health -= amount;
        Debug.Log("[Wolf] hit, hp=" + Mathf.RoundToInt(health));

        if (health <= 0f) Die();
        else if (health < maxHealth * 0.35f)
        {
            state = State.Retreat;
            stateUntil = Time.time + 5f;
        }
    }

    void Die()
    {
        Dead = true;
        if (Inventory.Instance != null)
        {
            Inventory.Instance.Add("meat", 2);
            Inventory.Instance.Add("wolf_pelt", 1);
        }
        Debug.Log("[Wolf] died");
        Destroy(gameObject, 0.1f);
    }
}
