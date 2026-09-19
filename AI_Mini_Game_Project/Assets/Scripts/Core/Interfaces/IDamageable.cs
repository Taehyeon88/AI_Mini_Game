public interface IDamageable
{
    int MaxHP { get; }
    int CurrentHP { get; }
    bool IsAlive { get; }
    void TakeDamage(int amount);
}
