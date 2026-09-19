# 7. 업그레이드 시스템 (Upgrade)

레벨업 시마다 3개 카드 무작위 제시. 카테고리 3종:

**A. 신규 무기 획득** (아직 안 가진 무기만)
- 도끼 획득
- 창 획득 (보조 · 강화 없음)

**B. 무기 강화** (가진 무기만)
- {무기명} 데미지 +20% (단검·도끼)
- {무기명} 쿨다운 -15% (단검만)
- {무기명} 발사체 +1 (단검만)
- *무기 강화 시 단검·도끼는 단계 스프라이트도 진화 (§5 `stageSprites`) — 시각 피드백.*

**C. 스탯 강화** (누적 = base 기준 가산 스택, 무기 강화와 동일 규칙 — §5 누적 공식. level = 해당 스탯 카드 적용 횟수, **시작 0**)
- 최대 HP: `MaxHP = baseMaxHP + 20 × level` (가산, int)
- 이동 속도: `MoveSpeed = baseSpeed × (1 + 0.10 × level)`
- 픽업 반경: `PickupRadius = baseRadius × (1 + 0.30 × level)`
- (HP 강화 시 현재 HP도 +20 동시 회복, in-place 누적 금지 — 매번 base에서 재계산)

**선정 규칙**
- 3장 모두 다른 카테고리/대상 선호
- 다 가진 무기는 신규 카드에서 제외
- 우선순위 없음 (단순 무작위, 토이 범위)

**데이터 형태 (코드↔자산 계약 — convention §4)**
- 각 카드 = `UpgradeData` 자산. `category` + 무기 카드면 `targetWeapon`·`weaponUpgradeType`, 스탯 카드면 `statType`, 강화량은 `amount`.
- 적용은 `UpgradeService.Apply(UpgradeData, PlayerController)` (SO에 동작 안 박음).
- **총 9종**: 신규 2(도끼·창) + 무기 강화 4(단검 데미지·쿨다운·발사체 / 도끼 데미지; 쿨다운·발사체+1은 단검 전용) + 스탯 3(HP·이속·픽업반경). **창은 강화 카드 없음**(획득 후 고정 성능).
