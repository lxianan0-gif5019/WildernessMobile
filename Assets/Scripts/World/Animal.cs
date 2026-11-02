using UnityEngine;

// Wildlife. Wanders, flees when the player gets close, and can be hunted for meat.
// Boars are neutral: they only fight back once you hit them.
public class Animal : MonoBehaviour
{
    public enum Kind { Rabbit, Deer, Boar }

    [Header("Identity")]
    public Kind kind = Kind.Deer;
    public string displayName = "Deer";
    public string dropItem = "meat";
    public int dropAmount = 2;

    [Header("Stats")]
    public float maxHealth = 40f;
    public float walkSpeed = 1.6f;
    public float runSpeed = 5.5f;
    public float fleeRange = 11f;
    public bool fightsBack = false;

    [Header("Attack (only if fightsBack)")]
    public float attackDamage = 8f;
    public float attackRange = 2.2f;
    public float attackInterval = 1.6f;

    enum State { Wander, Flee, Chase, Dead }

    State state = State.Wander;
    CharacterController cc;
    Transform player;
    Vector3 home;
    Vector3 target;
    float retargetAt;
    float nextAttackAt;
    float verticalVelocity;
    float health;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        health = maxHealth;
        home = transform.position;
    }

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
        PickWanderTarget();
    }

    void Update()
    {
        if (state == State.Dead) return;
        if (player == null) return;

        float distance = Vector3.Distance(player.position, transform.position);

        // Fire keeps wildlife away (boars too, unless they are already charging).
        if (state != State.Chase)
        {
            var fire = Campfire.Nearest(transform.position);
            if (fire != null && Vector3.Distance(fire.transform.position, transform.position) < fire.repelRadius)
            {
                state = State.Flee;
                Vector3 fromFire = transform.position - fire.transform.position;
                fromFire.y = 0f;
                if (fromFire.sqrMagnitude < 0.01f) fromFire = Vector3.forward;
                MoveTowards(transform.position + fromFire.normalized * 6f, runSpeed);
                StickToGround();
                return;
            }
        }

        // Decide state
        if (state != State.Chase)
        {
            if (fightsBack && health < maxHealth && distance < 18f) state = State.Chase;
            else if (distance < fleeRange) state = State.Flee;
            else state = State.Wander;
        }
        else if (distance > 25f) state = State.Wander;

        switch (state)
        {
            case State.Wander:
                if (Time.time >= retargetAt || Vector3.Distance(transform.position, target) < 1.2f)
                    PickWanderTarget();
                MoveTowards(target, walkSpeed);
                break;

            case State.Flee:
                Vector3 away = (transform.position - player.position);
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
                MoveTowards(transform.position + away.normalized * 6f, runSpeed);
                break;

            case State.Chase:
                if (distance > attackRange) MoveTowards(player.position, runSpeed);
                else AttackPlayer();
                break;
        }

        StickToGround();
    }

    void PickWanderTarget()
    {
        Vector2 offset = Random.insideUnitCircle * 14f;
        target = home + new Vector3(offset.x, 0f, offset.y);
        retargetAt = Time.time + Random.Range(3f, 7f);
    }

    void MoveTowards(Vector3 destination, float speed)
    {
        Vector3 dir = destination - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.04f) return;

        dir.Normalize();
        if (cc != null && cc.enabled) cc.Move(dir * speed * Time.deltaTime);
        else transform.position += dir * speed * Time.deltaTime;

        Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, 8f * Time.deltaTime);
    }

    void StickToGround()
    {
        var terrain = Terrain.activeTerrain;
        if (terrain == null) return;

        float groundY = terrain.SampleHeight(transform.position) + terrain.transform.position.y;
        if (transform.position.y > groundY + 0.2f)
        {
            verticalVelocity += -20f * Time.deltaTime;
            transform.position += Vector3.up * verticalVelocity * Time.deltaTime;
        }
        else
        {
            verticalVelocity = 0f;
            transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
        }
    }

    void AttackPlayer()
    {
        if (Time.time < nextAttackAt) return;
        nextAttackAt = Time.time + attackInterval;
        if (PlayerStats.Instance != null) PlayerStats.Instance.Damage(attackDamage);
        Debug.Log("[Animal] " + displayName + " hit the player for " + attackDamage);
    }

    public void TakeDamage(float amount)
    {
        if (state == State.Dead) return;

        health -= amount;
        Debug.Log("[Animal] " + displayName + " hit, hp=" + Mathf.RoundToInt(health));

        if (health <= 0f) Die();
        else
        {
            // wounded: flee harder, or turn and fight
            if (fightsBack) state = State.Chase;
            else state = State.Flee;
        }
    }

    void Die()
    {
        state = State.Dead;

        // animals now drop pickups on the ground, not straight into the bag
        if (Inventory.Instance != null)
        {
            Vector3 at = transform.position + Vector3.up * 0.4f;
            for (int i = 0; i < dropAmount; i++)
                ItemPickup.Spawn(dropItem, 1, at + Random.insideUnitSphere * 0.5f);
        }

        Debug.Log("[Animal] " + displayName + " died, dropped " + dropItem + " x" + dropAmount);

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        StartCoroutine(FadeOut());
    }

    // topple over and shrink away instead of popping out of existence
    System.Collections.IEnumerator FadeOut()
    {
        Vector3 startScale = transform.localScale;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, 0f, 80f);

        float t = 0f;
        const float duration = 0.55f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            transform.rotation = Quaternion.Slerp(startRot, endRot, p);
            transform.localScale = Vector3.Lerp(startScale, startScale * 0.1f, p);
            transform.position += Vector3.down * 0.35f * Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}
