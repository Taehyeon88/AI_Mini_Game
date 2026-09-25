using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(Collider2D))]
public class ExpGem : MonoBehaviour, IPickup
{
    [SerializeField] private float _magnetSpeed = 18.8f;

    private PlayerController _player;
    private ObjectPool<ExpGem> _pool;
    private int _expAmount;
    private bool _isReleased;

    public void Init(int expAmount, PlayerController player, ObjectPool<ExpGem> pool)
    {
        _expAmount = expAmount;
        _player = player;
        _pool = pool;
        _isReleased = false;
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance.State != GameState.Playing || _player == null)
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, _player.transform.position);
        if (distance > _player.PickupRadius)
        {
            return;
        }

        transform.position = Vector2.MoveTowards(
            transform.position,
            _player.transform.position,
            _magnetSpeed * Time.fixedDeltaTime);
    }

    public void OnPickup(PlayerController player)
    {
        if (_isReleased)
        {
            return;
        }

        _isReleased = true;
        SoundManager.Instance.PlayPickup();
        player.AddExp(_expAmount);
        _pool.Release(this);
    }
}
