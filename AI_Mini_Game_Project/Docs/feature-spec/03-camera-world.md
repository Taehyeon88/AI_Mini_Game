# 3. 카메라 & 월드 (Camera & World)

> 월드 모델: **무한 스크롤 평원** (경계 없음). VS·20 Minutes Till Dawn 방식.
> 챕터 매핑: **4-2**(배경 + 카메라). 스폰은 §8과 연계.

---

## 월드 / 배경
- 플레이 공간은 **경계 없는 무한 평원**. 플레이어 위치 클램프 없음, 보이지 않는 벽 없음.
- **배경 = 타일러블 바닥 스프라이트**(배포 `tile1.png` — `tile2`·`tile3`은 변형/인트로 배경용 선택, 안 써도 무방)를 카메라 따라다니게:
  - 구현(토이 최단): SpriteRenderer **Draw Mode = Tiled** 로 충분히 큰 바닥 + **타일 단위 스냅 추적** — 작은 스크립트가 `LateUpdate`에서 바닥을 카메라 위치로 옮기되 **타일 크기(64px/PPU 64 = 1 unit) 배수로 스냅**(`Mathf.Round(cam.x / t) * t`). 무늬가 월드에 고정돼 보여 이동감 유지.
  - ⚠️ 단순히 **카메라 자식**으로만 두면 무늬가 화면에 붙어 따라와 **이동감이 사라짐** — 금지. (대안: 자식 유지 + 머티리얼 `mainTextureOffset`에 카메라 위치를 넣어 스크롤)
  - 시작 위치: 월드 원점(0,0) 부근. 무한이지만 3분·float 정밀도 문제 없음.
- 데코(나무·돌 등)는 **선택** — 없어도 무방(비스코프 인접).
- **스프라이트 정렬(렌더 순서)**: 바닥 SpriteRenderer `Order in Layer` 낮게(예: **-100**), 캐릭터·투사체·EXP gem은 **0 이상**. (안 잡으면 바닥이 캐릭터를 가릴 수 있음 — 또는 바닥 전용 Sorting Layer `Background`)

## 카메라
- 2D **Orthographic**. **추적은 Cinemachine**으로 (코드 X) — Cinemachine 3.x: 메인 카메라에 `CinemachineBrain`, 별도 `CinemachineCamera`에 `Follow = Player` 연결 (4-2).
- ⚠️ **CM3은 `Follow`만으로 추적 안 됨** — `CinemachineCamera`에 위치 제어(Body) 컴포넌트가 없으면 `Follow`가 무시되고 카메라가 vcam의 transform을 그대로 따라간다(보통 (0,0,0) → **Z=0이 되어 플레이어가 near clip(0.3)에 잘려 화면에 안 보임**). 2D는 **`CinemachinePositionComposer`** 를 붙여 해결(CameraDistance 10 → 카메라 Z = 타깃 −10, 중앙 추적). vcam 자체 Transform Z로는 해결되지 않음.
- **orthographicSize: 9.4** 권장 (세로 약 18.75 units 시야). 스프라이트 스케일에 맞춰 조정. (CM 사용 시 vcam 렌즈의 Orthographic Size로 설정 — 브레인이 메인 카메라에 적용)
- 메인 카메라 **Z = -10** 유지.
- 데드존: 토이는 사실상 없음(중앙 고정 추적) — Cinemachine 기본값 소폭이면 충분.
- **카메라 쉐이크**(플레이어 피격·보스 등장, game-design §7 톤&비주얼): **Cinemachine Impulse**로 구현(4-10 폴리싱). 커스텀 쉐이크 코드 짜지 말 것.

## 해상도 · 화면
- **타깃 해상도: 1080×1920 (9:16, 세로)** — 실제 빌드 기준. Game 뷰도 9:16 고정.
- 종횡비는 **9:16 가정** (다른 비율 대응·레터박스는 비스코프).
- 카메라 Orthographic `size 9.4` → 9:16에서 세로 약 18.75 / 가로 약 10.6 units 시야.
- **UI Canvas Scaler**: `Scale With Screen Size`, Match **0.5** — **캔버스별 Reference Resolution은 §9 UI 참고** (HUD 640×360 / UpgradeUI 1920×1080, 실제 씬 기준).
- **스프라이트 PPU: 64** — 에셋은 **64×64 px**(16×16 원본의 4배 업스케일본, 1080p 시연 기준) → 타일·캐릭터 1개 = **1 unit**. 모든 스프라이트 Import에서 PPU 64 통일. 유닛 기반 수치(사거리·반경·거리)는 이 기준의 시작값 — 플레이테스트로 보정.
- **캐릭터 6종(player·rat·bat·cyclops·ghost·nec)은 2프레임 시트(128×64)**: Sprite Mode = **Multiple**, Grid **64×64**로 슬라이스. **걷기 = 2프레임 토글**(이동 중 0.2~0.3초 간격, 가벼운 공용 컴포넌트 1개), **대기·사망(디졸브) = 프레임[0]**. `EnemyData.sprite`에는 프레임[0]을 넣는다.
- 픽셀아트라 Import 설정: **Filter Mode = Point (no filter)**, **Compression = None** (뭉개짐 방지). Pixel-Perfect Camera는 선택(깔끔하면 적용).
- 창 모드: 개발·녹화는 windowed 1080×1920.

## 스폰과의 관계 (§8 연계)
- 적 스폰 위치: 카메라 **뷰포트 밖** 무작위 가장자리 + 약간의 마진.
- 화면에서 **31 units 이상** 멀어진 적은 풀로 반환(§8).
- 무한 평원이라 "맵 밖" 개념이 없음 → 디스폰 기준은 **오직 카메라 거리**.

## 에셋 체크 ✅
- 바닥: `tile1~3.png`(64×64) 확보됨 → `_GameKit/Sprites`에 포함해 배포. **기본 바닥 = tile1**.
