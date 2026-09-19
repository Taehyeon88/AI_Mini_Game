using System;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private float _clearTimeLimitSeconds = 180f;

    private float _elapsedTime;
    private int _killCount;

    public GameState State { get; private set; }
    public float ElapsedTime => _elapsedTime;
    public int KillCount => _killCount;

    public event Action<GameState> OnStateChanged;
    public event Action<IDamageable> OnBossSpawned;

    protected override void Awake()
    {
        base.Awake();
        ChangeState(GameState.Title);
    }

    private void Update()
    {
        if (State != GameState.Playing) return;

        _elapsedTime += Time.deltaTime;
        if (_elapsedTime >= _clearTimeLimitSeconds)
        {
            ChangeState(GameState.Clear);
        }
    }

    public void ChangeState(GameState next)
    {
        State = next;

        if (next == GameState.Playing)
        {
            Time.timeScale = 1f;
        }
        else if (next == GameState.LevelUpPaused)
        {
            Time.timeScale = 0f;
        }

        OnStateChanged?.Invoke(next);
    }

    public void AddKill()
    {
        _killCount++;
    }

    public void NotifyBossSpawned(IDamageable boss)
    {
        OnBossSpawned?.Invoke(boss);
    }

    public void TogglePause()
    {
        if (State != GameState.Playing) return;

        Time.timeScale = Time.timeScale > 0f ? 0f : 1f;
    }
}
