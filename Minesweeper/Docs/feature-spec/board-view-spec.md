# 보드·칸·렌더링 명세 — BoardModel / CellView / BoardConfigData / BoardTileSet / BoardCameraFitter

> [system-design.md](../system-design.md) §3.1 "보드"·§3.4 "렌더링"의 세부 명세 (분할 계획 1·4번을 한 문서로 묶음).
> 흐름·상태·이벤트는 [game-flow-spec.md](game-flow-spec.md)가 맡는다. 이 문서는 **칸과 보드, 그리고 화면에 그리는 방식**을 정한다.
> 이 문서는 **기획뿐**이며 구현(코드·씬·에셋)은 시작하지 않았다.
> 이 문서와 다른 문서가 충돌하면 먼저 차이를 짚고, 어느 쪽을 고칠지 정한 뒤 함께 동기화한다.
>
> 작성일: 2026-10-01

---

## 1. 확정된 설계 결정

| # | 결정 | 비고 |
|---|------|------|
| 1 | **`BoardModel`은 `MonoBehaviour`**, 씬의 `Board` GameObject에 부착 | 순수 C# 모델 방식 폐기 |
| 2 | 각 칸의 값은 **`CellView`(프리팹)** 가 가진다. 모델은 `CellView[]`로 칸에 접근 | 데이터 + 표시를 한 컴포넌트가 가짐 |
| 3 | 칸 배치는 Unity **`Grid` 컴포넌트**(월드 스페이스) | **Tilemap은 쓰지 않는다.** 최초 결정(Tilemap)에서 변경 |
| 4 | **`CellView`가 자기 자신의 사건을 판단**하고 `GameManager`에 알린다 | 지뢰가 열림 / 안전 칸이 열림 / 깃발이 바뀜 |
| 5 | **승패 집계는 `GameManager`** 가 한다 | 승리 조건 = 지뢰가 아닌 칸을 모두 열기 (클래식) |
| 6 | 칸은 **스스로 갱신**한다 (`SpriteRenderer` 교체) | `BoardView`, `OnCellsChanged`, `BoardResult`, `Outcome`, `CellPos` 불필요 |
| 7 | 좌표는 `Vector2Int` | 모델이 `UnityEngine`을 쓸 수 있으므로 |
| 8 | 승리 시 자동 깃발 / 패배 시 지뢰 공개는 **`GameManager`가 상태 전이 직후 `Board` 메서드를 호출**해 시작 | 처리 자체는 `BoardModel`이 수행 |

---

## 2. 구조

```
[씬] Board (GameObject)
 ├─ Grid                     셀 크기 1×1, 칸 배치 담당
 ├─ BoardModel               보드 단위 일: 칸 생성·지뢰 배치·연쇄 오픈·코딩·풀링
 └─ (자식) CellView × (W×H)  프리팹 인스턴스. 칸의 데이터 + SpriteRenderer

[씬] Main Camera ── BoardCameraFitter
```

### 호출 방향
```
Input ──▶ GameManager ──▶ BoardModel ──▶ CellView ──(알림)──▶ GameManager
```
- 명령은 위에서 아래로 (`Reveal`/`ToggleFlag`/`Chord`).
- **알림은 `CellView`에서 `GameManager`로 되돌아간다.** 의도된 순환이며, 알림 메서드는 3개로 고정한다 (§4.3).
- `CellView`는 알림 전에 `GameManager.Instance != null`을 확인한다 (테스트에서 `GameManager` 없이 `BoardModel`을 돌릴 수 있도록).

### 파일 목록
| 폴더 | 파일 | 종류 |
|------|------|------|
| `Board/` | `BoardModel` | MonoBehaviour (`[RequireComponent(typeof(Grid))]`) |
| | `CellView` | MonoBehaviour (프리팹) |
| | `CellState` | `enum { Hidden, Revealed, Flagged }` |
| | `BoardConfigData` | ScriptableObject |
| `View/` | `BoardTileSet` | ScriptableObject (칸 스프라이트 14종) |
| | `BoardCameraFitter` | MonoBehaviour |
| `Editor/` | `CellSpriteGenerator` | 에디터 전용 (스프라이트 시트 생성) |

폐기: `BoardView`, `Cell`(struct), `CellPos`, `BoardResult`, `Outcome`.

---

## 3. BoardConfigData (SO)

| 필드 | 설명 |
|------|------|
| `_displayName` | 난이도 표시 이름 |
| `_order` | 난이도 UI 정렬 순서 |
| `_width`, `_height`, `_mineCount` | 보드 크기와 지뢰 수 |

- `[SerializeField] private` + 읽기 프로퍼티만 공개.
- `OnValidate`: `_mineCount`를 `1 ~ W*H−9`로 보정 (첫 클릭 보호 영역 최대 9칸 확보).
- 에셋: `Resources/Boards/`에 `Beginner`(9×9/10) · `Intermediate`(16×16/40) · `Expert`(30×16/99). 난이도 UI가 `Resources.LoadAll<BoardConfigData>("Boards")`를 `_order`로 정렬해 쓴다 (이전 프로젝트의 SO 카탈로그 패턴).
- 에셋 생성은 Unity MCP로 한다.

---

## 4. CellView (프리팹)

### 4.1 프리팹 구성
루트에 `SpriteRenderer` + `CellView`. **콜라이더 없음** — 클릭 판정은 `Grid.WorldToCell`로 하므로 480개 콜라이더가 필요 없다.

### 4.2 데이터와 메서드
**데이터** (`[SerializeField] private`, 읽기 프로퍼티 공개)
| 필드 | 설명 |
|------|------|
| `Pos` (`Vector2Int`) | 자기 좌표 |
| `IsMine` | 지뢰 여부 |
| `AdjacentMines` | 인접 지뢰 수 (0~8) |
| `State` (`CellState`) | `Hidden` / `Revealed` / `Flagged` |
| `_endMark` | `None` / `Mine` / `Exploded` / `WrongFlag` — **표시 전용**, 데이터가 아님 |

**메서드** (C#에는 friend가 없으므로 public이지만 **호출자를 제한**한다)
```csharp
// 호출자: BoardModel
void Init(Vector2Int pos, BoardTileSet tileSet);  // 풀에서 꺼낼 때. 전체 리셋(_endMark 포함)
void SetMine();
void SetAdjacent(int n);
bool Reveal();       // State == Hidden일 때만 호출. 지뢰였으면 true 반환. 알림 포함
bool ToggleFlag();   // Hidden ↔ Flagged. 바뀌었으면 true. 알림 포함. Revealed에서는 false
// 호출자: BoardModel (GameManager의 지시로) — 알림 없음
void ForceFlag();    // 승리 후 자동 깃발
void ShowMine();     // 패배 후: 깃발 없는 지뢰 공개
void ShowWrongFlag();// 패배 후: 지뢰 아닌 칸의 깃발에 X
```

### 4.3 판단과 알림 (핵심 계약)
`CellView`는 **자기에게 일어난 일**만 판단하고, `GameManager`가 가진 알림 메서드 3개 중 하나를 호출한다.

| 사건 | `CellView`가 하는 일 | 알림 |
|------|----------------------|------|
| 안전 칸이 열림 | `State = Revealed` → 스프라이트 갱신 → 알림 | `GameManager.NotifySafeRevealed(CellView)` |
| 지뢰 칸이 열림 | `State = Revealed`, `_endMark = Exploded` → 스프라이트 갱신 → 알림 | `GameManager.NotifyMineRevealed(CellView)` |
| 깃발이 바뀜 | `State` 토글 → 스프라이트 갱신 → 알림 | `GameManager.NotifyFlagChanged(bool flagged)` |

- **알림은 항상 자기 갱신(스프라이트 교체)이 끝난 뒤** 한다. `GameManager`가 알림 안에서 종료 처리를 해도 자기 칸은 이미 완성된 상태여야 한다.
- 승리 조건이 "안전 칸을 모두 열기"이므로 **깃발의 정답 여부는 판정에 쓰지 않는다.** 깃발 알림은 개수(지뢰 카운터)만 바꾼다. (첫 클릭 전에는 어느 칸이 지뢰인지 모르므로 깃발 정답 여부를 승리에 쓰면 배치 직후 재계산이 필요해지는 문제도 없다.)
- `ForceFlag`/`ShowMine`/`ShowWrongFlag`는 **알림을 보내지 않는다.** 종료 후 화면 정리이므로 `GameManager`가 집계에 다시 반영할 필요가 없다.

### 4.4 스프라이트 결정 규칙 (`Refresh`)
| 우선순위 | 조건 | 스프라이트 |
|---------|------|------------|
| 1 | `_endMark == Exploded` | ExplodedMine |
| 2 | `_endMark == Mine` | Mine |
| 3 | `_endMark == WrongFlag` | WrongFlag |
| 4 | `State == Flagged` | Flag |
| 5 | `State == Revealed` | `Number[AdjacentMines]` |
| 6 | `State == Hidden` | Hidden |

풀에서 재사용될 때 이전 판의 `_endMark`가 남지 않도록 `Init`에서 반드시 초기화한다.

---

## 5. BoardModel

### 5.1 소유물과 직렬화 필드
- `CellView[] _cells` (1차원, `index = y*W + x`), `ObjectPool<CellView>` (convention: 풀링은 `UnityEngine.Pool.ObjectPool<T>`), `Grid`, `Width`, `Height`, `MineCount`, `_minesPlaced`.
- `[SerializeField] CellView _cellPrefab;` `[SerializeField] BoardTileSet _tileSet;`
- **집계값(열린 칸 수, 깃발 수)은 모델이 갖지 않는다.** `GameManager`가 알림으로 센다 (단일 출처).

### 5.2 공개 표면 (고정)
```csharp
int  Width  { get; }   int Height { get; }   int MineCount { get; }

void     Initialize(BoardConfigData config, System.Random rng);
CellView GetCell(Vector2Int pos);
bool     TryWorldToCell(Vector3 world, out Vector2Int pos);  // Input이 사용. Grid.WorldToCell + 범위 검사

void Reveal(Vector2Int pos);
void ToggleFlag(Vector2Int pos);
void Chord(Vector2Int pos);

void FlagAllMines();   // GameManager가 Won 전이 직후 호출
void ShowAllMines();   // GameManager가 Lost 전이 직후 호출
```
- 모든 연산은 **반환값이 없다.** 결과는 `CellView`의 알림으로 `GameManager`에 전달된다.
- `Initialize`는 `Awake`가 아니라 **명시적 메서드** — EditMode 테스트에서 `AddComponent`만 하면 `Awake`가 돌지 않기 때문. `rng`는 테스트 재현용 주입.

### 5.3 Initialize
1. 기존 `CellView`를 모두 풀에 반환.
2. `W×H`개를 풀에서 꺼내 `grid.GetCellCenterWorld(new Vector3Int(x, y, 0))`로 배치.
3. 각 칸 `Init(pos, tileSet)` → 전부 `Hidden`. `_minesPlaced = false`.

### 5.4 Reveal
```
Reveal(pos):
  cell = GetCell(pos)
  if cell.State != Hidden: return           ← 깃발·열린 칸이면 아무 일 없음 (지뢰 배치도 하지 않음)
  if !_minesPlaced: PlaceMines(pos)
  if cell.Reveal(): return                   ← 지뢰였음 (GameManager가 Lost 처리)
  if cell.AdjacentMines == 0: FloodFrom(cell)
```
**중요 가드**: 첫 클릭이 깃발 칸이라 무시됐는데 지뢰가 배치되면, 이후 진짜 첫 클릭이 보호 영역 밖에서 일어날 수 있다. 그래서 **`Hidden` 칸 검사를 지뢰 배치보다 먼저** 한다.

**PlaceMines(safePos)**
1. `safePos`와 8방 이웃(경계는 잘라냄)을 제외한 칸을 후보 인덱스 배열로 만든다.
2. Fisher-Yates 부분 셔플로 앞 `MineCount`개를 뽑아 `SetMine()`.
3. 각 지뢰의 8방 이웃에 `SetAdjacent` 누적 (O(지뢰 수 × 8)).
4. `_minesPlaced = true`.

**FloodFrom(start)** — `Queue<CellView>` BFS. **재귀 금지.**
- 이웃 중 `Hidden`(깃발 제외)인 칸을 `Reveal()` (안전 칸만 열림 — 0칸의 이웃은 지뢰일 수 없다).
- 열린 칸이 `AdjacentMines == 0`이면 큐에 넣는다. **`Reveal()`로 `Revealed`가 된 뒤 큐에 넣으므로** 중복 삽입이 없다.

### 5.5 Chord
```
Chord(pos):
  cell = GetCell(pos)
  if cell.State != Revealed or cell.AdjacentMines == 0: return
  if (이웃 중 Flagged 수) != cell.AdjacentMines: return      ← 무동작
  foreach 이웃 n where n.State == Hidden:
     if n.Reveal(): return                                    ← 지뢰를 열면 즉시 중단 (GameManager가 Lost 처리)
     if n.AdjacentMines == 0: FloodFrom(n)
```
- **지뢰를 열면 즉시 루프를 중단**한다. 남은 이웃을 계속 열면 `GameManager`가 `Lost` 처리하며 `ShowAllMines()`를 호출한 뒤에도 칸이 열리는 일이 생긴다. 열리지 않은 지뢰는 `ShowAllMines()`가 보여 준다.

### 5.6 ToggleFlag
`cell.ToggleFlag()` 호출만 한다 (`Hidden ↔ Flagged`, `Revealed`는 무시). 깃발 수 상한은 두지 않는다 (지뢰 카운터가 음수가 될 수 있음).

### 5.7 종료 후 화면 정리 (GameManager가 호출)
| 메서드 | 하는 일 | 데이터 `State` |
|--------|---------|---------------|
| `FlagAllMines()` | 지뢰인데 깃발이 없는 칸에 `ForceFlag()` | 변경됨 (`Flagged`) |
| `ShowAllMines()` | 깃발 없는 `Hidden` 지뢰에 `ShowMine()`, 지뢰 아닌 `Flagged` 칸에 `ShowWrongFlag()` | **변경 없음** (표시만) |

- 폭발 칸은 `CellView`가 열릴 때 스스로 `Exploded`로 표시하므로 `ShowAllMines()`는 건드리지 않는다.
- 이 두 메서드는 **지뢰 칸 또는 깃발 칸만** 건드리고 안전하게 열린 칸의 데이터는 건드리지 않는다. (`GameManager` 알림 처리 도중, 즉 `BoardModel`의 연쇄 오픈 루프가 아직 도는 중에 호출될 수 있기 때문.)
- `FlagAllMines()`는 알림을 보내지 않으므로 깃발 수는 `GameManager`가 직접 `MineCount`로 맞춘다 (game-flow-spec §5).

### 5.8 종료 후 방어
`BoardModel` 연산은 상태를 모른다. **종료 후 입력 차단은 `GameManager`**가 한다 (`IsFinished`면 `Reveal`/`ToggleFlag`/`Chord`를 `Board`에 전달하지 않음).

---

## 6. View/

### 6.1 BoardTileSet (SO) — 칸 스프라이트 14종
Tilemap을 쓰지 않으므로 `TileBase`가 아닌 **`Sprite`** 를 가진다. (이름은 `system-design.md`와 맞추기 위해 유지. 필요하면 `CellSpriteSet`으로 변경 가능.)

| 구분 | 내용 | 수 |
|------|------|----|
| 닫힌 칸 | Hidden, Flag | 2 |
| 열린 칸 | 숫자 0~8 | 9 |
| 종료 표시 | Mine, ExplodedMine, WrongFlag | 3 |
| **합계** | | **14** |

(이전 문서의 "타일 16종"은 오류였다. 14종이 맞다.)

### 6.2 스프라이트 생성 (Editor 스크립트)
- 스프라이트가 아직 없으므로 **절차 생성**: 32×32, 숫자 1~8은 5×7 도트 폰트 3배, 고전 지뢰찾기 색.
- 임포트: PPU 32 (1칸 = 1유닛), Point 필터, 압축·밉맵 없음.
- **권장: 14칸을 한 장의 시트(예: 128×128)로 만들어 14개로 슬라이스** → 480개 `SpriteRenderer`가 한 텍스처를 공유. (14장 개별 PNG도 동작하며 이 규모에서는 성능 문제가 아님.)
- 경계선은 스프라이트 안에 그려 넣어 소수 배율에서도 칸 사이 틈이 보이지 않게 한다.
- 메뉴 `Tools/Minesweeper/Generate Sprites` → `BoardTileSet`에 자동 연결. 이후 사용자가 원하는 스프라이트로 교체 가능.

### 6.3 BoardCameraFitter
`GameManager.OnBoardCreated` 수신 시와 창 크기 변경 시(`Update`에서 `Screen.width/height` 비교) 계산한다. 보드 영역은 `[0,W]×[0,H]` (셀 (0,0)의 중심이 (0.5, 0.5)).

```
size_세로 = (H + 2·pad) / (2·(1 − t))        t = 상단 HUD가 차지하는 화면 높이 비율
size_가로 = (W + 2·pad) / (2·화면비)
orthoSize = max(size_세로, size_가로)
카메라 x  = W / 2
카메라 y  = H/2 + orthoSize · t              HUD로 가려진 만큼 보드를 아래로 내려 남은 영역의 가운데에 맞춤
```
- Canvas Scaler를 1920×1080(높이 기준 매칭)으로 두면 `t`가 해상도와 무관한 상수 (`[SerializeField]`로 노출).
- **정수 배율 스냅(권장)**: 셀이 화면에서 정수 픽셀이 되도록 내린다.
  ```
  ppc       = floor( min( 가용높이px / (H + 2·pad), 화면너비px / (W + 2·pad) ) )
  orthoSize = 화면높이px / (2 · ppc)
  ```
  타일 경계가 화면 픽셀에 정렬되어 틈이 없다. 고급(30×16)은 1080p에서 셀 약 54px. 카메라 위치도 1/ppc 격자에 스냅하면 반픽셀 어긋남이 없다.

---

## 7. 개발 순서 (이 영역)

1. **asmdef**: `Scripts` 런타임 asmdef, `Scripts/Editor` Editor 전용 asmdef, `Tests`는 런타임을 참조하는 테스트 asmdef.
   - asmdef 없이 `Assembly-CSharp`에 두면 테스트 어셈블리가 참조할 수 없다.
   - Editor 폴더를 분리하지 않으면 빌드가 `UnityEditor` 참조 때문에 깨진다.
2. `CellState`, `CellView`
3. `BoardConfigData` + SO 3종 (Unity MCP)
4. 스프라이트 생성기 → `BoardTileSet` 에셋 → `CellView` 프리팹
5. `BoardModel`을 단계별로 구현: 배치 → 숫자 → 오픈·BFS → 깃발 → 코딩 → 종료 후 정리. 단계마다 테스트
6. 씬 `Board` 오브젝트(Grid + BoardModel) 구성, `GameManager` 구현 연결 (알림 메서드 3개 포함)
7. `BoardCameraFitter`
8. 검증

---

## 8. 위험·주의

| # | 위험 | 대응 |
|---|------|------|
| 1 | 첫 클릭이 깃발 칸인데 지뢰가 배치돼 이후 첫 클릭 안전이 깨짐 | §5.4: `Hidden` 검사를 지뢰 배치보다 먼저 |
| 2 | 코딩으로 지뢰를 여러 개 열 때 `Lost` 알림이 여러 번 | §5.5: 첫 지뢰에서 루프 중단. `GameManager`도 종료 후 알림은 무시 |
| 3 | 알림이 오는 도중(연쇄 오픈 루프 중) `GameManager`가 `ShowAllMines`를 호출해 데이터가 꼬임 | §5.7: 두 메서드는 지뢰·깃발 칸만 건드림 |
| 4 | `CellView` 알림 시점에 자기 갱신이 안 끝남 | §4.3: 알림은 항상 갱신 후 |
| 5 | 풀 재사용 시 이전 판의 `_endMark`·상태가 남음 | `Init`에서 전부 리셋 |
| 6 | `CellView`의 변경 메서드가 public이라 아무나 호출 가능 | §4.2 "호출자" 규칙을 명세·convention에 명시, 리뷰 시 호출처 검사 |
| 7 | `CellView → GameManager` 순환 의존 | 알림 3개로 고정, `Instance` null 가드. 부담이 커지면 `CellView`의 static 이벤트로 교체 (호출부 변경 최소) |
| 8 | 모델이 MonoBehaviour라 규칙 단위 테스트가 이전 설계보다 무거움 | `Awake` 대신 `Initialize`. 보드 규칙은 EditMode(임시 GameObject + 프리팹), **승패 판정은 `GameManager`가 필요하므로 PlayMode 테스트** |
| 9 | 칸 사이 틈(소수 배율) | 스프라이트에 경계선 + 정수 배율 스냅 |
| 10 | Editor 코드가 런타임 어셈블리에 섞임 | Editor 전용 asmdef |
| 11 | 난이도 전환 시 이전 칸 잔상 | `Initialize`가 먼저 전부 풀에 반환한 뒤 새로 꺼냄 |

---

## 9. 검증 관점

### EditMode 테스트 (`BoardModel`, 시드 고정, `GameManager` 없이)
1. 첫 클릭 칸과 이웃 8칸에 지뢰가 없다
2. 지뢰 수가 설정값과 정확히 같다
3. `AdjacentMines` 계산이 맞다
4. 0칸 연쇄 오픈 범위가 맞고 깃발 칸은 열리지 않는다
5. 첫 클릭이 깃발 칸이면 지뢰가 배치되지 않고, 이후 첫 클릭이 안전하다
6. 코딩: 정상 / 오깃발로 지뢰 열림 / 깃발 수 불일치 시 무동작 / 지뢰를 열면 즉시 중단
7. `FlagAllMines`가 지뢰에만 깃발을 꽂고, `ShowAllMines`가 `State`를 바꾸지 않고 `_endMark`만 설정한다
8. `Initialize` 반복 호출 시 이전 판의 잔상이 없다
9. 고급 보드를 수천 번 생성해도 예외가 없다

### PlayMode 테스트 / 수동 확인 (`GameManager` 연동)
- 안전 칸 알림 → 열린 칸 수 집계, 전부 열면 `Won`
- 지뢰 알림 → `Lost`, 코딩으로 여러 지뢰를 열어도 `Lost`는 한 번
- 깃발 알림 → 지뢰 카운터 증감 (음수 허용)
- 1920×1080 · 1280×720 · 2560×1080에서 난이도 3종 스크린샷 → 칸 정렬·틈 없음·카메라 맞춤
- 모서리 칸 클릭 좌표 정확도, 난이도 전환 후 잔상 없음
