using UnityEngine;

// ─── Damage Type ────────────────────────────────────────────────────────────
// Categorizes the source of a hit. Useful for resistances, immunities, and
// type-specific reactions later. Add new types here as combat grows.
public enum DamageType
{
    Basic,          // Standard projectile hit
    Special,        // Player's manual special attack
    Contact,        // Touching an enemy / contact damage
    Environmental   // Hazards, spikes, etc.
}

// ─── Damage Payload ─────────────────────────────────────────────────────────
// A package of information about a single hit. Created by attacks, consumed
// by IDamageable receivers. Using a struct keeps this lightweight (no GC) and
// passing it as a single parameter means we can add fields later without
// breaking existing method signatures.
//
// Usage:
//   DamagePayload payload = new DamagePayload {
//       amount = 5f,
//       type = DamageType.Basic,
//       source = gameObject,
//       knockback = Vector2.right * 3f
//   };
//   target.TakeDamage(payload);
public struct DamagePayload
{
    // How much damage this hit deals.
    public float amount;

    // Category of damage. Lets receivers handle different types differently
    // (e.g. an enemy could be immune to Basic but vulnerable to Special).
    public DamageType type;

    // The GameObject that dealt this damage. Useful for kill credit, AI
    // reactions ("I was hit by who?"), and knockback direction calculation.
    public GameObject source;

    // Knockback impulse to apply to the receiver. Zero vector = no knockback.
    public Vector2 knockback;

    // Convenience constructor for the most common case.
    public DamagePayload(float amount, DamageType type, GameObject source)
    {
        this.amount = amount;
        this.type = type;
        this.source = source;
        this.knockback = Vector2.zero;
    }
}