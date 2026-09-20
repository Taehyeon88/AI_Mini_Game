---
description: 새 잡몹(Enemy) 타입 추가 — Docs/enemy-creation-guide.md 패턴을 그대로 실행(EnemyData 자산 생성 + 웨이브 등록)
argument-hint: [enemy 이름, 예: bat]
---

인자: $ARGUMENTS

## 0단계 — 이름 확정
- 인자가 비어 있으면, 먼저 어떤 적을 추가할지 사용자에게 물어본다. 임의로 이름을 짓지 않는다.
- 이름은 영문 소문자로 정규화해서 이후 단계에 쓴다(04-enemy.md의 "코드·자산명은 영문 PNG 이름을 사용" 규칙과 동일 — 스프라이트 파일명·SO 자산명과 맞추기 위함).

## 1단계 — PNG 존재 확인 (필수 게이트, 다른 어떤 단계보다 먼저)
- `Assets/_GameKit/Sprites/{이름}.png` 파일이 있는지 확인한다.
- **없으면 여기서 멈춘다.** "{이름}.png가 없다"고 알리고, 어떻게 할지 사용자에게 물어본다(예: 다른 이름/철자로 다시 시도, 다른 경로의 이미지를 지정, 이미지 준비 후 나중에 재실행 등). 답 없이 임의로 진행하거나 이미지를 대신 만들어내지 않는다.
- 있으면 2단계로 진행.

## 2단계 이후 — `Docs/enemy-creation-guide.md` 그대로 실행
그 문서를 먼저 읽고, 문서의 1~6단계를 이 적 이름으로 그대로 수행한다. 요약하면:

1. **스펙값 확인**: `Docs/feature-spec/04-enemy.md`(필요시 `game-design.md`)에서 이 이름에 해당하는 `maxHP`·`moveSpeed`·`attackPower`·`dropExp`·등장시점을 찾는다. **표에 없는 이름이면 값을 추측하지 말고 사용자에게 직접 물어본다.**
2. **스프라이트 슬라이스 확인**: `.meta` 파일에서 `spriteMode: 2`(Multiple)이고 `{이름}_0`/`{이름}_1` 서브스프라이트로 슬라이스돼 있는지 확인. 안 돼 있으면 에디터에게 안내하고 멈춘다(Claude가 대신 슬라이싱하지 않음).
3. **`{Name}Data.asset` 생성**: UnityMCP `manage_scriptable_object`로 `Assets/Resources/Enemies/{Name}Data.asset` 생성 + 필드 패치. 가이드 3-2/3-3에 적힌 정확한 patch 문법과 함정(ArraySize 직접 patch 불가, `ref`는 patch 아이템의 형제 키, 서브스프라이트 2개 이상이면 `guid`+`spriteName` 필요)을 그대로 따른다.
4. **WaveStage 등록**: `Docs/feature-spec/08-wave-spawner.md` 표를 보고 기존 구간에 섞을지 새 구간을 만들지 판단해, UnityMCP `manage_components`로 `EnemySpawner`의 `_stages`(또는 해당 stage의 `_enemyPool`)에 등록. 가이드 4번의 "구조체 배열은 개별 필드 경로로, 참조 배열은 통째로" 차이를 그대로 따른다. `Managers` GameObject의 instanceID는 매번 `find_gameobjects`로 새로 조회(하드코딩 금지).
5. **검증**: `read_console`로 에러/경고 0 확인 → Play 모드에서 `execute_code`로 `GameManager.Instance.ChangeState(GameState.Playing)` 후 새 타입이 스폰되는지 확인. 헤드리스 테스트 중 Update 틱이 늦게 도는 현상은 버그가 아님(가이드 5번 참고).
6. **예외 처리**: 가이드 "6. 이 패턴으로 안 커버되는 것"(Ghost 같은 소환물, 콜라이더 크기 변경이 필요한 타입, EXP 드랍/사망 이펙트·피격 플래시 같은 보류 항목)에 해당하면 조용히 진행하지 말고 먼저 사용자에게 알린다.

## 진행 원칙
- 각 단계 시작 전 무엇을 할지 한 줄로 먼저 말한다.
- 스펙에 없는 값을 추측해서 채우지 않는다 — 모르면 물어본다.
- 이미 완료된 인프라(`Assets/Prefabs/Enemy.prefab`, 레이어, Physics2D 충돌 매트릭스)는 재사용만 하고 다시 만들지 않는다.
