---
description: Docs/convention.md 기준으로 코드 위반 점검 (검토만, 자동 수정 X)
argument-hint: [파일/폴더 경로 (생략 시 현재 변경분 git diff 대상)]
---

`Docs/convention.md`를 먼저 읽고, 그 안의 규칙에 대해 아래 대상 코드를 점검한다.

대상: $ARGUMENTS
(인자가 없으면 `git diff`와 `git status`로 현재 변경/미커밋 파일들을 대상으로 한다. 그것도 없으면 어떤 파일을 볼지 먼저 물어본다.)

이 커맨드는 **검토 전용**이다. 코드를 고치지 않는다 — 위반 목록만 보고한다.

점검 항목 (convention.md 기준, 발견 위치는 file:line으로):

1. **§1 프로젝트 규약**
   - `public` 필드 노출 (프로퍼티 없이) — `[SerializeField] private`만 허용
   - 매직 넘버 (게임플레이 수치가 SO 데이터가 아닌 코드에 하드코딩)
   - `Update()` 안에서 `GetComponent`/`FindObjectOfType` 호출
   - 새 적/무기/픽업이 기존 패턴을 따르지 않고 다른 구조로 짜여 있음 (다르다면 왜 다른지 짚기)
   - 매니저가 `Singleton<T>`를 상속하지 않고 직접 싱글톤 패턴을 구현
   - 커스텀 오브젝트 풀 구현 (풀링은 `UnityEngine.Pool.ObjectPool<T>`만 허용)

2. **§1 기본 컨벤션**
   - 한 파일에 public 타입 2개 이상, 또는 파일명 ≠ 타입명
   - 접근 제한자 생략(암묵적 private 아님, 명시 누락)
   - 미사용 `using`
   - 한 메서드/한 줄이 여러 책임을 겸함
   - 컴포넌트 캐시를 `TryGetComponent` + null 가드 없이 사용 (또는 `GetComponent` 결과 null 체크 누락)
   - `OnEnable`/`OnDisable` 짝 없이 이벤트 구독/해제
   - 태그·레이어 문자열 하드코딩
   - 시간 의존 로직에서 `Time.deltaTime` 누락
   - 마무리 단계인데 `Debug.Log`가 정리 안 되고 남아 있음
   - 자명한 코드에 불필요한 주석 (또는 반대로 '왜'가 필요한 곳에 주석 없음)

3. **§1 네이밍**
   - MonoBehaviour/ScriptableObject(`~Data`)/인터페이스(`I` 접두사) 네이밍 규칙 위반

4. **§2 폴더 위치**
   - 인터페이스가 `Core/Interfaces/` 밖에 있음, SO 데이터가 `Resources/{Enemies,Weapons,Upgrades}/` 밖에 있음 등
   - SO 카탈로그 로드를 인스펙터 드래그나 Addressables로 함 (`Resources.LoadAll<T>` 방식이어야 함)

5. **§3 제공 인프라 (수정 금지 · 상속·사용만)**
   - `Singleton.cs` 등 제공 인프라 코드 자체를 수정함 (상속·사용만 허용, 원본 수정 금지)
   - `Singleton<T>` 파생 클래스가 `Awake`를 오버라이드하면서 `base.Awake()`를 먼저 호출하지 않음
   - 투사체/EXP gem/잡몹을 `ObjectPool.Get`/`Release` 없이 직접 `Instantiate`/`Destroy`함

6. **§4 인터페이스 계약 / 시그니처 고정**
   - `IDamageable`, `IWeapon`, `IPickup` 시그니처가 문서와 다름
   - 창(Spear)이 강화 없는 보조 무기 규칙을 어기고 `ApplyUpgrade`를 호출받음
   - 열거형(`GameState`, `WeaponUpgradeType`, `WeaponKind`, `StatKind`, `UpgradeCategory`) 값이 문서와 다름
   - `GameManager`/`PlayerController` 공개 표면(프로퍼티·메서드·이벤트 이름과 시그니처)이 문서와 다름
   - ScriptableObject 필드 이름이 문서(`EnemyData`/`WeaponData`/`UpgradeData`/`BossData`)와 다름

7. **§6 물리 레이어 & 충돌 / §7 투사체 타깃 규칙** (코드만 대상 — 충돌 매트릭스 세팅·씬 배치는 에디터 작업이라 이 커맨드의 범위 밖)
   - 레이어 이름 문자열을 코드에 하드코딩
   - 투사체가 잘못된 진영(예: PlayerProjectile이 Player를 타격)을 때리는 로직

보고 형식:
- 위반이 없으면 "위반 없음"이라고 짧게 말한다.
- 위반이 있으면 위반마다 `파일:줄번호` — 어떤 규칙(§ 번호)을 어떻게 어겼는지 1~2줄로 설명한다. 심각도나 수정 우선순위를 임의로 매기지 말고 목록만 제시한다.
- 시그니처를 일부러 바꿔야 할 것 같은 경우가 보이면(§5 "조용히 바꾸지 말고 차이를 먼저 짚기" 원칙), 그것도 위반 목록과 분리해서 "설계 논의가 필요한 지점"으로 짚어준다.
- 수정은 하지 않는다. 사용자가 별도로 고쳐달라고 하면 그때 진행한다.
