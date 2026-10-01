# 게임 흐름 명세 — GameManager / GameState

> [system-design.md](../system-design.md) §3.2 "상태·흐름"의 세부 명세 (분할 계획 2번).
> 칸·보드·렌더링은 [board-view-spec.md](board-view-spec.md)가 맡는다.
> 이 문서는 **기획뿐**이며 구현(코드·씬)은 시작하지 않았다.
> 이 문서와 다른 문서가 충돌하면 먼저 차이를 짚고, 어느 쪽을 고칠지 정한 뒤 함께 동기화한다.
>
> 작성일: 2026-10-01 (같은 날 개정: `CellView` 알림 방식 반영) · 참고: 이전 프로젝트 `Core/GameManager.cs`·`GameState.cs`·`Singleton.cs`, `convention.md` "GameManager 공개 표면"

---

## 1. 목적·범위

### GameManager가 맡는 것
- 게임 상태(`GameState`) 보유와 전이
- **승패 집계**: `CellView`가 보내는 알림을 세어 승리를 판단하고, 패배 알림을 받아 종료 처리
- 현재 `BoardModel` 참조 (`[SerializeField]`) 와 판 시작(`Board.Initialize`)
- 타이머, 남은 지뢰 카운터(`MinesRemaining`)
- 입력이 호출하는 동작 창구 (`Reveal` / `ToggleFlag` / `Chord`)와 이벤트 발행
- 종료 후 화면 정리 **시작** (승리: `Board.FlagAllMines()`, 패배: `Board.ShowAllMines()`)

### 맡지 않는 것
| 대상 | 담당 |
|------|------|
| 한 칸에 일어난 사건의 판단 (지뢰가 열림 / 안전 칸이 열림 / 깃발이 바뀜) | `CellView` |
| 지뢰 배치, 0칸 연쇄 오픈, 코딩, 종료 후 화면 정리의 **실행** | `BoardModel` |
| 칸 그리기, 카메라 맞춤 | `CellView`, `BoardCameraFitter` |
| 마우스 → 셀 좌표 해석, UI 위 클릭 차단 | Input |
| 화면 표시(카운터·타이머·결과 패널) | UI |

GameManager는 **얇게** 유지한다. 칸의 사건은 `CellView`가 판단해 알려 주고, GameManager는 그것을 세어 상태·이벤트로 옮긴다.

---

## 2. GameState

```csharp
// Core/GameState.cs
public enum GameState { Ready, Playing, Won, Lost }
```

이전 프로젝트의 `Boot, Title, Playing, LevelUpPaused, GameOver, Clear`에서 바뀐 점:
| 제거 | 이유 |
|------|------|
| `Boot`, `Title` | 타이틀 화면이 없다. 앱이 켜지면 바로 `Ready` |
| `LevelUpPaused` | 일시정지 개념이 없다 (`Time.timeScale` 미사용) |
| `GameOver`, `Clear` → `Lost`, `Won` | 지뢰찾기 용어로 교체 |

### 상태별 허용 동작
| 동작 | Ready | Playing | Won / Lost |
|------|:-----:|:-------:|:----------:|
| `Reveal` | O (첫 오픈 때 Playing으로) | O | 무시 |
| `Chord` | O (열린 숫자 칸이 없어 모델이 무동작) | O | 무시 |
| `ToggleFlag` | O | O | 무시 |
| 타이머 누적 | X | O | X |
| `StartNewGame` / `Restart` | O | O | O |

---

## 3. GameManager 공개 표면 (고정)

`Singleton<GameManager>` 상속 (제공 인프라 `Singleton.cs`는 이전 프로젝트에서 복사). 다른 시스템이 의존하는 표면만 고정하고 내부 구현은 자유.

```csharp
// Core/GameManager.cs
GameState        State           { get; }
BoardConfigData  Config          { get; }   // 현재 판의 설정
BoardModel       Board           { get; }   // 씬의 Board 오브젝트 ([SerializeField] 참조). 수정은 GameManager 경유
int              ElapsedSeconds  { get; }   // 정수 초, 상한 999
int              MinesRemaining  { get; }   // 지뢰 수 − 깃발 수. 음수 허용
bool             IsFinished      { get; }   // State == Won || State == Lost

static event System.Action<BoardModel> OnBoardCreated;      // 새 판 시작 (BoardCameraFitter: 카메라 맞춤)
static event System.Action             OnFlagCountChanged;  // 깃발 수 변화 (값은 MinesRemaining 폴링)
static event System.Action<int>        OnTimeChanged;       // 정수 초가 바뀔 때만
static event System.Action<GameState>  OnStateChanged;      // 상태 전이

// 입력(Input)이 호출
void StartNewGame(BoardConfigData config);  // 난이도 변경
void Restart();                             // = StartNewGame(Config)
void Reveal(Vector2Int pos);
void ToggleFlag(Vector2Int pos);
void Chord(Vector2Int pos);

// CellView만 호출 (알림). 다른 시스템은 호출 금지
void NotifySafeRevealed(CellView cell);     // 안전 칸이 열렸다
void NotifyMineRevealed(CellView cell);     // 지뢰 칸이 열렸다
void NotifyFlagChanged(bool flagged);       // 깃발이 꽂힘(true) / 뽑힘(false)
```

- **이벤트는 static** — `GameManager.OnStateChanged += ...`처럼 `Instance` 없이 구독/해제한다. UI의 `OnEnable`이 GameManager의 `Awake`보다 먼저 돌 수 있기 때문 (이전 프로젝트와 동일한 이유).
- **구독은 `OnEnable`, 해제는 `OnDisable`.**
- **`ChangeState`는 private.** 이전 프로젝트는 public이었지만, 여기서는 입력·UI가 상태를 임의로 바꾸지 못하게 하고 전이는 위 메서드를 통해서만 일어난다.
- `[SerializeField] private BoardConfigData _startConfig;` — 시작 난이도. `[SerializeField] private BoardModel _board;` — 씬의 `Board`. 씬에 단일 인스턴스로 존재하는 매니저는 SO 카탈로그 대신 `[SerializeField] private`을 쓴다는 convention 예외에 해당.
- 내부 집계값(비공개): `_revealedSafe`(열린 안전 칸 수), `_flagCount`(깃발 수), `_safeTotal`(= 가로×세로 − 지뢰 수). **집계의 단일 출처는 GameManager**이며 `BoardModel`은 갖지 않는다.
- 좌표는 `Vector2Int` (모델이 `UnityEngine`을 쓰므로 자체 좌표 타입은 두지 않는다).

---

## 4. 상태 전이표

```
        StartNewGame(any)
 ┌──────────────────────────────────┐
 ▼                                  │
Ready ──(첫 NotifySafeRevealed)──▶ Playing ──(전부 열림)──▶ Won ──┐
                                      └──(NotifyMineRevealed)──▶ Lost ─┤
                                                                       └─▶ StartNewGame
```

| 트리거 | 현재 상태 | 가드 | 결과 |
|--------|-----------|------|------|
| `StartNewGame` / `Restart` | 모든 상태 | 없음 | 집계 리셋, `Board.Initialize` → `Ready` |
| `Reveal` / `Chord` / `ToggleFlag` (Input) | Won, Lost | — | **무시** (`Board`에 전달하지 않음) |
| `Reveal` / `Chord` / `ToggleFlag` (Input) | Ready, Playing | 없음 | `Board`의 해당 메서드 호출 |
| `NotifySafeRevealed` | Ready | — | `Playing`으로 전이 → 아래 줄 계속 |
| `NotifySafeRevealed` | Ready, Playing | — | `_revealedSafe++`. `== _safeTotal`이면 **`Won`** |
| `NotifyMineRevealed` | Ready, Playing | — | **`Lost`** |
| `NotifyFlagChanged` | Ready, Playing | — | `_flagCount ± 1`, `OnFlagCountChanged` |
| `Notify*` 전부 | Won, Lost | — | **무시** (코딩으로 지뢰를 여러 개 열어도 `Lost`는 한 번) |

핵심 가드: **`Ready → Playing`은 "첫 안전 칸이 실제로 열렸다는 알림"을 받을 때** 일어난다. 깃발 칸을 눌러 모델이 무시했거나 깃발만 꽂은 경우에는 알림이 오지 않으므로 타이머가 시작되지 않는다.

승리 조건(확정): **지뢰가 아닌 칸을 모두 연다** (`_revealedSafe == _safeTotal`). 깃발의 정답 여부는 승리에 쓰지 않는다 (깃발은 카운터용).

극단 케이스: 첫 클릭 한 번으로 곧바로 승리하는 경우(작은 보드)에는 `Playing`을 발행한 뒤 `Won`을 발행한다. 상태 전이는 항상 순서대로 발행해 구독자가 `Ready → Won`으로 건너뛰지 않게 한다.

---

## 5. 이벤트 발행 순서 (계약)

### 종료 처리 `EndGame(state)`
`NotifySafeRevealed`(승리) 또는 `NotifyMineRevealed`(패배)가 호출한다.
```
1. State = state                       타이머·입력이 즉시 멈춘다
2. 종료 후 화면 정리
     Won : Board.FlagAllMines();  _flagCount = Board.MineCount;  OnFlagCountChanged 발행
     Lost: Board.ShowAllMines()
3. OnStateChanged(state)               항상 마지막
```
`OnStateChanged(Won/Lost)`를 마지막에 두는 이유: 결과 패널 등이 반응해 뜨기 전에 보드와 카운터가 이미 완성돼 있어야 한다. `FlagAllMines()`는 알림을 보내지 않으므로 깃발 수는 GameManager가 직접 맞춘다.

### 입력 한 번의 처리
| 입력 | 발행 순서 |
|------|-----------|
| 깃발 토글 | `OnFlagCountChanged` |
| 첫 오픈 | `OnStateChanged(Playing)` (첫 안전 칸 알림 시점) → (그 입력으로 끝나면) 종료 처리 §5 상단 |
| 일반 오픈 / 코딩 | (끝나면) 종료 처리 §5 상단 |

한 입력 안에서 `OnStateChanged(Won/Lost)`는 **항상 가장 마지막**이다.

### StartNewGame
```
1. 집계 리셋, Board.Initialize(config, rng), _safeTotal 계산
2. OnBoardCreated       Board 전달
3. OnTimeChanged(0)
4. OnFlagCountChanged
5. OnStateChanged(Ready)   — 이미 Ready였어도 항상 발행 (결과 패널 숨김 등을 일관되게)
```

### 알림 도중 재진입 주의
알림은 `BoardModel`의 연쇄 오픈·코딩 루프 **도중**에 올 수 있다. 그 안에서 `EndGame`이 `Board.FlagAllMines()`/`ShowAllMines()`를 호출하므로, 이 두 메서드는 **지뢰 칸·깃발 칸만 건드리고 열린 안전 칸의 데이터는 건드리지 않는다** (board-view-spec §5.7). 코딩은 첫 지뢰에서 루프를 중단한다 (board-view-spec §5.5).

---

## 6. 타이머

- `Update()`에서 **`State == Playing`일 때만** `Time.deltaTime`을 누적한다 (이전 프로젝트 패턴).
- 누적값이 **999에서 멈춘다** (게임은 계속 진행). 결과 패널 시간도 같은 값.
- `OnTimeChanged`는 **정수 초가 바뀔 때만** 발행한다 (표시는 floor, 0에서 시작).
- `Time.timeScale`은 건드리지 않는다.
- `Won`/`Lost` 전이와 동시에 누적이 멈춘다 (Update 가드로 자연히 보장).
- `StartNewGame`에서 누적값을 0으로 리셋하고 `OnTimeChanged(0)`을 발행한다.

---

## 7. 초기화 시점

- `Awake()`: `base.Awake()`만 (Singleton 등록).
- `Start()`: `StartNewGame(_startConfig)` 호출. **모든 오브젝트의 `Awake`/`OnEnable`이 끝난 뒤**라서 `OnEnable`에서 구독한 UI·`BoardCameraFitter`가 최초 `OnBoardCreated`/`OnStateChanged(Ready)`를 놓치지 않는다 (이전 프로젝트의 `Start()`에서 `ChangeState(Title)` 패턴과 동일).
- 씬 리로드를 쓰지 않으므로 `Awake`/`Start`는 앱 시작 시 한 번만 돈다. 이후 모든 초기화는 `StartNewGame` 한 경로로 통일한다.

---

## 8. 구현 스케치 (참고용 의사코드)

> 완성 코드가 아니라 흐름 확인용이다.

```csharp
public void StartNewGame(BoardConfigData config)
{
    Config = config;
    _revealedSafe = 0;  _flagCount = 0;  _elapsed = 0f;  _shownSeconds = 0;

    _board.Initialize(config, new System.Random());
    _safeTotal = _board.Width * _board.Height - _board.MineCount;

    OnBoardCreated?.Invoke(_board);
    OnTimeChanged?.Invoke(0);
    OnFlagCountChanged?.Invoke();
    ChangeState(GameState.Ready);                 // 항상 발행
}

// ── Input이 호출 ──
public void Reveal(Vector2Int pos)     { if (IsFinished) return; _board.Reveal(pos); }
public void ToggleFlag(Vector2Int pos) { if (IsFinished) return; _board.ToggleFlag(pos); }
public void Chord(Vector2Int pos)      { if (IsFinished) return; _board.Chord(pos); }

// ── CellView가 호출 (알림) ──
public void NotifySafeRevealed(CellView cell)
{
    if (IsFinished) return;
    if (State == GameState.Ready) ChangeState(GameState.Playing);

    _revealedSafe++;
    if (_revealedSafe == _safeTotal) EndGame(GameState.Won);
}

public void NotifyMineRevealed(CellView cell)
{
    if (IsFinished) return;                       // 코딩으로 여러 개 열려도 한 번만
    EndGame(GameState.Lost);
}

public void NotifyFlagChanged(bool flagged)
{
    if (IsFinished) return;
    _flagCount += flagged ? 1 : -1;
    OnFlagCountChanged?.Invoke();
}

private void EndGame(GameState result)
{
    State = result;                               // 1. 타이머·입력 즉시 정지 (OnStateChanged는 마지막에)
    if (result == GameState.Won)
    {
        _board.FlagAllMines();
        _flagCount = _board.MineCount;
        OnFlagCountChanged?.Invoke();
    }
    else _board.ShowAllMines();
    OnStateChanged?.Invoke(result);               // 3. 항상 마지막
}

private void Update()
{
    if (State != GameState.Playing) return;

    _elapsed = Mathf.Min(_elapsed + Time.deltaTime, 999f);
    int seconds = (int)_elapsed;
    if (seconds != _shownSeconds) { _shownSeconds = seconds; OnTimeChanged?.Invoke(seconds); }
}
```

`EndGame`은 `ChangeState`와 달리 상태 대입과 이벤트 발행 사이에 화면 정리를 끼워 넣는다 (§5 순서). 구현 시 `ChangeState` 안에서 처리하든 별도 메서드로 두든 §5 순서를 만족해야 한다.

---

## 9. 다른 문서와의 관계 (이번 개정으로 반영 완료)

`system-design.md`는 같은 날 이 설계에 맞춰 함께 개정했다.

| # | 항목 | 내용 |
|---|------|------|
| 1 | 이벤트 | `OnBoardCreated`, `OnTimeChanged` 추가. **`OnCellsChanged` 제거** (칸이 스스로 갱신하므로 불필요) |
| 2 | 발행 순서 | 상태 변경(`Won`/`Lost`)은 항상 마지막 |
| 3 | 판단 위치 | 칸의 사건 판단은 `CellView`, 집계·승패 전이는 `GameManager` (`BoardResult`/`Outcome` 없음) |
| 4 | 좌표 타입 | `Vector2Int` |
| 5 | `OnFlagCountChanged` | 값을 싣지 않고 `MinesRemaining`을 읽는다 |
| 6 | `ChangeState` | 이전 프로젝트는 public, 여기서는 private |
| 7 | `Ready`의 깃발 | 허용. 깃발만으로는 `Playing`으로 전이하지 않음 |
| 8 | 승리 조건 | 안전 칸을 모두 열면 승리 (깃발 정답 여부는 미사용) |

---

## 10. BoardModel / CellView에 요구하는 것

GameManager가 동작하려면 다음이 필요하다. 상세 시그니처는 [board-view-spec.md](board-view-spec.md).

| 요구 | 제공 |
|------|------|
| `CellView`가 자기 사건을 판단해 알림 3개 중 하나를 보냄 | board-view-spec §4.3 |
| 알림은 칸의 자기 갱신이 끝난 뒤에 보냄 | board-view-spec §4.3 |
| `Width`, `Height`, `MineCount` | `_safeTotal` 계산, `FlagAllMines` 후 깃발 수 보정 |
| `Initialize(config, rng)` | `StartNewGame` |
| `Reveal` / `ToggleFlag` / `Chord` | Input 명령 전달 |
| `FlagAllMines()` / `ShowAllMines()` (알림 없음, 안전 칸 데이터 불변) | `EndGame`의 종료 후 정리 |
| 첫 `Reveal` 시 지연 배치 (`Hidden` 칸일 때만 배치) | 첫 클릭 안전 |
| `BoardConfigData`의 이름·정렬 필드, 설정 검증 | 난이도 UI, `mines ≤ 가로×세로 − 9` |

---

## 11. 위험·빈틈 검토

| # | 위험 | 대응 |
|---|------|------|
| 1 | 깃발 칸을 눌러 모델이 무시했는데 `Playing`으로 전이·타이머 시작 | §4 가드: 안전 칸이 실제로 열렸다는 알림을 받을 때만 전이 |
| 2 | 코딩으로 지뢰를 여러 개 열어 `Lost`가 중복 처리됨 | `IsFinished`면 알림 무시 + 코딩 루프는 첫 지뢰에서 중단 |
| 3 | 이벤트 순서가 뒤바뀌어 결과 패널이 보드 정리보다 먼저 뜸 | §5: `OnStateChanged`는 항상 마지막, 화면 정리·깃발 수 갱신이 먼저 |
| 4 | 연쇄 오픈 루프 도중 `ShowAllMines`/`FlagAllMines`가 호출돼 데이터가 꼬임 | 두 메서드는 지뢰·깃발 칸만 변경 (board-view-spec §5.7) |
| 5 | 비활성 GameObject에 붙은 UI가 이벤트를 못 받음 (`OnEnable`이 안 돌아 구독 누락) | 결과 패널은 **항상 활성인 컨트롤러 오브젝트**가 구독하고, 패널 오브젝트만 껐다 켠다 |
| 6 | 난이도 변경 시 이전 보드 잔재·이벤트 중복 구독 | 구독은 `OnEnable`/`OnDisable`, 보드 교체는 `Board.Initialize`가 풀 반환 후 재생성 |
| 7 | `CellView`가 `GameManager.Instance`가 없는 상태(EditMode 테스트 등)에서 알림 | `Instance != null` 가드 |
| 8 | `Singleton.Instance`는 `Destroy` 시 해제되지 않음 | 씬 리로드를 쓰지 않아 현재는 무해. 쓰게 되면 `OnDestroy`에서 해제하도록 인프라 수정 필요 |
| 9 | 도메인 리로드를 끄면 static 이벤트 구독이 플레이 사이에 남음 | 현재 설정은 `m_EnterPlayModeOptions: 0`(리로드 켜짐)이라 무해. 끄게 되면 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`로 이벤트를 `null` 초기화 |
| 10 | `GameManager` 판정 테스트가 EditMode에서 어려움 (`Awake`가 돌지 않아 `Instance`가 없음) | 승패 판정은 **PlayMode 테스트**로 검증. 별도 `GameSession` 클래스 분리는 토이 범위에 과해 채택하지 않음 |
| 11 | 타이머가 999에서 멈춘 뒤 `OnTimeChanged`가 계속 발행 | 정수 값이 바뀔 때만 발행하므로 999 이후엔 발행 없음 |
| 12 | `CellView → GameManager` 순환 의존 | 알림 3개로 고정. 부담이 커지면 `CellView`의 static 이벤트로 교체 |

### 열린 질문
- 타이머를 클래식처럼 첫 클릭 시 **1부터 시작**할지 (현재 안: 0부터).
- 진행 중(`Playing`) 난이도 변경 시 확인 창을 둘지 — GameManager는 확인 없이 즉시 리셋하는 것으로 두고, 확인 UI는 UI 명세에서 결정.

---

## 12. 검증 관점

`GameManager`는 MonoBehaviour이고 `Awake`에서 `Instance`가 만들어지므로 **PlayMode 테스트 또는 플레이 모드 수동 확인**으로 검증한다. 보드 규칙은 `BoardModel` EditMode 테스트가 담당한다 (board-view-spec §9).

### 상태 전이
1. 앱 시작 직후 `State == Ready`, 타이머 0, `MinesRemaining == 지뢰 수`
2. 깃발만 꽂고 오픈하지 않으면 `Ready` 유지, 타이머 정지
3. 깃발 칸을 눌러도 `Ready` 유지
4. 첫 오픈 시 `Playing`, 타이머 시작
5. 지뢰를 열면 `Lost`, 지뢰가 아닌 칸을 모두 열면 `Won`, 타이머 정지
6. 코딩으로 지뢰를 여러 개 열어도 `Lost`는 한 번만 발행된다
7. `Won`/`Lost` 후 `Reveal`/`Chord`/`ToggleFlag` 무시
8. 어느 상태에서든 `Restart`/난이도 변경 시 `Ready`로 리셋

### 이벤트·수치
9. 발행 순서가 §5와 같다 (임시 로그 구독자 또는 MCP `read_console`로 확인)
10. 승리 시 `MinesRemaining == 0`, 지뢰에 자동 깃발
11. 패배 시 지뢰 공개·폭발 칸 강조·오깃발 X, 연쇄 오픈 중 종료돼도 데이터 이상 없음
12. 깃발을 지뢰 수보다 많이 꽂으면 `MinesRemaining`이 음수
13. 타이머가 999에서 멈추고 `OnTimeChanged`가 정수 초마다 한 번씩만 발행
14. 난이도를 여러 번 바꿔도 이벤트 중복 발행·칸 잔상이 없다
