using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "GameData/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [SerializeField] private int _maxHP;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private int _attackPower;
    [SerializeField] private int _dropExp;
    [SerializeField] private Sprite[] _sprites;
    [SerializeField] private float _animInterval;
    [SerializeField] private float _contactDamageInterval;
    [SerializeField] private float _hitFlashDuration;

    public int MaxHP => _maxHP;
    public float MoveSpeed => _moveSpeed;
    public int AttackPower => _attackPower;
    public int DropExp => _dropExp;
    public Sprite[] Sprites => _sprites;
    public float AnimInterval => _animInterval;
    public float ContactDamageInterval => _contactDamageInterval;
    public float HitFlashDuration => _hitFlashDuration;
}
