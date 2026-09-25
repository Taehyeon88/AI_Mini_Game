using Unity.Cinemachine;
using UnityEngine;

public class CameraShakeManager : Singleton<CameraShakeManager>
{
    [SerializeField] private CinemachineImpulseSource _hitImpulseSource;
    [SerializeField] private CinemachineImpulseSource _bossSpawnImpulseSource;

    protected override void Awake()
    {
        base.Awake();

        if (_hitImpulseSource == null)
        {
            Debug.LogError($"{nameof(CameraShakeManager)}: {nameof(_hitImpulseSource)}가 연결되지 않았습니다.", this);
        }
        if (_bossSpawnImpulseSource == null)
        {
            Debug.LogError($"{nameof(CameraShakeManager)}: {nameof(_bossSpawnImpulseSource)}가 연결되지 않았습니다.", this);
        }
    }

    private void OnEnable()
    {
        GameManager.OnBossSpawned += HandleBossSpawned;
    }

    private void OnDisable()
    {
        GameManager.OnBossSpawned -= HandleBossSpawned;
    }

    public void ShakeOnHit()
    {
        _hitImpulseSource?.GenerateImpulse();
    }

    public void ShakeOnBossSpawn()
    {
        _bossSpawnImpulseSource?.GenerateImpulse();
    }

    private void HandleBossSpawned(IDamageable boss)
    {
        ShakeOnBossSpawn();
    }
}
