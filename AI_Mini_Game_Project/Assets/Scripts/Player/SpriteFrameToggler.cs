using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFrameToggler : MonoBehaviour
{
    [SerializeField] private Sprite[] _frames;
    [SerializeField] private float _interval = 0.25f;

    private SpriteRenderer _spriteRenderer;
    private float _timer;
    private int _frameIndex;
    private bool _isMoving;

    private void Awake()
    {
        if (!TryGetComponent(out _spriteRenderer))
        {
            Debug.LogError($"{nameof(SpriteFrameToggler)} requires a SpriteRenderer.", this);
        }
    }

    private void Update()
    {
        if (_spriteRenderer == null || !_isMoving || _frames == null || _frames.Length < 2)
        {
            return;
        }

        _timer += Time.deltaTime;
        if (_timer < _interval)
        {
            return;
        }

        _timer -= _interval;
        _frameIndex = 1 - _frameIndex;
        _spriteRenderer.sprite = _frames[_frameIndex];
    }

    public void SetFrames(Sprite[] frames, float interval)
    {
        _frames = frames;
        _interval = interval;
        _timer = 0f;
        _frameIndex = 0;

        if (_spriteRenderer != null && _frames != null && _frames.Length > 0)
        {
            _spriteRenderer.sprite = _frames[0];
        }
    }

    public void SetMoving(bool isMoving)
    {
        if (_isMoving == isMoving)
        {
            return;
        }

        _isMoving = isMoving;

        if (!isMoving)
        {
            _timer = 0f;
            _frameIndex = 0;
            if (_spriteRenderer != null && _frames != null && _frames.Length > 0)
            {
                _spriteRenderer.sprite = _frames[0];
            }
        }
    }
}
