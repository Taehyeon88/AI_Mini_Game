using UnityEngine;

public class IntroScreen : MonoBehaviour
{
    [SerializeField] private GameObject _panelRoot;

    private void OnEnable()
    {
        GameManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged -= HandleStateChanged;
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Title) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            GameManager.Instance.ChangeState(GameState.Playing);
        }
    }

    private void HandleStateChanged(GameState state)
    {
        _panelRoot.SetActive(state == GameState.Title);
    }
}
