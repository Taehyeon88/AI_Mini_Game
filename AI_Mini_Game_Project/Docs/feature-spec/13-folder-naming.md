# 13. 폴더 구조 & 씬 초기 구성 (CLAUDE.md 본문에 박는 내용)

> 폴더 **트리의 SoT** — CLAUDE.md 본문(자동 로딩)에 이 트리를 그대로 박는다(CLAUDE-template과 동기화).
> **네이밍·코드 규약은 `convention.md`(§1)가 SoT** — 여기선 요약만.

```
Assets/
├─ _GameKit/        ← 강사 제공 리소스 (4-0에서 받음)
│  ├─ Sprites/
│  ├─ Sounds/
│  └─ Fonts/
├─ Scripts/
│  ├─ Core/         ← GameManager, 인터페이스
│  │  └─ Interfaces/  (IDamageable, IWeapon, IPickup)
│  ├─ Player/
│  ├─ Enemy/
│  ├─ Weapon/
│  ├─ Pickup/
│  ├─ Upgrade/
│  ├─ UI/
│  └─ Editor/         ← 에디터 전용 도구(WaveDebuggerWindow 등), 빌드 미포함
├─ Resources/          ← SO 카탈로그 (런타임 Resources.LoadAll, 인스펙터 드래그 X)
│  ├─ Enemies/      ← EnemyData 자산
│  ├─ Weapons/      ← WeaponData 자산
│  └─ Upgrades/     ← UpgradeData 자산
└─ Scenes/
   └─ Main.unity
```

**네이밍·필드 규약은 `convention.md` §1이 SoT** (요약: MonoBehaviour 역할 명사 · ScriptableObject `~Data` 접미사 · 인터페이스 `I` 접두사 · 필드는 `[SerializeField] private`만).

## 씬 초기 구성 (Main.unity — 본인이 에디터에서 배치)
강의 시작 시 씬에 있어야 할 오브젝트(코드 아님, 에디터 작업):
- **Player** (스프라이트 + Rigidbody2D + 트리거 콜라이더, `Player` 레이어)
- **Main Camera** (Orthographic, size 9.4, Z -10) + **Cinemachine Virtual Camera**(Follow=Player)
- **무한 바닥** (타일 SpriteRenderer, Draw Mode=Tiled, 카메라 자식 — §3)
- **Managers** (빈 GameObject 1개에 `GameManager`·`EnemySpawner`·`BossSpawner` 부착)
- **Canvas** (HUD/레벨업/보스HP/결과/인트로 컨테이너 — §9·§11)
- 레이어 & 충돌 매트릭스 세팅 (convention §6)
- **프리팹 구성**: Enemy = 스프라이트 + 트리거 콜라이더(RB2D 없음) / **투사체(Player·Enemy) = 트리거 콜라이더 + Kinematic Rigidbody2D(Gravity 0)** — 트리거 판정은 쌍 중 한쪽에 RB2D 필수 (convention §6)
