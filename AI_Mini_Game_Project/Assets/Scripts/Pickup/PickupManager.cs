using UnityEngine;
using UnityEngine.Pool;

public class PickupManager : Singleton<PickupManager>
{
    [SerializeField] private ExpGem _gemPrefab;
    [SerializeField] private Transform _gemParent;

    private PlayerController _player;
    private ObjectPool<ExpGem> _pool;

    protected override void Awake()
    {
        base.Awake();

        _player = FindFirstObjectByType<PlayerController>();
        if (_player == null)
        {
            Debug.LogError($"{nameof(PickupManager)}: PlayerController를 씬에서 찾지 못했습니다.", this);
        }

        _pool = new ObjectPool<ExpGem>(
            createFunc: () => Instantiate(_gemPrefab, _gemParent),
            actionOnGet: gem => gem.gameObject.SetActive(true),
            actionOnRelease: gem => gem.gameObject.SetActive(false),
            actionOnDestroy: gem => Destroy(gem.gameObject),
            collectionCheck: true,
            defaultCapacity: 16,
            maxSize: 60);
    }

    public void Spawn(Vector2 position, int expAmount)
    {
        ExpGem gem = _pool.Get();
        gem.transform.position = position;
        gem.Init(expAmount, _player, _pool);
    }
}
