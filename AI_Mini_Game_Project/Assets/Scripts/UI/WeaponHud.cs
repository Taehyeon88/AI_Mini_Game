using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class WeaponHud : MonoBehaviour
{
    private static readonly WeaponKind[] DisplayedKinds = { WeaponKind.Knife, WeaponKind.Axe, WeaponKind.Spear};

    [SerializeField] private PlayerController _player;
    [SerializeField] private WeaponSlotView _slotPrefab;

    private ObjectPool<WeaponSlotView> _pool;
    private readonly Dictionary<WeaponKind, WeaponSlotView> _activeSlots = new Dictionary<WeaponKind, WeaponSlotView>();

    private void Awake()
    {
        if (_player == null)
        {
            Debug.LogError($"{nameof(WeaponHud)}: _player가 연결되지 않았습니다.", this);
        }

        if (_slotPrefab == null)
        {
            Debug.LogError($"{nameof(WeaponHud)}: _slotPrefab이 연결되지 않았습니다.", this);
        }

        _pool = new ObjectPool<WeaponSlotView>(
            createFunc: () => Instantiate(_slotPrefab, transform),
            actionOnGet: slot => slot.gameObject.SetActive(true),
            actionOnRelease: slot => slot.gameObject.SetActive(false),
            actionOnDestroy: slot => Destroy(slot.gameObject),
            collectionCheck: true,
            defaultCapacity: DisplayedKinds.Length,
            maxSize: DisplayedKinds.Length);
    }

    private void Update()
    {
        if (_player == null || _slotPrefab == null)
        {
            return;
        }

        foreach (WeaponKind kind in DisplayedKinds)
        {
            bool owned = _player.Weapons.TryGet(kind, out IWeapon weapon);
            bool hasSlot = _activeSlots.TryGetValue(kind, out WeaponSlotView slot);

            if (owned && !hasSlot)
            {
                slot = _pool.Get();
                slot.transform.SetAsLastSibling();
                _activeSlots[kind] = slot;
            }
            else if (!owned && hasSlot)
            {
                _pool.Release(slot);
                _activeSlots.Remove(kind);
                continue;
            }

            if (owned)
            {
                slot.SetData(GetStageSprite(weapon), weapon.CurrentDamage);
            }
        }
    }

    // Knife.cs/Axe.cs 내부(private) 스테이지 계산식과 동일 — IWeapon 공개 표면(Data/Level)만으로 HUD가 재계산.
    private static Sprite GetStageSprite(IWeapon weapon)
    {
        Sprite[] stageSprites = weapon.Data.StageSprites;
        int index = Mathf.Clamp((weapon.Level - 1) / 2, 0, stageSprites.Length - 1);
        return stageSprites[index];
    }
}
