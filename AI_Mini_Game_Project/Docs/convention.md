# 코딩 컨벤션 & 코드 계약 (convention.md)

> 뱀서 토이프로젝트의 **코드 규약 + 제공 인프라 + 인터페이스 계약**을 한 곳에 모은 문서.
> - **무엇을** 만드는지 → `game-design.md`
> - **어떻게**(동작·수치) → `feature-spec.md` (§1~§14)
> - **코드 규칙**(이 문서) → 코드 작성·수정 전 필독. CLAUDE.md가 이 문서를 참조한다.
>
> 핵심 원칙: 구현은 AI가 챕터마다 다르게 생성해도 좋지만, **여기 적힌 시그니처·열거형·SO 필드 이름·인프라는 그대로 고정**한다. 이게 고정돼야 매번 다른 코드가 나와도 챕터끼리 서로 붙는다.

---

## 1. 코드 규약

### 프로젝트 규약 (이 프로젝트 고유)
- **필드 노출은 `[SerializeField] private`만**. `public` 필드 금지 (읽기 노출이 필요하면 프로퍼티).
- **매직 넘버 금지** — 게임플레이 수치는 모두 ScriptableObject 데이터로 (§4 SO 형태). **예외**: `PlayerController`·`GameManager`처럼 씬에 **단일 인스턴스**만 존재해 카탈로그가 필요 없는 대상은 SO를 새로 만들지 않고 `[SerializeField] private` 필드로 노출한다(§4 SO 카탈로그는 Enemy/Weapon/Upgrade처럼 `Resources.LoadAll`로 여러 개 불러오는 대상 전용).
- `Update()`에서 `GetComponent` / `FindObjectOfType` 호출 금지 — Awake/Start에서 캐시.
- 적·무기·픽업 추가는 **같은 패턴 반복** — 새 패턴 만들기 전에 기존 패턴부터 따르기.
- 매니저(GameManager 등)는 제공된 **`Singleton<T>` 상속** (직접 싱글톤 패턴 짜지 말 것 — §3).
- 풀링은 **`UnityEngine.Pool.ObjectPool<T>`** 사용 (커스텀 풀 금지 — §3).

### 기본 컨벤션 (Unity/C# 일반)
- **한 파일 한 public 타입**, 파일명 = 타입명.
- **접근 제한자 명시** (private 생략하지 말기).
- 미사용 `using` 제거. 한 줄 한 책임.
- 컴포넌트 캐시는 `TryGetComponent` + null 가드.
- **이벤트 구독은 `OnEnable`, 해제는 `OnDisable`** (구독 누수 방지).
- 태그·레이어 **문자열 하드코딩 금지** → `LayerMask`/상수로.
- 이동·타이머 등 시간 의존 로직은 **`Time.deltaTime`** (프레임 독립).
- `Debug.Log`는 디버그용 — 마무리 단계에서 정리.
- 주석은 **'왜'만** (자명한 코드 주석 금지).

### 네이밍
- MonoBehaviour: 역할 명사 (`PlayerController`, `EnemySpawner`).
- ScriptableObject: `~Data` 접미사 (`WeaponData`, `EnemyData`).
- 인터페이스: `I` 접두사 (`IDamageable`, `IWeapon`, `IPickup`).

---

## 2. 폴더 위치
- 인터페이스: `Assets/Scripts/Core/Interfaces/` · 열거형: `Assets/Scripts/Core/`
- 제공 인프라(`Singleton.cs` 등): `Assets/Scripts/Core/`
- **SO 데이터 카탈로그: `Assets/Resources/{Enemies,Weapons,Upgrades}/`** — 런타임에 `Resources.LoadAll<T>("폴더명")`로 일괄 로드(인스펙터 드래그 X, **Addressables 안 씀** — 토이 범위). `/so-data`도 이 경로에 생성. 프리팹(Enemy·투사체)은 SerializeField로 충분.
- 전체 폴더 구조는 CLAUDE.md 본문 / `feature-spec/13-folder-naming.md` 참고.

---

## 3. 제공 인프라 코드 (수정 금지 · 상속·사용만)

> 게임플레이가 아니라 **인프라**. 매번 똑같이 나와야 하는 보일러플레이트라 AI에 생성시키지 않고 **강사가 제공**(4-0 배포). 학생은 **수정하지 않고 상속·사용만** 한다.

### Singleton<T> — MonoBehaviour 제너릭 싱글톤
씬에 1개만 존재하는 매니저(GameManager 등)의 베이스. 단일 씬(`Main.unity`)이라 `DontDestroyOnLoad`는 의도적으로 **안 쓴다**(토이 범위).

```csharp
// Assets/Scripts/Core/Singleton.cs
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
```

**사용 규약**
- 매니저는 이 클래스를 상속: `public class GameManager : Singleton<GameManager>`
- 파생 클래스가 `Awake`를 쓰면 **반드시 `base.Awake()` 먼저 호출**
- 접근은 `GameManager.Instance.ElapsedTime` 식 (공개 표면은 §4)

### 풀링 — UnityEngine.Pool.ObjectPool<T> (직접 구현 X)
명세서 곳곳(투사체 50 prewarm, 잡몹 스폰/디스폰)에서 풀링 사용. **커스텀 풀 짜지 말고 Unity 내장 풀**(LTS 기본 제공). 직접 구현은 발산·버그 포인트.

```csharp
using UnityEngine.Pool;
// new ObjectPool<Projectile>(createFunc, onGet, onRelease, onDestroy,
//                            collectionCheck:true, defaultCapacity:50, maxSize:200);
```

**사용 규약**
- 투사체·EXP gem·잡몹은 풀에서 꺼내고(`Get`) 화면 밖/수명 종료 시 반환(`Release`)
- prewarm 수치는 명세서 기준(투사체 씬당 50)

### (선택) 보류 중인 인프라
- `GameEvents` 정적 이벤트 허브 — 지금은 GameManager `OnStateChanged` + `OnBossSpawned`(예외) 두 이벤트로 충분. 그 외 알림은 프로퍼티 폴링/`ChangeState`. 구독자 늘면 그때. (이벤트 '호출 vs 발행' 자체는 4-6에서 다룸)
- `EnemyData`/`WeaponData` 추상 베이스 — `/so-data`로 충분히 일관. 보류.

> 원칙: **제공 코드는 최소로.** 인프라(싱글톤·풀)만 깔고, 게임플레이는 학생이 Claude로 만든다.

---

## 4. 인터페이스 계약 (시그니처 고정)

> 시그니처를 바꿔야겠다고 판단되면 **조용히 바꾸지 말고 차이를 먼저 짚고** 진행 (CLAUDE.md 톤 규약과 동일).

### IDamageable
플레이어·잡몹·보스가 모두 구현. UI HP바(§9)는 구체 타입을 모른 채 `MaxHP`/`CurrentHP`에 바인딩.

```csharp
public interface IDamageable
{
    int  MaxHP      { get; }
    int  CurrentHP  { get; }      // 0 이하 → 사망
    bool IsAlive    { get; }      // CurrentHP > 0
    void TakeDamage(int amount);  // 음수 금지. 0 이하로 떨어지면 사망 처리
}
```
- HP는 **int** (수치: 플레이어 100, 쥐 10, 박쥐 15, 사이클롭스 40, 유령 20, 보스 500)
- 사망 처리(드랍·디스폰·게임오버)는 각 구현체 내부 책임

### IWeapon
자동 발사 무기 3종(단검·도끼·창)이 구현. 플레이어의 무기 관리자가 매 프레임 `Tick`을 돌린다. **창은 강화 없는 보조 무기**(WeaponBuff 카드 없음 → `ApplyUpgrade` 미호출).

```csharp
public interface IWeapon
{
    WeaponData Data         { get; }                   // 기본 수치는 SO에서
    int        Level        { get; }                   // 1 + 총 강화 횟수 (시작 1 = 무강화. 표시·stageSprites 선택용)
    int        CurrentDamage { get; }                  // 현재 실제 공격력 (스택 반영) — HUD 폴링용 (§9)
    void Tick(float deltaTime);                        // 쿨다운 진행 + 조건 충족 시 자동 발사
    void ApplyUpgrade(WeaponUpgradeType type);         // 해당 타입 스택 +1 (공식은 타입별 스택 기준 — §5 누적 공식)
}
```
- 발사 입력 없음(자동). 타겟팅은 무기별로 다름(가장 가까운 적 / 회전 / 무작위).
- 투사체는 ObjectPool 사용(§3).

### IPickup
EXP gem이 구현. 향후 회복·골드 등으로 확장 가능.

```csharp
public interface IPickup
{
    void OnPickup(PlayerController player);  // 픽업 반경(기본 2.3) 진입 시 호출
}
```
- EXP gem은 `OnPickup`에서 EXP 가산 후 자신을 풀로 반환.

### 열거형 (고정)
```csharp
// Core/GameState.cs — §12 상태 다이어그램과 1:1
public enum GameState { Boot, Title, Playing, LevelUpPaused, GameOver, Clear }

// Upgrade/WeaponUpgradeType.cs — §7 무기 강화 카테고리
public enum WeaponUpgradeType { Damage, Cooldown, ExtraProjectile }

// Weapon/WeaponKind.cs — 무기 식별 (NewWeapon 지급·WeaponBuff 대상)
public enum WeaponKind { Knife, Axe, Spear }

// Player/StatKind.cs — 스탯 강화 대상
public enum StatKind { MaxHP, MoveSpeed, PickupRadius }

// Upgrade/UpgradeCategory.cs — §7 카드 카테고리
public enum UpgradeCategory { NewWeapon, WeaponBuff, StatBuff }
```

### GameManager 공개 표면 (고정)
싱글톤(`Singleton<GameManager>` 상속 — §3). 다른 시스템(UI·스포너·무기)이 의존하는 표면만 고정, 내부 구현은 자유.

```csharp
// Core/GameManager.cs
GameState State        { get; }
float     ElapsedTime  { get; }   // 경과 시간(초). 타이머·웨이브·보스 등장(120s) 기준
int       KillCount    { get; }   // 잡몹·보스 처치 시 +1 (각 적 사망 처리에서 GameManager.AddKill() 호출)

event System.Action<GameState> OnStateChanged;  // 상태 전환 시 발행 (§12)
event System.Action<IDamageable> OnBossSpawned; // 보스 등장 시 보스 전달 (UI 보스HP바가 그 보스 HP 폴링·BGM 구독) — 예외로 고정

void ChangeState(GameState next);   // 전환은 이 메서드로만
void AddKill();                     // 처치 수 +1 (잡몹·보스 사망 처리에서 호출)
void NotifyBossSpawned(IDamageable boss);  // BossSpawner가 호출 → 내부에서 OnBossSpawned 발행 (4-8)
```
- 일시정지는 `Time.timeScale` 0/1 (레벨업·ESC)
- 클리어 우선순위: 보스 사망 → 즉시 Clear / 3분 타임아웃 → Clear(안전망) / HP 0 → GameOver
- **사망·레벨업은 별도 이벤트 계약 없이 각 처리에서 `ChangeState` 직접 호출** (보스 처치→Clear · 플레이어 HP0→GameOver · 레벨업→LevelUpPaused). HUD 등 표시는 `OnStateChanged` 구독 또는 `CurrentHP`/`ElapsedTime` 프로퍼티 폴링.

### PlayerController 공개 표면 (고정)
`IDamageable` 구현(HP) + 아래. (HUD·업그레이드·무기 시스템이 의존)
```csharp
PlayerWeapons Weapons { get; }   // 보유 무기. 매 프레임 각 IWeapon.Tick(dt) 호출 + AddWeapon(WeaponKind) (4-4 생성)
//   AddWeapon(kind): kind → 해당 IWeapon 구현 생성 + Resources의 WeaponData 매핑해 주입
int CurrentLevel { get; }        // HUD·레벨업
int CurrentExp   { get; }        // 현재 레벨 내 누적
int ExpToNext    { get; }        // 다음 레벨까지 필요량 (§6) → EXP바 fill = CurrentExp / ExpToNext
float MoveSpeed  { get; } float PickupRadius { get; }   // StatBuff 대상 (MaxHP는 IDamageable, MoveSpeed는 ×1.1이라 float)

void AddExp(int amount);                    // IPickup(EXP gem)이 호출 — 레벨업 판정·LevelUpPaused 전환은 내부 처리 (§6)
void ApplyStat(StatKind type, float amount); // UpgradeService StatBuff 분기가 호출 — base 기준 가산 스택 (§7)
```
- `PlayerWeapons`: `AddWeapon(WeaponKind)` 외 **`bool TryGet(WeaponKind, out IWeapon)`** 제공 (WeaponBuff 대상 무기 조회·중복 지급 방지)
- 입력은 **레거시 Input Manager**(`Input.GetAxisRaw`) 가정 — Project Settings ▸ Player ▸ **Active Input Handling = Both**(또는 Old). (신규 Input System 단독이면 `GetAxis` 런타임 에러)
- 모든 시스템(스포너·무기 Tick·타이머)은 `GameManager.State == Playing`일 때만 진행 (Title/LevelUpPaused/Clear/GameOver에선 정지 — §12).

### ScriptableObject 데이터 형태 (코드↔자산 계약)
필드 **이름**을 고정해야 `/so-data`로 만든 자산이 코드와 맞물린다. (노출은 `[SerializeField] private` + 프로퍼티)

> 이 카탈로그는 **Enemy/Weapon/Upgrade처럼 `Resources.LoadAll`로 여러 개 불러오는 대상 전용**이다. `PlayerController`·`GameManager`는 씬에 단일 인스턴스만 존재해 카탈로그가 필요 없으므로 SO를 만들지 않고, HP·이동속도·픽업반경·무적시간·클리어 제한시간 등을 각 스크립트에 `[SerializeField] private` 필드로 직접 노출한다(§1 매직 넘버 금지 규칙의 예외).

```csharp
// Resources/Enemies — §4  (Resources.LoadAll<EnemyData>("Enemies"))
EnemyData   : maxHP(int), moveSpeed(float), attackPower(int), dropExp(int),
              sprites(Sprite[]), animInterval(float), contactDamageInterval(float),
              hitFlashDuration(float)

// Resources/Weapons — §5
WeaponData  : displayName(string), damage(int), cooldown(float), range(float),
              projectileCount(int), projectileSpeed(float), prefab(GameObject),
              stageSprites(Sprite[]),
              // 강화 수치 (단검·도끼)
              projectileLifetime(float), upgradeDamageRate(float), upgradeCooldownRate(float), cooldownFloor(float),
              // 단검 전용
              projectileSpreadOffset(float),
              // 도끼 전용
              pivotAngularSpeed(float), selfSpinSpeed(float),
              // 창 전용
              orbitRadius(float), orbitDuration(float), orbitAngularSpeed(float), homingTurnRate(float)
//   · 도끼 회전 개수 = projectileCount 초기값(3). ExtraProjectile 강화는 단검 전용(도끼·창 projectileCount 불변).
//   · projectileSpeed = 투사체 속도(u/s). 창=유도 투사체라 사용.
//   · stageSprites = IWeapon.Level로 단계 선택(Lv1~2→[0]·3~4→[1]·5+→[2]). 창은 강화 없어 [0] 고정.
//   · displayName = 강화 카드의 "{무기명}" 표기 소스.
//   · projectileSpreadOffset = 단검 발사체 2발 이상일 때 진행 방향에 수직인 발사 위치 오프셋(유니트). 방향은 전부 동일(평행 발사), 발사 위치만 균등 분산.
//   · pivotAngularSpeed = 도끼 피봇 공전 속도(deg/sec). selfSpinSpeed = 도끼 자전 속도(deg/sec).
//   · orbitRadius = 창 대기 반경(u). orbitDuration = 창 대기 시간(sec). orbitAngularSpeed = 창 대기 공전 속도(deg/sec). homingTurnRate = 창 최대 선회율(deg/sec).

// Resources/Upgrades — §7  (Resources.LoadAll<UpgradeData>("Upgrades"))
UpgradeData : category(UpgradeCategory: NewWeapon/WeaponBuff/StatBuff),
              displayName(string), description(string), icon(Sprite),
              targetWeapon(WeaponKind),         // NewWeapon=지급할 무기 · WeaponBuff=강화 대상 무기
              weaponUpgradeType(WeaponUpgradeType), // WeaponBuff 전용 (Damage/Cooldown/ExtraProjectile)
              statType(StatKind),               // StatBuff 전용 (MaxHP/MoveSpeed/PickupRadius)
              amount(float)                     // 강화량: 0.2=+20%, 20=+20HP 등 (category·타입별 해석)

// Enemy/BossData — 보스 전용 SO. 카탈로그 로드 아님(§2) — BossController에 SerializeField로 직접 연결(보스 1종만 존재)
BossData    : keepDistance(float), keepDistanceDeadzone(float),
              boltDamage(int), boltSpeed(float), boltInterval(float), boltLifetime(float), boltSprite(Sprite),
              summonInterval(float), summonMinCount(int), summonMaxCount(int), summonCap(int), summonRadius(float),
              ghostData(EnemyData), boltPrefab(GameObject)
//   · keepDistance/keepDistanceDeadzone = 보스가 플레이어와 유지하려는 거리(u)와 허용 오차. FixedUpdate에서 거리 차만큼 접근/후퇴.
//   · boltDamage/boltSpeed/boltInterval/boltLifetime/boltSprite = 원거리 마법탄(EnemyProjectile) 발사 주기·수치.
//   · summonInterval/summonMinCount/summonMaxCount/summonCap/summonRadius = 유령 소환 주기·마리수 범위·동시 생존 상한·소환 반경.
//   · ghostData = 소환할 유령의 EnemyData. boltPrefab = 마법탄 프리팹(EnemyProjectile 부착, Kinematic RB2D 포함 — §6).
//   · 보스의 기본 스탯(HP·이동속도·공격력·드랍EXP·스프라이트)은 BossData가 아니라 **별도 EnemyData**(§4, HP 500)를 BossController가 함께 참조 — 잡몹과 같은 IDamageable 계약 재사용.

> **업그레이드 적용**: `UpgradeData`는 **데이터만**. 작은 `UpgradeService.Apply(UpgradeData u, PlayerController p)`가 `category`로 분기 —
> `NewWeapon` → `p.Weapons.AddWeapon(u.targetWeapon)` · `WeaponBuff` → 해당 무기 `IWeapon.ApplyUpgrade(u.weaponUpgradeType)` · `StatBuff` → `p`의 `u.statType` 스탯에 `u.amount` 적용.
> SO에 동작 박지 말 것 → **새 카테고리만 코드, 기존 카테고리 변형은 자산 추가만**.

---

## 5. 계약이 막는 발산 예시
- `TakeDamage(int)` vs `Damage(float, Vector2)` 처럼 챕터마다 깨지는 시그니처
- `WeaponData.cooldown` vs `coolTime` 같은 필드명 불일치로 자산이 안 붙는 문제
- 상태 이름(`Paused` vs `LevelUpPaused`)이 갈려 이벤트 구독이 어긋나는 문제

---

## 6. 물리 레이어 & 충돌 (고정 — 에디터에서 설정)

> 전투 판정은 전부 **isTrigger 콜라이더**(물리 밀림 없음). 이동 물리는 플레이어 Rigidbody2D(Dynamic)만. 레이어 문자열 하드코딩 금지(§1) → 아래 레이어를 만들고 충돌 매트릭스(Project Settings ▸ Physics2D)로 통제.
>
> ⚠️ **트리거 이벤트는 쌍 중 한쪽에 Rigidbody2D 필수**(Unity 2D 규칙). 그래서 **모든 투사체 프리팹(PlayerProjectile·EnemyProjectile)에 Kinematic Rigidbody2D(Gravity 0)** 부착 — 안 붙이면 투사체↔적 판정이 아예 안 뜬다. 적·픽업 프리팹은 RB2D 없음(상대인 플레이어/투사체의 RB가 판정 담당).

레이어: `Player` · `Enemy` · `Boss` · `PlayerProjectile` · `EnemyProjectile` · `Pickup`

| 주체 ＼ 대상 | Player | Enemy/Boss | Pickup | PlayerProj | EnemyProj |
|---|---|---|---|---|---|
| **Player** | — | 트리거(접촉 데미지) | 트리거(흡수) | 무시 | 트리거(피격) |
| **Enemy/Boss** | ↑ | 무시(겹침 허용) | 무시 | 트리거(피격) | 무시 |
| **PlayerProj** | 무시 | 트리거 | 무시 | 무시 | 무시 |
| **EnemyProj** | 트리거 | 무시 | 무시 | 무시 | 무시 |
| **Pickup** | ↑ | 무시 | 무시 | 무시 | 무시 |

- 적끼리·투사체끼리·투사체-픽업은 전부 **무시**(불필요한 판정 제거).
- 충돌 매트릭스 세팅은 **에디터 작업(본인)** — 4-5 시작 전 잡아둔다.

## 7. 투사체 타깃 규칙 (고정)
- **무기 투사체**(PlayerProjectile): `Enemy`/`Boss`만 맞힘.
- **적 투사체**(EnemyProjectile = 보스 마법탄): `Player`만 맞힘.
- `OnTriggerEnter2D`에서 상대 레이어 확인 후 `IDamageable.TakeDamage` 호출. 같은 진영·다른 투사체는 무시.
- 유도 무기(창)는 발사 후 매 프레임 타겟 방향으로 조향(homing). `PlayerProjectile` — 첫 명중 시 소멸.

## 8. 스프라이트 정렬(Sorting Order, 고정)

> Sorting Layer는 `Default` 하나만 쓴다(신규 레이어 생성 안 함 — 토이 범위). 같은 레이어 안에서 `SpriteRenderer.sortingOrder` 값으로만 위아래를 가른다. 값이 같으면 Unity가 내부적으로(Z거리 등) 타이브레이크하는데 2D에서는 사실상 비결정적이라 겹칠 때 그림이 랜덤하게 위/아래로 갈릴 수 있다 — **겹쳐야 하는 대상끼리는 반드시 값을 다르게 고정**한다.

```
FloorChunk               : -100
Player / Enemy / Boss    : 0
무기(근접·투사체 스프라이트) : 10
```

- 새 무기 프리팹(AxeBlade·KnifeProjectile 등, `weapon-creation-guide.md` 참고)은 `SpriteRenderer.sortingOrder = 10`으로 만든다 — 항상 캐릭터(Player/Enemy/Boss, 0) 위에 그려지게.
- Player·Enemy 사이(둘 다 0)는 현재 순서 미정 — 필요해지면 그때 값을 나눈다(§1 "미리 만들지 않기" 원칙).
