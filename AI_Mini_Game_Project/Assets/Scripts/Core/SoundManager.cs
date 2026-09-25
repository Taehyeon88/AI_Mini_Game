using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    [SerializeField] private AudioSource _sfxSource;
    [SerializeField] private AudioSource _bgmSource;

    [SerializeField] private AudioClip _bgmClip;
    [SerializeField] private AudioClip _bossBgmClip;
    [SerializeField] private AudioClip _weaponFireClip;
    [SerializeField] private AudioClip _bossFireClip;
    [SerializeField] private AudioClip _playerHitClip;
    [SerializeField] private AudioClip _pickupClip;
    [SerializeField] private AudioClip _levelUpClip;
    [SerializeField] private AudioClip _gameOverClip;
    [SerializeField] private AudioClip _clearClip;

    [SerializeField] private float _weaponFireVolumeScale = 0.5f;

    private void OnEnable()
    {
        GameManager.OnStateChanged += HandleStateChanged;
        GameManager.OnBossSpawned += HandleBossSpawned;
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged -= HandleStateChanged;
        GameManager.OnBossSpawned -= HandleBossSpawned;
    }

    public void PlayWeaponFire() => _sfxSource.PlayOneShot(_weaponFireClip, _weaponFireVolumeScale);
    public void PlayBossFire() => _sfxSource.PlayOneShot(_bossFireClip);
    public void PlayPlayerHit() => _sfxSource.PlayOneShot(_playerHitClip);
    public void PlayPickup() => _sfxSource.PlayOneShot(_pickupClip);

    private void HandleStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Playing:
                if (!_bgmSource.isPlaying)
                {
                    _bgmSource.clip = _bgmClip;
                    _bgmSource.Play();
                }
                break;
            case GameState.LevelUpPaused:
                _sfxSource.PlayOneShot(_levelUpClip);
                break;
            case GameState.GameOver:
                _sfxSource.PlayOneShot(_gameOverClip);
                _bgmSource.Stop();
                break;
            case GameState.Clear:
                _sfxSource.PlayOneShot(_clearClip);
                _bgmSource.Stop();
                break;
        }
    }

    private void HandleBossSpawned(IDamageable boss)
    {
        _bgmSource.clip = _bossBgmClip;
        _bgmSource.Play();
    }
}
