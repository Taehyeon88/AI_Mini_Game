# 잡몹(Enemy) 추가 가이드 — Rat 구현 패턴 정리

> Rat(쥐)을 실제로 구현하면서 밟은 절차를 일반화한 문서. **새 잡몹 타입(Bat·Cyclops 등) 추가 시 이 문서만 보고 반복**하면 됨(convention.md §1 "적 추가는 같은 패턴 반복" 원칙).
> 코드 새로 짤 일은 거의 없음 — 신규 타입 추가는 대부분 **데이터(SO 자산) 생성 + 웨이브 등록**만으로 끝남. `Enemy.cs`/`EnemyData.cs`/`EnemySpawner.cs`/`WaveStage.cs`는 제네릭이라 수정 불필요.

## 0. 전제 조건 (한 번만 — Rat 때 이미 끝남, 다시 안 건드림)

- 스크립트: `Assets/Scripts/Enemy/{EnemyData,Enemy,WaveStage,EnemySpawner}.cs`
- 프리팹: `Assets/Prefabs/Enemy.prefab` — **제네릭 1종 공유**(타입별 프리팹 안 만듦). SpriteRenderer + CircleCollider2D(Is Trigger) + `Enemy.cs` + `SpriteFrameToggler.cs`, RB2D 없음, Layer=`Enemy`.
- 레이어 6종(`Player`/`Enemy`/`Boss`/`PlayerProjectile`/`EnemyProjectile`/`Pickup`) + Physics2D 충돌 매트릭스(convention §6) 세팅 완료.
- 씬의 `Managers`에 `EnemySpawner` 배치, `_mainCamera`/`_enemyPrefab` 연결 완료, `_stages` 배열 보유.

새 타입 추가할 때 위는 재사용만 하고, 아래 1~4단계만 반복한다.

## 1. 스펙값 확인

- `Docs/feature-spec/04-enemy.md` 표에서 타입의 `maxHP`·`moveSpeed`·`attackPower`·`dropExp`·등장 시점 확인.
- `Docs/feature-spec/08-wave-spawner.md` 표에서 어느 시간 구간에 등장하는지 확인(기존 구간에 섞이는지, 새 구간이 시작되는지).
- `contactDamageInterval`(공통 0.5)·`hitFlashDuration`(game-design.md "단색 플래시 0.1초") 같은 공통 값은 Rat과 동일하게 채움 — **로직에서는 아직 미사용**(EXP 드랍은 6장, 피격 플래시·사망 이펙트는 "폴리싱 4-10" 차시로 보류 중인 상태 그대로 유지).

## 2. 스프라이트 확인 (에디터 선행 작업 필요 여부 판단)

- `Assets/_GameKit/Sprites/{name}.png` 존재 확인.
- `.meta` 파일을 열어 `spriteMode: 2`(Multiple)이고 `{name}_0`/`{name}_1` 서브스프라이트로 이미 슬라이스돼 있는지 확인 (Rat 사례: 128×64 시트, 64×64 프레임 2장, PPU 64, `rat_0`/`rat_1`).
- **이미 슬라이스돼 있으면** 에디터 작업 없이 Claude가 바로 3단계 진행 가능(`_GameKit`은 강사 제공 에셋이라 대부분 이미 돼 있음).
- **안 돼 있으면** 에디터에게 안내하고 멈출 것: Sprite Editor ▸ Slice ▸ Type=Grid By Cell Size, Pixel Size 64×64.

## 3. `{Name}Data.asset` 생성 — UnityMCP `manage_scriptable_object`

### 3-1. 자산 생성
```
manage_scriptable_object(action="create", type_name="EnemyData",
  folder_path="Assets/Resources/Enemies", asset_name="{Name}Data")
```

### 3-2. 스칼라 필드 패치 (반드시 `dry_run: true`로 먼저 검증 후 실제 적용)
```json
[
  {"path": "_maxHP", "value": <int>},
  {"path": "_moveSpeed", "value": <float>},
  {"path": "_attackPower", "value": <int>},
  {"path": "_dropExp", "value": <int>},
  {"path": "_animInterval", "value": 0.25},
  {"path": "_contactDamageInterval", "value": 0.5},
  {"path": "_hitFlashDuration", "value": 0.1}
]
```

### 3-3. `sprites` 배열 (별도 호출 — 함정 주의)
- `_sprites.Array.size`는 patch로 **못 씀** ("Unsupported SerializedPropertyType: ArraySize" 에러) → size 패치는 생략하고 `data[0]`/`data[1]`을 바로 지정하면 배열이 자동으로 늘어남.
- object reference 패치는 `{"path": "...", "ref": {...}}`처럼 **patch 아이템의 형제 키**로 `ref`를 둠. `{"value": {"ref": ...}}`처럼 중첩하면 실패함("Object reference must contain 'instanceID', 'guid', 'path', or 'name'.").
- 텍스처 안에 서브스프라이트가 2개 이상이면 `path`만으로는 "Multiple compatible sub-assets found" 에러가 남 → 반드시 `guid` + `spriteName` 조합 사용. guid는 `Assets/_GameKit/Sprites/{name}.png.meta`의 `guid:` 값.

```json
[
  {"path": "_sprites.Array.data[0]", "ref": {"guid": "<{name}.png의 guid>", "spriteName": "{name}_0"}},
  {"path": "_sprites.Array.data[1]", "ref": {"guid": "<{name}.png의 guid>", "spriteName": "{name}_1"}}
]
```

## 4. WaveStage에 등록 — UnityMCP `manage_components`

- 08-wave-spawner.md 표를 보고 판단:
  - **기존 시간 구간에 섞여 들어가는 타입**(예: 30~75초에 박쥐 합류) → 그 stage의 `_enemyPool` 배열에 이 타입만 추가.
  - **새 시간 구간이 시작되는 타입**(예: 75초부터 사이클롭스 합류) → `_stages` 배열에 새 WaveStage 원소 자체를 추가(시작시간·간격·동시최대도 표에서).
- **핵심 함정 1**: `_stages` 같은 **커스텀 구조체 배열**은 통째로 JSON 리스트를 넣으면 배열 크기만 반영되고 내부 필드는 전부 기본값(0/null)으로 생성됨 → 반드시 `_stages.Array.data[i]._필드명` 개별 경로로 하나씩 `set_property` 호출.
- 반면 `_enemyPool`처럼 **단순 참조 타입 배열**은 한 번에 전체 값을 넣어도 정상적으로 채워짐(구조체 배열과 동작이 다름).
- **핵심 함정 2 (이미 값이 있는 구조체 배열에 새 stage 추가할 때)**: `_stages.Array.data[N]`(N = 현재 배열 크기, 즉 새로 추가하려는 인덱스)에 개별 필드든 통째 객체든 **직접 patch하면 "SerializedProperty not found" 에러**가 남 — 이 자동확장(auto-grow)은 배열이 **비어있을 때(size 0)만** 동작하고, 이미 원소가 있는 배열을 늘릴 때는 안 먹힘.
  - 해결: 먼저 `_stages` 전체에 원하는 최종 크기만큼의 더미 리스트(`[{}, {}, ...]`)를 넣어 **크기만** 맞춘다 → 이 순간 기존 원소 값도 전부 기본값으로 초기화되어 버림(부작용) → **기존 stage 포함 전체 stage의 필드를 처음부터 다시 개별 경로로 재입력**해야 함. 즉 stage를 하나 추가할 때마다 이전에 등록해둔 stage들의 값도 같이 재입력하는 게 정상 흐름이다(까먹지 말 것).
- **핵심 함정 3 (배열 patch 확인 시 grep 주의)**: 저장된 씬 파일에서 `_enemyPool` 참조는 에셋 **이름 문자열이 아니라 GUID**로 저장된다(`{fileID: ..., guid: <해당 .asset의 guid>, type: 2}`). `grep "BatData" Main.unity`처럼 이름으로 찾으면 실제로는 있는데도 못 찾아 "유실됐다"고 착각하기 쉬움 — 반드시 해당 `.asset`의 실제 guid로 검색하거나, UnityMCP 리소스(`mcpforunity://scene/gameobject/{id}/component/{name}`)로 구조화된 값을 확인할 것.

```
manage_components(action="set_property", target=<Managers instanceID>, search_method="by_id",
  component_type="EnemySpawner",
  property="_stages.Array.data[1]._enemyPool",
  value=[{"path":"Assets/Resources/Enemies/RatData.asset"}, {"path":"Assets/Resources/Enemies/BatData.asset"}])
```
(신규 stage를 만드는 경우 `_startTime`/`_spawnInterval`/`_maxConcurrent`는 각각 `_stages.Array.data[i]._필드명` 경로로 개별 호출.)

> `target`(Managers의 instanceID)은 세션마다 바뀔 수 있으므로 매번 `find_gameobjects(search_term="Managers")`로 새로 조회할 것 — 하드코딩 금지.

## 5. 검증

- `read_console`로 에러/경고 0 확인.
- Play 모드 진입 → `execute_code`로 `GameManager.Instance.ChangeState(GameState.Playing)` 호출(Title 스킵) → `FindObjectsByType<Enemy>()`로 새 타입도 섞여 스폰되는지, HP·속도가 데이터대로인지 확인.
- **주의**: MCP로 헤드리스 조작 중에는 에디터가 포커스 없이 백그라운드로 돌아 `Update()` 틱이 불규칙하게 늦게 뜀(디스폰 등 프레임 폴링 로직이 실시간보다 늦게 반영될 수 있음). 로직 자체가 맞는지는 리플렉션으로 private 메서드(`PollActiveEnemies` 등)를 직접 `Invoke`해서 검증 가능 — 실제 버그는 아니고 헤드리스 테스트 특유의 현상.

## 6. 이 패턴으로 안 커버되는 것 (타입별로 다시 판단)

- **Ghost(소환물)**: 웨이브 테이블에 안 들어감(보스가 직접 소환, 04-enemy.md/08-wave-spawner.md 마지막 줄). 3단계(EnemyData 자산 생성)까지는 동일하지만 4단계(WaveStage 등록)는 해당 없음 — 보스 챕터에서 별도 처리.
- **콜라이더 크기**: `Enemy.prefab`이 제네릭 1종 공유라 CircleCollider2D 반지름이 고정. 사이클롭스처럼 몸집이 다른 타입이 꼭 필요하면 `EnemyData`에 반지름 필드를 추가해야 하는데, 이는 convention.md §4 SO 필드 계약을 바꾸는 일이라 **조용히 추가하지 말고 먼저 차이를 짚을 것**.
- **EXP gem 드랍 / 사망 이펙트·피격 플래시**: Rat과 동일하게 보류 상태(6장 EXP&Level, "폴리싱 4-10"). 새 타입을 추가해도 이 두 가지는 자동으로 계속 보류됨 — 이 가이드의 스코프 밖.

## 참고: 완료된 예시 (Rat, Bat, Cyclops)

| 필드 | Rat | Bat | Cyclops | 비고 |
|---|---|---|---|---|
| maxHP | 10 | 15 | 40 | |
| moveSpeed | 2.3 | 4.4 | 1.6 | |
| attackPower | 5 | 6 | 12 | |
| dropExp | 1 | 2 | 3 | 값만 채움, 로직 미사용 |
| animInterval | 0.25 | 0.25 | 0.25 | 스펙에 수치 없어 기존 기본값과 통일(가정) |
| contactDamageInterval | 0.5 | 0.5 | 0.5 | |
| hitFlashDuration | 0.1 | 0.1 | 0.1 | 값만 채움, 로직 미사용 |
| sprites | rat_0/1 (`rat.png`, guid `006b84a8847b6bd479c18c2adabc39ca`) | bat_0/1 (`bat.png`, guid `15759199a8e196a47b95f79bbbc06d72`) | cyclops_0/1 (`cyclops.png`, guid `3c0ec379c426a4c4cb23697d1015bdcf`) | 전부 `Assets/_GameKit/Sprites/` |
| EnemyData.asset guid | `a2ce4b61820be604fae126e99d2df234` | `d43e052fa7512e9409ce1887ad394f13` | `01302474ff624e740bbe03756fff35fc` | 씬 파일에서 grep으로 확인할 때 이 guid를 찾을 것(핵심 함정 3) |
| 등록 위치 | `_stages.Array.data[0]` (start 0, interval 1.5, max 12, pool=[Rat]) | `_stages.Array.data[1]` (start 30, interval 1.0, max 20, pool=[Rat,Bat]) | `_stages.Array.data[2]` (start 75, interval 0.7, max 35, pool=[Rat,Bat,Cyclops]) | stage 추가할 때마다 이전 stage 전부 재입력 필요(핵심 함정 2) |

## 부록: 웨이브 즉시 테스트용 OnGUI 디버거

Play 모드에서 실제 경과 시간을 기다리지 않고 원하는 웨이브를 바로 재현하고 싶으면 `Assets/Scripts/Enemy/WaveDebugger.cs`(클래스 전체 `#if UNITY_EDITOR`로 감싼 MonoBehaviour, 씬의 `WaveDebugger` GameObject에 부착됨)를 사용한다. Game 뷰 좌상단에 stage별 버튼이 뜨고, 클릭하면 `EnemySpawner.DebugForceSpawnStage(index)`가 호출되어:
1. 기존에 떠 있는 적을 전부 정리(`_pool.Release`)하고
2. 그 stage로 강제 전환(`_debugForcedStageIndex`가 시간 기반 자동 전환을 오버라이드 — 안 그러면 다음 프레임에 실제 `ElapsedTime` 기준으로 원래 stage로 되돌아감)
3. 그 stage의 동시최대 수만큼 즉시 버스트 스폰 후, 이후에도 그 페이스로 계속 보충 스폰.

`EnemySpawner`에 `StageCount`/`DescribeStage(int)`/`DebugForceSpawnStage(int)` 3개 공개 API가 `#if UNITY_EDITOR`로 추가돼 있음(전부 빌드에는 포함 안 됨). 새 stage를 추가해도 버튼은 `StageCount` 기준으로 자동 늘어나므로 `WaveDebugger.cs`는 수정할 필요 없음.
