# 무기(Weapon) 추가 가이드 — Knife(단검) 구현 패턴 정리

> 단검(Knife)을 실제로 구현하면서 밟은 절차를 일반화한 문서. **다음 무기(도끼·창) 추가 시 이 문서를 보고 반복**한다.
> **적(Enemy) 가이드와 결정적으로 다른 점**: 잡몹은 신규 타입 추가가 대부분 "데이터(SO 자산) 생성 + 웨이브 등록"만으로 끝나지만(코드는 제네릭이라 그대로 재사용), 무기는 **도끼(회전 궤도)·창(유도 투사체)이 단검(직선 발사·최근접 타겟)과 동작 자체가 다르다.** 그래서 이 문서는 "그대로 복붙"이 아니라 "재사용할 것 / 새로 짤 것"을 구분해서 안내한다.

## 0. 전제 조건 (한 번만 — Knife 때 이미 끝남, 다시 안 건드림)

- 인터페이스: `Assets/Scripts/Core/Interfaces/IWeapon.cs` — `Data`/`Level`/`CurrentDamage` 프로퍼티 + `Tick(float)`/`ApplyUpgrade(WeaponUpgradeType)` 메서드. **시그니처 고정**(convention §4) — 새 무기라고 멤버를 추가/변경하지 않는다.
- 열거형: `Assets/Scripts/Weapon/WeaponKind.cs`(`Knife`/`Axe`/`Spear`), `Assets/Scripts/Upgrade/WeaponUpgradeType.cs`(`Damage`/`Cooldown`/`ExtraProjectile`) — **3종 값 이미 존재**. 새 이름을 추가할 필요 없음(도끼·창도 이미 `WeaponKind`에 있음).
- SO 스키마: `Assets/Scripts/Weapon/WeaponData.cs` — 19개 필드(모든 무기 공용, 단검 전용/도끼 전용/창 전용 필드가 섞여 있음: `pivotAngularSpeed`/`selfSpinSpeed`/`orbitRadius`/`orbitDuration`/`orbitAngularSpeed`/`homingTurnRate`는 단검엔 안 쓰이고 0으로 둠). **필드를 추가/변경하지 않는다** — convention §4에 고정된 계약. 이 무기와 무관한 필드도 전부 0으로 두면 됨(Knife가 오빗/호밍 필드를 0으로 둔 선례와 동일).
- 무기 관리: `Assets/Scripts/Weapon/PlayerWeapons.cs` — `Dictionary<WeaponKind, IWeapon>`, `AddWeapon(kind)`/`TryGet(kind, out weapon)`/`Tick(deltaTime)`. `AddWeapon`이 `Resources.Load<WeaponData>($"Weapons/{kind}Data")`로 개별 로드 후 `CreateWeapon` switch로 `IWeapon` 구현체를 생성함 — **이 switch에 새 case를 추가하는 게 이 가이드의 핵심 작업**.
- 참고용 구현체(직선 발사 패턴): `Assets/Scripts/Weapon/Knife.cs`(plain C#, `IWeapon` 구현) + `Assets/Scripts/Weapon/KnifeProjectile.cs`(MonoBehaviour, 투사체).
- 레이어 6종(`Player`/`Enemy`/`Boss`/`PlayerProjectile`/`EnemyProjectile`/`Pickup`) + Physics2D 충돌 매트릭스(convention §6) 세팅 완료 — `PlayerProjectile`은 `Enemy`/`Boss`와만 충돌하도록 이미 구성됨.
- `PlayerController.Awake()`에 `Weapons = new PlayerWeapons(transform); Weapons.AddWeapon(WeaponKind.Knife);`, `Update()`에 `Weapons.Tick(Time.deltaTime)` 연결 완료(단검은 시작 무기라 즉시 부여됨 — **아래 4-4 경고 참고, 도끼/창은 이 줄을 그대로 따라 하면 안 됨**).
- `Resources/Weapons/KnifeData.asset` 생성 완료 — 3절 예시로 재사용.

**Enemy.prefab과 다른 점**: 적은 프리팹 1종을 모든 타입이 공유하지만, 무기 투사체는 **무기마다 별도 프리팹**이다(스프라이트·충돌 크기·컴포넌트 구성이 무기별로 다를 수 있음). 도끼처럼 투사체가 아예 없는 무기는 프리팹 대신 "플레이어 자식으로 상시 유지되는 오브젝트" 하나만 있으면 된다(5절 참고).

새 무기 추가할 때 위는 재사용만 하고, 아래 1~6단계만 반복한다.

## 1. 스펙값 확인

`Docs/feature-spec/05-weapon.md` 표에서 해당 무기의 데미지·쿨다운·사거리·투사체 속도(있다면)를 확인한다.

| 무기 | 발사 패턴 | 데미지 | 쿨다운 | 사거리 |
|------|-----------|--------|--------|--------|
| 도끼 (Axe) | 플레이어 주변 회전(3개) | 6 | 지속(재타격 0.5초 고정) | 2.3 (반경) |
| 창 (Spear) | 유도 투사체 — 발사 후 적 추적 | 15 | 1.5초 | (유도, 속도 15.6) |

- 표에 **없는** 구체 수치(예: 도끼의 정확한 `orbitAngularSpeed`, 창의 정확한 `homingTurnRate`)는 추측해서 채우지 않는다 — 사용자에게 직접 물어본다.
- 강화 규칙(05-weapon.md 문단 참고): 도끼는 **데미지 강화만** 존재(쿨다운·ExtraProjectile 강화 없음, 도끼 회전 개수도 불변). 창은 **강화 자체가 없음**(보조 무기).

## 2. 스프라이트 확인

- `Assets/_GameKit/Sprites/axe1.png`~`axe3.png`(도끼, 강화단계=회전체 스프라이트 겸용, 단검의 `sword1~3`과 동일한 패턴) 또는 `Assets/_GameKit/Sprites/spear.png`(창, 강화 없어 단일 스프라이트) 존재 확인.
- **없으면 여기서 멈춘다.** 에디터에게 준비를 요청하고 사용자에게 알린다 — Claude가 대신 만들어내지 않는다.

## 3. `{Name}Data.asset` 생성 — UnityMCP `manage_scriptable_object`

새 SO 클래스를 만들지 않는다 — 기존 `WeaponData` 타입을 그대로 재사용해 자산 인스턴스만 새로 만든다.

### 3-1. 자산 생성
```
manage_scriptable_object(action="create", type_name="WeaponData",
  folder_path="Assets/Resources/Weapons", asset_name="{Name}Data")
```

### 3-2. 스칼라 필드 패치 (반드시 `dry_run: true`로 먼저 검증 후 실제 적용)

KnifeData.asset 실제 값(참고용 — 새 무기는 05-weapon.md 표 값으로 교체):
```json
[
  {"path": "_displayName", "value": "단검"},
  {"path": "_damage", "value": 10},
  {"path": "_cooldown", "value": 0.6},
  {"path": "_range", "value": 12.5},
  {"path": "_projectileCount", "value": 1},
  {"path": "_projectileSpeed", "value": 21.9},
  {"path": "_projectileLifetime", "value": 2},
  {"path": "_upgradeDamageRate", "value": 0.2},
  {"path": "_upgradeCooldownRate", "value": 0.15},
  {"path": "_cooldownFloor", "value": 0.15},
  {"path": "_projectileSpreadOffset", "value": 0.3}
]
```
이 무기와 무관한 필드(`_pivotAngularSpeed`/`_selfSpinSpeed`/`_orbitRadius`/`_orbitDuration`/`_orbitAngularSpeed`/`_homingTurnRate`)는 전부 `0`으로 둔다. **필드 소유권은 convention.md:223-235의 주석에 명시돼 있다 — `pivotAngularSpeed`/`selfSpinSpeed`는 도끼 전용, `orbitRadius`/`orbitDuration`/`orbitAngularSpeed`는 창 전용이다("orbit"이라는 이름만 보고 회전 무기=도끼로 착각하지 말 것 — 실제로 궤도 필드는 창 쪽이다).** 도끼라면 `_pivotAngularSpeed`/`_selfSpinSpeed`를 채우고 나머지(`_projectileSpeed`/`_projectileLifetime` 등 투사체 전용 필드, `_orbit*` 계열)를 0으로, 창이라면 `_orbitRadius`/`_orbitDuration`/`_orbitAngularSpeed`(대기 단계)와 `_homingTurnRate`(선회율)를 채우고 강화 관련 필드(`_upgradeDamageRate` 등)는 스펙상 강화가 없으므로 0으로 둔다.

### 3-3. `_prefab`/`_stageSprites` 참조 패치 (별도 호출 — 함정 주의, enemy-creation-guide.md 3-3과 동일 원리)

- object reference 패치는 `{"path": "...", "ref": {...}}`처럼 **patch 아이템의 형제 키**로 `ref`를 둔다. `{"value": {"ref": ...}}`처럼 중첩하면 실패한다.
- 배열(`_stageSprites`)도 `.Array.size`는 직접 patch 불가 — `data[0]`/`data[1]`/`data[2]`를 바로 지정하면 배열이 자동으로 늘어난다.
- 텍스처 안에 서브스프라이트가 2개 이상이면 `path`만으로는 "Multiple compatible sub-assets found" 에러가 남 → `guid` + `spriteName` 조합 사용. guid는 `Assets/_GameKit/Sprites/{name}.png.meta`의 `guid:` 값.
- 프리팹 참조(`_prefab`)는 subasset이 아니라 단일 에셋이므로 `guid`만 지정하면 된다(스프라이트처럼 `spriteName` 불필요).

```json
[
  {"path": "_prefab", "ref": {"guid": "<{Name}Projectile.prefab의 guid>"}},
  {"path": "_stageSprites.Array.data[0]", "ref": {"guid": "<axe1.png의 guid>", "spriteName": "axe1"}},
  {"path": "_stageSprites.Array.data[1]", "ref": {"guid": "<axe2.png의 guid>", "spriteName": "axe2"}},
  {"path": "_stageSprites.Array.data[2]", "ref": {"guid": "<axe3.png의 guid>", "spriteName": "axe3"}}
]
```
투사체가 없는 무기(도끼)는 `_prefab`을 비워두거나(회전체는 무기 클래스가 직접 관리) 회전체 프리팹을 만들었다면 그 guid를 넣는다 — 어느 쪽으로 할지 결정 후 진행 전 사용자에게 짚어준다(§5 설계 논의 원칙).

## 4. 코드 작성

공통: `{Name}.cs`(plain C#, `IWeapon` 구현, **MonoBehaviour 아님** — Knife.cs와 동일 구조)를 만들고, `PlayerWeapons.cs`의 `CreateWeapon` switch에 case를 추가한다.

```csharp
case WeaponKind.Axe:
    return new Axe(data, _owner);
```

### 4-1. 도끼(Axe) — 회전 궤도, 투사체 없음

- **투사체 없음** — 생성자에서 회전체(블레이드) GameObject를 `projectileCount`(초기 3)개 **1회** `Instantiate`해 플레이어 자식(`_owner`)으로 유지한다. 발사형이 아니므로 `ObjectPool` 불필요(Knife처럼 풀을 만들지 않는다). `AxeBlade` 프리팹은 `KnifeProjectile.prefab`과 달리 **Rigidbody2D/Collider2D가 필요 없다**(`SpriteRenderer`만 — 데미지 판정은 트리거 충돌이 아니라 무기 클래스가 직접 `OverlapCircleAll`로 수행하기 때문). `SpriteRenderer.sortingOrder = 10`(convention §8) — 안 맞추면 적과 겹칠 때 뒤로 그려짐.
- `Tick(deltaTime)`은 두 가지를 함: (1) 피봇각을 `pivotAngularSpeed` 기준으로, 블레이드 자전각을 `selfSpinSpeed` 기준으로 각각 누적 갱신해 블레이드들을 `_owner.position`에서 **`_data.Range`** 반경 위(`i*360/count`씩 균등 분산)에 배치·자전시킨다(**주의: `orbitRadius`가 아니다** — 그 필드는 창 전용이고, 도끼는 별도 궤도 반경 필드가 없어 `Range`를 targeting 반경처럼 재사용한다). (2) `Physics2D.OverlapCircleAll(_owner.position, _data.Range)`으로 접촉 중인 적을 찾아 데미지 — **동일 적을 다시 때리려면 `WeaponData.Cooldown`(고정 0.5초) 간격이 지나야 함**(적별로 마지막 타격 시각을 따로 추적해야 함 — Knife의 "쿨다운 1개"와 달리 "적 개체별 쿨다운"이 필요하다는 점이 구조적으로 다름, `Dictionary<IDamageable, float>`로 추적).
- `ApplyUpgrade(type)`는 `WeaponUpgradeType.Damage`만 스택 반영, `Cooldown`/`ExtraProjectile`은 무시(도끼엔 해당 강화가 없음 — 스펙 §5-2 "도끼 회전 개수는 불변" 근거).

### 4-2. 창(Spear) — 유도 투사체, 강화 없음 (대기 선회 → 유도 2단계, 확정됨)

- `Tick(deltaTime)`은 Knife와 거의 동일(쿨다운 관리 + `FindNearestTarget` + 발사, 후보 0마리면 쿨다운 미소모 — 05-weapon.md 공통 규칙). Knife와 달리 다중 발사(`projectileSpreadOffset`)가 없으므로 `Fire`는 항상 1발만 쏜다.
- **"대기" 단계의 정체(사용자 확인 완료)**: 발사 즉시 유도로 날아가는 게 아니라, **발사 지점(스폰 위치, 플레이어를 따라가지 않음)에서 `orbitDuration`초 동안 `orbitRadius` 반경으로 제자리 선회**(차징 모션)한 뒤 유도로 전환된다. 타겟은 **발사 시 1회만 캡처**(`Transform`을 들고 있음, Knife처럼 매 프레임 재탐색 안 함).
- `{Name}Projectile.cs`는 `KnifeProjectile.cs` 구조(Awake `TryGetComponent`+로그 가드, `ObjectPool` Get/Release, `OnTriggerEnter2D`+`_isReleased` 중복 릴리즈 가드, `FixedUpdate`에서 `GameManager.Instance.State` 게이팅)를 그대로 재사용하되 `FixedUpdate`를 2단계로 분기한다:
  - **대기(선회) 단계** (`_remainingOrbitTime > 0`): 선회각을 `orbitAngularSpeed * deltaTime`만큼 누적 → `스폰위치 + (cos,sin)(선회각) * orbitRadius`로 `MovePosition`. 스프라이트는 궤도 접선 방향(이동 방향, 선회각 ± 90°)을 보도록 회전 — 위치가 도는 것과 별개로 스프라이트가 그 방향을 향해야 자연스럽다.
  - **유도 단계**(대기 종료 후): 타겟이 살아있으면(`activeInHierarchy` + `IDamageable.IsAlive` 둘 다 확인 — 풀링된 적이 비활성화됐거나 죽은 경우 배제) 현재 진행각을 타겟 방향각 쪽으로 `Mathf.MoveTowardsAngle`로 `homingTurnRate * deltaTime`만큼만 회전(초과 회전 금지, 급선회 방지) → 새 방향으로 `_velocity` 갱신. 타겟이 무효화되면 마지막 `_velocity` 그대로 직진 유지(05-weapon.md "타겟 소멸 시 직진 후 수명"). 매 틱 `_velocity * projectileSpeed * deltaTime`만큼 이동.
  - 스프라이트 회전은 기존 Knife와 동일한 아트 보정식(`atan2(dir) - 90°`, 12시 방향이 창끝인 아트 기준)을 두 단계 모두 재사용.
- `ApplyUpgrade(WeaponUpgradeType type)`는 **계약상 구현은 있어야 하지만**(`IWeapon` 시그니처 고정, §4) **내부 로직 없음**(빈 메서드) — 창은 강화가 없는 보조 무기이기 때문. 3택 업그레이드 카드 목록에 창의 강화가 노출되지 않도록 막는 것은 06장(업그레이드 시스템) 쪽 책임이라 여기서 강제하지 않는다 — 이 갭이 있다는 것만 인지하고 넘어간다.

### 4-3. 공통 경고 — 시작 시 자동 부여하지 말 것

05-weapon.md: "무기 1번(단검)은 시작 시 보유, 2·3번(도끼·창)은 레벨업 3택에서 획득 가능." `PlayerController.Awake()`의 `Weapons.AddWeapon(WeaponKind.Knife)` 줄을 그대로 본떠 도끼·창도 Awake에 추가하면 스펙 위반이다. 지금 단계에서는 `PlayerWeapons.AddWeapon(WeaponKind.Axe)`가 (테스트 코드나 `execute_code`로) 정상 동작하는지까지만 검증하고, 실제 게임 플레이 중 부여 시점 연결은 업그레이드 선택 시스템(9장, 아직 미구현) 몫으로 남긴다.

## 5. 프리팹 생성 (투사체가 있는 무기만 — 창)

`KnifeProjectile.prefab`을 만들 때 실제로 겪은 함정:

- GameObject 생성 시 `manage_gameobject create`의 `component_properties`로 스프라이트를 인라인 지정하면 **실패**한다(`"Property 'sprite' not found"`, 트랜잭션이라 오브젝트 자체가 생성 안 됨). → 먼저 빈 GameObject를 생성한 뒤, 별도 `manage_components set_property` 호출로 스프라이트를 지정한다. 이때 **SerializedProperty 경로명 `m_Sprite`를 쓴다(C# 프로퍼티명 `sprite`가 아니다)**.
- `manage_prefabs create_from_gameobject`의 `target`에 GameObject의 인스턴스 ID(음수 정수)를 넘기면 "GameObject not found" 에러가 난다. → **GameObject 이름 문자열**(`"SpearProjectile"`)을 넘긴다.
- 컴포넌트 구성: `SpriteRenderer`(`sortingOrder = 10` — convention §8, 안 맞추면 적과 겹칠 때 뒤로 그려짐) + `Rigidbody2D`(Kinematic, `gravityScale=0`) + `Collider2D`(`isTrigger=true`) + `{Name}Projectile` 스크립트. Layer = `PlayerProjectile`(이미 세팅된 충돌 매트릭스가 `Enemy`/`Boss`와만 충돌하도록 처리해줌 — 코드에서 레이어를 직접 확인할 필요 없음, `KnifeProjectile.OnTriggerEnter2D`의 `TryGetComponent<IDamageable>` 선례와 동일).

도끼는 투사체가 아니므로 이 절이 해당 없다 — 회전체는 플레이어 자식 GameObject로 무기 클래스가 직접 `Instantiate`해 들고 있는다(4-1 참고). 씬에 미리 배치가 필요한지, 프리팹으로 만들지는 진행 전 사용자에게 확인한다(에디터 배치는 CLAUDE.md 작업 분담상 사용자 영역).

## 6. 검증

- `read_console`로 에러/경고 0 확인.
- Play 모드 진입 → 근처에 적 스폰 후 자동 발사/회전/접촉 데미지 확인.
- **주의 1 (Play 모드 프레임 자동 진행이 불규칙함)**: 이 환경은 MCP 호출 사이 Play 모드 프레임이 **항상 멈춰 있는 것도, 항상 실시간으로 흐르는 것도 아니다** — `Time.frameCount`가 몇 초씩 멈춰 있다가 특정 호출(주로 `Instantiate`/씬 변경처럼 무거운 작업) 뒤에 수천 프레임이 한꺼번에 몰아서 진행되는 등 불규칙하게 버스트로 진행되는 게 관찰됐다(Spear 검증 때 확인). 이 배경 틱이 테스트 중인 무기와 무관한 다른 무기(예: 시작 무기 Knife)나 `EnemySpawner`도 같이 돌려서 결과를 오염시킬 수 있다 — **테스트용 적·플레이어는 다른 무기 사거리 밖(예: 좌표를 (100,100)처럼 멀리)로 이동시켜 격리**하고, 특정 인스턴스를 다시 찾을 땐 `FindObjectsByType` 전체 순회 후 자신이 발사한 투사체인지 `_target` 필드(리플렉션)로 정확히 매칭해서 집어야 한다(엉뚱한 풀 인스턴스를 잡지 않도록).
- **주의 2 (`Rigidbody2D.MovePosition`은 물리 스텝이 실제로 돌아야 반영됨)**: `FixedUpdate()`를 리플렉션으로 직접 호출해도 `MovePosition`으로 요청한 위치는 **`Physics2D.Simulate()`가 실행되기 전까진 `transform.position`에 반영되지 않는다**(요청만 큐잉됨). 따라서 이동·회전 로직을 결정론적으로 검증하려면: `Physics2D.simulationMode`를 `SimulationMode2D.Script`로 바꾼 뒤, `FixedUpdate()` 리플렉션 호출 → `Physics2D.Simulate(Time.fixedDeltaTime)` 호출을 한 쌍으로 반복하고, **끝나면 반드시 `simulationMode`를 원래 값(`FixedUpdate`)으로 되돌린다**(부작용 방지, Knife 때부터 확인된 원칙).
- `execute_code`로 `Tick(deltaTime)`은 직접 호출(계약상 public), 내부 private 필드(예: `_pool`, `_cooldownTimer`, `_remainingOrbitTime`, `_velocity`, `_target`)는 리플렉션으로 조회·조작한다.
- 확인 항목: 데미지 적용(대상 HP 감소), 쿨다운/재타격 간격, (투사체가 있다면) `ObjectPool` Get/Release 동작, `ApplyUpgrade` 스택 반영(도끼는 Damage만, 창은 무반응 확인), 유도형이면 대기 반경 준수(`Vector2.Distance`로 스폰 지점과의 최대 거리 확인) + 선회율 상한 준수(`homingTurnRate * deltaTime` 이상 한 틱에 꺾이지 않는지) + 타겟 소멸 시 직진 유지.

## 7. 이 패턴으로 안 커버되는 것

- 도끼(궤도)·창(유도)은 이미 서로 다른 하위 패턴이다. **도끼 이후 또 다른 회전/궤도형 근접무기**가 추가되면 이 문서의 4-1(도끼)을 참고하고, **창 이후 또 다른 유도 투사체**가 추가되면 4-2(창)를 참고한다 — 즉 3가지 스펙(직선/궤도/유도)을 벗어나는 새로운 발사 패턴이 나오면 이 가이드로 커버되지 않으니, 조용히 끼워 맞추지 말고 먼저 차이를 짚는다.
- `WeaponData`에 없는 새 수치가 필요해 보이는 경우(예: 궤도 무기가 아닌 다른 이동 패턴): 필드 추가는 convention §4 SO 필드 계약을 바꾸는 일이라 **조용히 추가하지 말고 먼저 사용자에게 알린다**.
- **창(Spear)의 "대기" 단계 — 해결됨**: 이전에는 이 gap을 사용자 확인 전으로 남겨뒀으나, `/add-weapon spear` 실행 시 확인해 4-2절에 확정 반영함(발사 지점 제자리 선회 → 유도 전환). 새로운 유도형 무기를 또 추가할 일이 있으면 이 확정된 구조를 선례로 참고하되, 수치(반경·시간·속도)는 여전히 그 무기 스펙에서 새로 확인할 것.
- 업그레이드 카드 3택 노출/선택 로직 자체(창의 "강화 없음"을 UI에서 어떻게 숨기는지 포함)는 9장(업그레이드 시스템) 범위 — 이 가이드의 스코프 밖.

## 참고: 완료된 예시 (Knife)

| 필드 | 값 | 비고 |
|---|---|---|
| displayName | 단검 | |
| damage | 10 | |
| cooldown | 0.6 | |
| range | 12.5 | |
| projectileCount | 1 | |
| projectileSpeed | 21.9 | |
| projectileLifetime | 2 | |
| upgradeDamageRate | 0.2 | |
| upgradeCooldownRate | 0.15 | |
| cooldownFloor | 0.15 | |
| projectileSpreadOffset | 0.3 | |
| sword1/2/3 guid | `c1865122a5c01fa43b83dab877ce75a7` / `9ee2083b4ffd944489ad543ca2d3a0ec` / `0773902f7eacdcd438d0410bf53a2529` | `Assets/_GameKit/Sprites/` |
| KnifeProjectile.prefab guid | `cb6a14ac6978d2342a3f7cfe5e714d6e` | |
| WeaponData 스크립트 guid | `38ba9ce2cfc259948a50373588f8b6dc` | 새 `{Name}Data.asset`도 동일 스크립트 참조 |
| 풀 prewarm/maxSize | 50 / 200 | `Knife.cs`의 `PrewarmCount` 상수 — 인프라 파라미터라 SO 필드가 아니라 코드에 하드코딩(convention §3 "prewarm 50" 명세와 EnemySpawner 선례 기준) |

## 참고: 완료된 예시 (Axe)

| 필드 | 값 | 비고 |
|---|---|---|
| displayName | 도끼 | |
| damage | 6 | |
| cooldown | 0.5 | 적별 재타격 간격(고정, 강화 없음) |
| range | 2.3 | 궤도 반경 + 접촉판정 반경 겸용(전용 필드 없음) |
| projectileCount | 3 | 블레이드 개수(불변) |
| pivotAngularSpeed | 120 | deg/sec, 스펙에 없어 사용자 확인 |
| selfSpinSpeed | 360 | deg/sec, 스펙에 없어 사용자 확인 |
| upgradeDamageRate | 0.2 | Damage 강화만 존재 |
| axe1/2/3 guid | `53dc8e039e15f5f438173232bfbe5a46` / `0f3d81cc667b88147af4e6617c52ac67` / `520492fd40611c54bb3f759d44e6996c` | |
| AxeBlade.prefab guid | `4bb6dd1ce864cdc499c6db097eda2003` | SpriteRenderer만(물리 컴포넌트 없음), `sortingOrder=10` |

## 참고: 완료된 예시 (Spear)

| 필드 | 값 | 비고 |
|---|---|---|
| displayName | 창 | |
| damage | 15 | |
| cooldown | 1.5 | |
| range | 12.5 | 표에 수치 없어 Knife와 동일값으로 사용자 확인 |
| projectileCount | 1 | 다중 발사 없음 |
| projectileSpeed | 15.6 | |
| projectileLifetime | 3 | |
| orbitRadius | 0.3 | 대기 선회 반경, 스펙에 없어 사용자 확인 |
| orbitDuration | 0.2 | 대기 선회 시간(초), 스펙에 없어 사용자 확인 |
| orbitAngularSpeed | 540 | 대기 선회 속도(deg/sec), 스펙에 없어 사용자 확인 |
| homingTurnRate | 270 | 유도 최대 선회율(deg/sec), 스펙에 없어 사용자 확인 |
| spear guid | `04bf59deb66f4ce439047f55ae445e7e` | 단일 스프라이트, `_stageSprites[0]`만 사용(강화 없어 고정) |
| SpearProjectile.prefab guid | `316aee9fe4e820c45b85408ac3bc0d1e` | `sortingOrder=10` |
