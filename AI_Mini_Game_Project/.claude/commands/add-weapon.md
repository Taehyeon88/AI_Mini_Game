---
description: 새 무기(Weapon) 타입 추가 — Docs/weapon-creation-guide.md 패턴을 그대로 실행(WeaponData 자산 생성 + IWeapon 구현 + PlayerWeapons 등록)
argument-hint: [무기 이름, 예: axe]
---

인자: $ARGUMENTS

## 0단계 — 이름 확정
- 인자가 비어 있으면, 먼저 어떤 무기를 추가할지 사용자에게 물어본다. 임의로 이름을 짓지 않는다.
- 이름은 `Assets/Scripts/Weapon/WeaponKind.cs`에 이미 존재하는 값(`Axe`/`Spear`)이어야 한다. 다른 이름이면 `05-weapon.md` 스펙에 없다고 알리고 진행 여부를 먼저 확인한다.

## 1단계 — PNG 존재 확인 (필수 게이트, 다른 어떤 단계보다 먼저)
- `Docs/feature-spec/05-weapon.md` 표 기준 에셋 파일명을 확인한다 — 도끼는 `Assets/_GameKit/Sprites/axe1~3.png`, 창은 `Assets/_GameKit/Sprites/spear.png`.
- **없으면 여기서 멈춘다.** "{파일}이 없다"고 알리고 어떻게 할지 사용자에게 물어본다. 답 없이 임의로 진행하거나 이미지를 대신 만들어내지 않는다.
- 있으면 2단계로 진행.

## 2단계 이후 — `Docs/weapon-creation-guide.md` 그대로 실행
그 문서를 먼저 읽고, 문서의 1~6절을 이 무기 이름으로 그대로 수행한다. 요약하면:

1. **스펙값 확인**: `Docs/feature-spec/05-weapon.md`에서 데미지·쿨다운·사거리·투사체 속도(있다면)를 찾는다. **표에 없는 구체 수치(도끼 `orbitAngularSpeed`, 창 `homingTurnRate` 등)는 추측하지 말고 사용자에게 직접 물어본다.**
2. **스프라이트 확인**: 1단계에서 이미 확인한 PNG가 강화 단계별(도끼 `axe1~3`, 단검과 동일 패턴)인지 단일(창 `spear`)인지 표에서 재확인.
3. **`{Name}Data.asset` 생성**: UnityMCP `manage_scriptable_object`로 `Assets/Resources/Weapons/{Name}Data.asset` 생성(새 SO 클래스 아님, 기존 `WeaponData` 재사용) + 필드 패치. 가이드 3-2/3-3에 적힌 정확한 patch 문법과 함정(ArraySize 직접 patch 불가, `ref`는 patch 아이템의 형제 키, 서브스프라이트 2개 이상이면 `guid`+`spriteName` 필요, 프리팹 참조는 `guid`만)을 그대로 따른다. 이 무기와 무관한 필드는 0으로 둔다.
4. **코드 작성**: `{Name}.cs`(plain C#, `IWeapon` 구현, MonoBehaviour 아님)를 작성하고 `PlayerWeapons.CreateWeapon` switch에 case를 추가한다. **도끼(궤도·접촉데미지·Damage 강화만)와 창(유도 투사체·강화 없음)은 단검(직선 발사)과 동작 자체가 다르다** — 가이드 4-1/4-2를 그대로 따른다. `PlayerController.Awake()`에 도끼/창을 Knife처럼 즉시 `AddWeapon`으로 부여하지 않는다(스펙상 2·3번 무기는 레벨업 3택 획득 — 가이드 4-3).
5. **프리팹 생성**(투사체가 있는 무기만 — 창): `KnifeProjectile.prefab` 만들 때 쓴 절차(가이드 5절: 빈 GameObject 생성 후 `m_Sprite` 경로로 스프라이트 지정, `manage_prefabs create_from_gameobject`의 `target`은 이름 문자열)를 그대로 따른다. 도끼는 투사체가 없으므로 해당 없음 — 회전체를 프리팹으로 만들지 씬 배치로 할지 진행 전 사용자에게 확인한다.
6. **검증**: `read_console`로 에러/경고 0 확인 → Play 모드에서 데미지·쿨다운(재타격 간격)·강화 스택 반영을 확인한다. 헤드리스 테스트 중 Play 모드 프레임이 자동으로 안 흐르는 현상은 버그가 아니다(가이드 6절 참고) — `execute_code`로 리플렉션 직접 호출해 검증한다.
7. **예외 처리**: 가이드 "7. 이 패턴으로 안 커버되는 것"(직선/궤도/유도 3가지를 벗어나는 새 발사 패턴, `WeaponData`에 없는 새 필드가 필요해 보이는 경우, 업그레이드 3택 노출 로직)에 해당하면 조용히 진행하지 말고 먼저 사용자에게 알린다.

## 진행 원칙
- 각 단계 시작 전 무엇을 할지 한 줄로 먼저 말한다.
- 스펙에 없는 값을 추측해서 채우지 않는다 — 모르면 물어본다.
- `WeaponData`의 필드 스키마(19개, convention §4에 고정)는 절대 추가·변경하지 않는다.
- `Knife.cs`/`KnifeProjectile.cs`/`PlayerWeapons.cs` 기존 파일은 새 case 추가 외에는 건드리지 않는다.
- 이미 완료된 인프라(레이어, PlayerProjectile 충돌 매트릭스, ObjectPool 인프라)는 재사용만 하고 다시 만들지 않는다.
