using System;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private float _clearTimeLimitSeconds = 180f;

    private float _elapsedTime;
    private int _killCount;

    public GameState State { get; private set; }
    public float ElapsedTime => _elapsedTime;
    public float RemainingTime => Mathf.Max(0f, _clearTimeLimitSeconds - _elapsedTime);
    public int KillCount => _killCount;
    public PlayerController Player { get; private set; }

    public static event Action<GameState> OnStateChanged;
    public static event Action<IDamageable> OnBossSpawned;

    protected override void Awake()
    {
        base.Awake();

        Player = FindFirstObjectByType<PlayerController>();
        if (Player == null)
        {
            Debug.LogError($"{nameof(GameManager)}: PlayerController를 씬에서 찾지 못했습니다.", this);
        }
    }

    private void Start()
    {
        // OnStateChanged가 static이라도 최초 발행 시점은 씬의 모든 Awake/OnEnable이 끝난 뒤여야
        // 구독자(OnEnable에서 구독)가 이 최초 발행을 놓치지 않는다.
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
