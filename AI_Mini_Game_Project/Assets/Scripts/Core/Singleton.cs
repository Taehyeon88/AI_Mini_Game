using UnityEngine;

/// <summary>
/// 씬에 하나만 존재하는 MonoBehaviour 매니저용 베이스.
/// 사용: public class GameManager : Singleton<GameManager> { ... }
/// </summary>
public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = (T)this;
    }
}
