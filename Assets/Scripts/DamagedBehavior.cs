using UnityEngine;

public interface IDamageable
{
    float MaxHealth { get; }
    float CurrentHealth { get; }
    bool IsDead { get; }

    void TakeDamage(float amount);
    void Heal(float amount);
    void Die();

    //void ReactionToLight();

    void ApplyForce(Vector2 force)
    {
        //rb.linearVelocity = force;
    }

}
