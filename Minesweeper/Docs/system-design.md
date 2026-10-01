# 지뢰찾기 미니게임 — 시스템 설계 (기반 문서)

> 이 문서는 지뢰찾기 프로젝트의 **기반(SoT) 문서**다. 세부 시스템 명세는 **이 문서를 근거로 분할**해 작성한다.
> 세부 명세가 이 문서와 충돌하면 먼저 차이를 짚고, 어느 쪽을 고칠지 정한 뒤 두 문서를 함께 동기화한다.
> 현재 단계는 **기획뿐**이며 구현(코드·씬·에셋)은 시작하지 않았다.
>
> 작성일: 2026-10-01 (같은 날 개정: Grid + CellView 구조, 판단·알림 방식 반영) · 엔진: Unity 6000.3.12f1 (URP, Input System 1.19, UGUI, 2D Grid)
>
> **세부 명세**
> - [feature-spec/game-flow-spec.md](feature-spec/game-flow-spec.md) — GameManager / GameState, 상태 전이, 이벤트, 타이머
> - [feature-spec/board-view-spec.md](feature-spec/board-view-spec.md) — BoardModel / CellView / BoardConfigData / 렌더링 / 카메라

---

## 1. 개요·범위

### 확정된 결정
| 항목 | 결정 |
|------|------|
| 규칙 범위 | 클래식 + 편의 기능 |
| 승리 조건 | 지뢰가 아닌 칸을 **모두 열면** 승리 (깃발 정답 여부는 승리에 쓰지 않음) |
| 입력·플랫폼 | PC 마우스 (Windows 빌드) |
| 보드 구성 | Unity **`Grid` 컴포넌트**(월드 스페이스) + **`CellView` 프리팹**(`SpriteRenderer`). **Tilemap은 쓰지 않음** |
| 보드 모델 | `BoardModel`은 `MonoBehaviour`. 칸의 값은 `CellView`가 가진다 |
| 판단과 집계 | 칸의 사건은 `CellView`가 판단해 `GameManager`에 알리고, 승패 집계는 `GameManager`가 한다 |
| UI | UGUI (Screen Space - Overlay) |
| 기준 해상도 | 1920×1080 가로 |

### 포함
- 난이도 3단계

  | 난이도 | 크기 (가로×세로) | 지뢰 |
  |--------|-----------------|------|
  | 초급 | 9×9 | 10 |
  | 중급 | 16×16 | 40 |
  | 고급 | 30×16 | 99 |

- 첫 클릭 안전 · 깃발 · 0칸 연쇄 오픈 · 코딩(숫자 칸 주변 한꺼번에 열기)
- 타이머 · 남은 지뢰 카운터 · 승/패 표시 · 재시작 · 난이도 변경

### 비스코프 (이번 범위 밖)
커스텀 보드 크기, 베스트 기록 저장, 물음표(?) 마크, 모바일 터치, 힌트/솔버, 무추측(no-guess) 생성, 사운드, 다국어.

---

## 2. 시스템 구성

### 2.1 시스템 목록과 책임
| 시스템 | 책임 |
|--------|------|
| 게임 흐름 (`GameManager`) | 상태 전이, 승패 집계, 타이머, 지뢰 카운터, 이벤트 발행, 종료 후 화면 정리 시작 |
| 보드 (`BoardModel`, `MonoBehaviour`) | 칸 생성·풀링, 첫 클릭 후 지뢰 배치, 0칸 연쇄 오픈, 코딩, 종료 후 화면 정리 실행 |
| 칸 (`CellView`, 프리팹) | 자기 데이터(지뢰·인접 수·상태) 보유, 자기 스프라이트 갱신, **자기 사건을 판단해 `GameManager`에 알림** |
| 입력 (`BoardInput`) | 마우스 → 셀 좌표 → `GameManager` 호출 |
| 렌더링 (`BoardTileSet`, `BoardCameraFitter`) | 칸 스프라이트 묶음, 카메라 맞춤 |
| UI | 상단 바·난이도 선택·결과 패널 |

### 2.2 호출 방향
```
Input ──▶ GameManager ──▶ BoardModel ──▶ CellView
              ▲                              │
              └────────── 알림 3개 ───────────┘
              │
              └── 이벤트 ──▶ UI / BoardCameraFitter
```
- 명령은 위에서 아래로: `Input → GameManager → BoardModel → CellView`.
- **알림은 `CellView`에서 `GameManager`로** 되돌아간다 (의도된 순환). 알림은 3개로 고정:
  `NotifySafeRevealed` / `NotifyMineRevealed` / `NotifyFlagChanged`.
- UI·`BoardCameraFitter`는 `GameManager`의 **static 이벤트**를 구독한다. 보드 상태를 직접 바꾸지 않는다.
- **승패 규칙의 단일 출처는 `GameManager`** (집계). 칸 단위 사건 판단의 단일 출처는 `CellView`.

### 2.3 권장 폴더 구조
```
Assets/
├─ Scripts/
│  ├─ Core/      GameManager, GameState (Singleton<T> 복사)
│  ├─ Board/     BoardModel, CellView, CellState, BoardConfigData(SO)
│  ├─ View/      BoardTileSet(SO), BoardCameraFitter
│  ├─ Input/     BoardInput
│  ├─ UI/        카운터·타이머·리셋·난이도·결과 패널
│  └─ Editor/    에디터 전용 도구 (칸 스프라이트 생성 등)
├─ Tests/EditMode/   BoardModel 테스트
├─ Tests/PlayMode/   GameManager 승패 판정 테스트
├─ Resources/Boards/ 난이도별 BoardConfigData 3종
├─ Sprites/          칸 스프라이트 시트
├─ Prefabs/          CellView 프리팹
└─ Scenes/Main.unity
Docs/                프로젝트 루트 (이 문서 위치)
```
- 폴더 트리는 확정이 아니라 **권장안**이다. 확정은 폴더·네이밍 명세(후속)에서 한다.
- **asmdef**: `Scripts` 런타임, `Scripts/Editor` Editor 전용, 테스트(EditMode·PlayMode)는 런타임을 참조. (상세: board-view-spec §7)
- 난이도 수치는 코드에 박지 않고 `BoardConfigData`(ScriptableObject)로 둔다.

---

## 3. 시스템별 핵심 규칙 (계약)

> 후속 세부 명세가 반드시 따를 고정 사항이다. 여기서 정한 이름·규칙을 바꾸려면 이 문서부터 고친다.

### 3.1 보드·칸
**칸(`CellView`)이 가지는 값**
| 필드 | 설명 |
|------|------|
| `IsMine` | 지뢰 여부 |
| `AdjacentMines` | 8방 이웃의 지뢰 수 (0~8) |
| `State` | `Hidden` / `Revealed` / `Flagged` |

**좌표**: `Vector2Int`. Unity `Grid`의 셀 좌표와 동일하며 y-up이다 (뒤집기 변환을 두지 않는다).

**규칙**
1. **지연 배치**: 보드 생성 시점에는 지뢰를 배치하지 않는다. 첫 `Reveal`에서, **클릭한 칸과 그 8방 이웃을 제외한 칸**에만 무작위 배치한다 → 첫 클릭은 항상 0칸 오픈이 된다.
   - **`Hidden` 칸을 눌렀을 때만 배치한다.** 깃발 칸·열린 칸 클릭은 무시하고 배치하지 않는다 (배치 후 무시되면 이후 첫 클릭의 안전이 깨진다).
   - 무작위는 시드를 주입할 수 있어야 한다 (테스트 재현용).
   - 검증: `mines ≤ 가로×세로 − 9`.
2. **0칸 연쇄 오픈**: `AdjacentMines == 0`인 칸을 열면 이웃을 연쇄로 연다. **재귀 금지, Queue 기반 BFS.** 깃발 칸은 열지 않는다.
3. **깃발**: `Hidden ↔ Flagged` 토글. `Revealed` 칸에는 무시. `Flagged` 칸은 오픈 요청을 무시한다. 깃발 수 상한은 없다.
4. **코딩(Chord)**: `Revealed` 숫자 칸에서, 인접 깃발 수 == 숫자일 때만 나머지 `Hidden` 이웃을 연다.
   - 깃발이 틀려 지뢰가 열리면 패배. **지뢰를 열면 즉시 루프를 중단**한다.
   - 인접 깃발 수 ≠ 숫자이면 **아무 일도 일어나지 않는다**.
5. **사건 판단과 알림 (`CellView`)**: 칸은 자기에게 일어난 일을 판단해 `GameManager`에 알린다. 알림은 **자기 갱신(스프라이트 교체)이 끝난 뒤** 보낸다.
   | 사건 | 알림 |
   |------|------|
   | 안전 칸이 열림 | `NotifySafeRevealed(CellView)` |
   | 지뢰 칸이 열림 (스스로 `Exploded` 표시) | `NotifyMineRevealed(CellView)` |
   | 깃발이 꽂히거나 뽑힘 | `NotifyFlagChanged(bool flagged)` |
6. **승리 조건**: 지뢰가 아닌 칸을 모두 연다. `GameManager`가 `NotifySafeRevealed`를 세어 `열린 안전 칸 수 == 가로×세로 − 지뢰 수`이면 승리. (깃발의 정답 여부는 승리에 쓰지 않는다.)
7. **패배 조건**: `NotifyMineRevealed`를 받으면 패배. 종료 후에는 이후 알림을 무시한다 (코딩으로 지뢰를 여러 개 열어도 패배 처리는 한 번).
8. **종료 후 화면 정리**: `GameManager`가 상태 전이 직후 `Board`에 지시하고, `BoardModel`이 수행한다.
   - 승리: 남은 지뢰에 자동 깃발 (`FlagAllMines`, 데이터 변경)
   - 패배: 남은 지뢰 공개·잘못 꽂은 깃발 X 표시 (`ShowAllMines`, **표시만** — 데이터 `State`는 변경하지 않음)
   - 이 처리는 알림 도중(연쇄 오픈 루프 중)에 호출될 수 있으므로 **지뢰·깃발 칸만 건드린다**.

> 변경 통지 목록(`OnCellsChanged`)과 연산 결과 타입(`BoardResult`/`Outcome`)은 두지 않는다. 칸이 스스로 갱신하고 알림으로 전달하기 때문이다.

### 3.2 상태·흐름
```
Ready ──(첫 안전 칸 열림)──▶ Playing ──(안전 칸 전부 열림)──▶ Won
                                └────(지뢰 열림)──▶ Lost
Won / Lost ──(재시작 / 난이도 변경)──▶ Ready
Playing ──(재시작 / 난이도 변경)──▶ Ready
```
| 상태 | 의미 |
|------|------|
| `Ready` | 보드 준비됨, 첫 오픈 전. 타이머 정지. 깃발은 허용 (깃발만으로는 `Playing`으로 전이하지 않음) |
| `Playing` | 첫 안전 칸이 열린 이후. 타이머 동작 |
| `Won` / `Lost` | 종료. 타이머 정지, 보드 입력 잠금 |

**이벤트** (`GameManager`의 static 이벤트. 구독은 `OnEnable`, 해제는 `OnDisable`)
| 이벤트 | 시점 |
|--------|------|
| `OnBoardCreated(BoardModel)` | 새 판 시작 (카메라 맞춤) |
| `OnFlagCountChanged` | 깃발 수 변경 (값은 `MinesRemaining` 폴링) |
| `OnTimeChanged(int)` | 정수 초가 바뀔 때만 |
| `OnStateChanged(GameState)` | 상태 전이 |

**발행 순서**: 한 입력 안에서 `OnStateChanged(Won/Lost)`는 **항상 가장 마지막**이다. 종료 처리는 `상태 대입 → 화면 정리 → (승리 시) OnFlagCountChanged → OnStateChanged` 순서 (상세: game-flow-spec §5).

**규칙**
- **재시작·난이도 변경은 씬 리로드 없이 보드를 재생성**한다 (난이도 변경에 어차피 재생성이 필요하므로 한 경로로 통일).
- **타이머**: `Playing` 동안 `Time.deltaTime`을 누적. UI는 정수 초가 바뀔 때만 갱신. 표시 상한 **999**.
- **지뢰 카운터** = 지뢰 수 − 깃발 수 (**음수 허용**).
- **패배 연출**: 남은 모든 지뢰 공개 · 폭발 칸 강조 · 잘못 꽂은 깃발은 X 표시.
- **승리 연출**: 남은 지뢰에 자동 깃발, 카운터 0.
- **종료 후 입력 잠금**: `Won`/`Lost`에서는 `GameManager`가 보드 입력을 `Board`에 전달하지 않는다. 재시작·난이도 변경만 허용.

### 3.3 입력 (Input System)
| 입력 | 대상 칸 | 동작 |
|------|--------|------|
| 좌클릭 | `Hidden` | 오픈 |
| 좌클릭 | `Revealed` 숫자 | 코딩 |
| 우클릭 | `Hidden`/`Flagged` | 깃발 토글 |
| 가운데 클릭 | `Revealed` 숫자 | 코딩 |

- **좌+우 동시 누름 코딩은 미지원** (판정이 까다로움). 대체: 숫자 칸 좌클릭 / 가운데 클릭.
- 좌표 변환: 마우스 위치 → 월드(`Camera.ScreenToWorldPoint`) → **`Board.TryWorldToCell`** (내부에서 `Grid.WorldToCell` + 범위 검사). Input은 `CellView`를 직접 다루지 않는다.
- **UI 위 클릭 차단**: `EventSystem.current.IsPointerOverGameObject()`가 true면 보드 입력을 무시. EventSystem은 `InputSystemUIInputModule`을 사용 (구 Standalone 모듈 금지).

### 3.4 렌더링 (Grid + CellView)
- 보드 오브젝트에 `Grid`(셀 크기 1×1)를 두고, `CellView` 프리팹 인스턴스를 `Grid.GetCellCenterWorld`로 배치한다. 셀 (0,0)이 좌하단이고 보드 영역은 `[0,W]×[0,H]`.
- `CellView`는 `SpriteRenderer` 하나만 가진다 (콜라이더 없음 — 클릭은 `Grid.WorldToCell`로 판정). 칸 풀링은 `ObjectPool<CellView>`.
- **칸 스프라이트 14종**을 `BoardTileSet`(ScriptableObject)으로 묶는다.

  | 구분 | 스프라이트 |
  |------|-----------|
  | 닫힌 칸 | Hidden, Flag |
  | 열린 칸 | 숫자 0~8 (9종) |
  | 종료 표시 | Mine, ExplodedMine, WrongFlag |

- 스프라이트는 아직 없다. **에디터 스크립트로 절차 생성**(한 장의 시트를 14개로 슬라이스)하는 방안을 쓰되, SO로 분리해 이후 원하는 스프라이트로 교체 가능하게 한다.
- **카메라 자동 맞춤**: 보드 크기와 상단 HUD 비율을 반영해 Orthographic size를 계산하고 보드를 중앙 정렬한다. 정밀 수식과 정수 배율 스냅은 board-view-spec §6.3.
- 난이도 변경 시 `BoardModel.Initialize`가 기존 칸을 풀에 반환하고 새로 꺼내 배치한다.

### 3.5 UI
- **상단 바**: 남은 지뢰 카운터 | 리셋 버튼 | 타이머
- **난이도 선택**: 3개 버튼
- **결과 패널**: 승/패 + 소요 시간 + 다시하기
- **폰트 주의**: TMP 기본 폰트(LiberationSans)에는 **한글이 없다**. 초기에는 숫자·영문 위주로 시작하고, 한글 라벨이 필요하면 한글 폰트 에셋을 추가한다 (이전 프로젝트 `_GameKit/Fonts`의 폰트를 재사용 가능).

---

## 4. 검토 결과 (위험·빈틈)

| # | 위험 | 대응 |
|---|------|------|
| 1 | 첫 클릭에 지뢰가 있으면 즉사 (가장 흔한 버그) | 지연 배치 + 클릭 칸·이웃 제외, 단위 테스트로 고정 |
| 2 | 깃발 칸을 눌러 무시됐는데 지뢰가 배치돼 이후 첫 클릭 안전이 깨짐 | `Hidden` 검사를 지뢰 배치보다 먼저 (3.1 규칙 1) |
| 3 | 재귀 플러드필은 큰 보드에서 스택 위험 | Queue 기반 BFS |
| 4 | UI 클릭이 보드까지 통과 | `IsPointerOverGameObject` + `InputSystemUIInputModule` 확인 |
| 5 | 클릭 매핑·카메라 맞춤을 직접 구현해야 함 | `Board.TryWorldToCell`, `BoardCameraFitter`, 좌표 y-up 통일 |
| 6 | 칸 스프라이트가 없음 | 절차 생성 + `BoardTileSet` SO로 교체 가능하게 |
| 7 | 난이도 변경 시 칸·카메라·UI 잔상, 이벤트 중복 구독 | `Initialize`가 풀 반환 후 재생성, 구독/해제 점검 |
| 8 | 좌+우 동시 클릭 코딩은 구현·판정 난이도가 높음 | 가운데 클릭 / 숫자 칸 좌클릭으로 대체 (3.3 명시) |
| 9 | 한글 폰트 부재 | 3.5 참고, 폰트 에셋 추가 |
| 10 | 깃발을 꽂은 뒤 첫 클릭 시 처리 모호 | 깃발 칸은 오픈 무시. 지뢰 배치의 보호 영역은 깃발과 무관 |
| 11 | 코딩으로 지뢰를 여러 개 열면 `Lost`가 중복 처리됨 | 코딩 루프는 첫 지뢰에서 중단 + `GameManager`가 종료 후 알림을 무시 |
| 12 | 알림 도중(연쇄 오픈 루프 중) 종료 후 화면 정리가 호출돼 데이터가 꼬임 | 정리 메서드는 지뢰·깃발 칸만 변경 |
| 13 | `CellView → GameManager` 순환 의존 | 알림 3개로 고정, `Instance != null` 가드. 부담이 커지면 `CellView`의 static 이벤트로 교체 |
| 14 | 모델이 MonoBehaviour라 규칙 단위 테스트가 이전 설계(순수 C#)보다 무거움 | `Awake` 대신 `Initialize`. 보드 규칙은 EditMode, 승패 판정은 `GameManager`가 필요하므로 PlayMode |
| 15 | `CellView`의 변경 메서드가 public이라 아무나 호출 가능 | "호출자" 규칙을 명세·convention에 명시, 리뷰 시 호출처 검사 |
| 16 | `Assets/Docs.meta`만 있고 폴더가 없음 (고아 메타) | Docs는 프로젝트 루트 `Docs/` 사용. Unity가 경고하면 `.meta` 정리 |

### 열린 질문 (후속 명세에서 확정)
- 타이머를 클래식처럼 첫 클릭 시 **1부터 시작**할지 (현재 안: 0부터).
- 난이도 변경 UI의 위치와 진행 중 변경 시 확인 창 여부.
- 칸 스프라이트를 직접 제공할지, 생성 스크립트 결과물을 그대로 쓸지.

---

## 5. 다음 단계 — 세부 명세 분할 계획

| 순서 | 명세 | 상태 |
|------|------|------|
| 1 | 보드·칸·렌더링 | **작성됨** — [board-view-spec.md](feature-spec/board-view-spec.md) (분할 계획의 보드 모델 + 렌더링을 통합) |
| 2 | 게임 흐름 | **작성됨** — [game-flow-spec.md](feature-spec/game-flow-spec.md) |
| 3 | 입력 | 대기 — 입력 매핑, `TryWorldToCell` 사용, UI 차단, 입력 잠금 |
| 4 | UI | 대기 — 레이아웃, 컴포넌트 목록, 폰트, 결과 패널 구독 구조 |
| 5 | 폴더·네이밍·코드 규약, 빌드 | 대기 — 폴더 확정, asmdef, 네이밍·코드 규칙, Windows 빌드 설정 |

**구현은 명세가 정리된 이후에 시작한다.**

---

## 6. 검증 관점 (각 명세에 포함할 항목)

### EditMode 테스트 (`BoardModel`, 시드 고정, `GameManager` 없이)
1. 첫 클릭 칸과 이웃 8칸에 지뢰가 없다
2. 지뢰 수가 설정값과 정확히 같다
3. `AdjacentMines` 계산이 맞다
4. 0칸 연쇄 오픈 범위가 맞고 깃발 칸은 열리지 않는다
5. 첫 클릭이 깃발 칸이면 지뢰가 배치되지 않고, 이후 첫 클릭이 안전하다
6. 코딩: 정상 / 오깃발로 지뢰 열림 / 깃발 수 불일치 시 무동작 / 지뢰를 열면 즉시 중단
7. `FlagAllMines`는 지뢰에만 깃발을 꽂고, `ShowAllMines`는 `State`를 바꾸지 않는다
8. `Initialize` 반복 호출 시 이전 판의 잔상이 없다
9. 고급 보드를 수천 번 생성해도 예외가 없다

### PlayMode 테스트 / 수동 확인 (`GameManager` 연동)
안전 칸을 모두 열면 `Won` · 지뢰를 열면 `Lost`(코딩으로 여러 개 열어도 한 번) · 깃발 카운터 증감과 음수 · 깃발만 꽂으면 `Ready` 유지 · 이벤트 발행 순서 · 난이도 3종 카메라 맞춤 · 첫 클릭 오프닝 · 패배 연출(오깃발 X 표시) · 승리 자동 깃발 · 종료 후 입력 잠금 · UI 위 클릭 차단 · 난이도 전환 후 잔상 없음 · Windows 빌드 실행.
