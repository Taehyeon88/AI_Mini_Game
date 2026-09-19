# 12. 게임 흐름 (Game Flow / State)

```
[Boot] → [Title (단순)] → [Playing] ⇄ [LevelUpPaused]
                              ↓ (HP 0)       ↓ (보스 처치 or 3분 안전망)
                          [GameOver]              [Clear]
                              ↓                       ↓
                          [Title] (Space/버튼)     [Title] (Space/버튼)
```

**클리어 트리거 우선순위**
1. 보스 사망 → 즉시 Clear (2:00~3:00 사이 정상 흐름)
2. 3분 타임아웃 → Clear (보스 못 잡은 예외 케이스, 안전망)
3. 그 전에 플레이어 HP 0 → GameOver

`GameManager`(싱글톤)이 상태 보유.
- 상태 전환 시 이벤트 발행 (`OnStateChanged`) + 보스 등장만 `OnBossSpawned`(예외). 그 외(사망·레벨업)는 각 처리에서 `ChangeState` 직접 호출 (convention §4)
- 타이머·점수는 GameManager가 가짐 (`ElapsedTime`/`KillCount` 프로퍼티로 HUD가 폴링)
- 일시정지는 `Time.timeScale` 0/1
- **`State == Playing`일 때만** 스포너·타이머·무기 Tick·적 이동이 진행 (Title·LevelUpPaused·Clear·GameOver에선 정지). 각 시스템이 `GameManager.State`를 확인하거나 `timeScale=0`로 일괄 정지.
- **재시작**: 결과/게임오버에서 `SceneManager.LoadScene(현재 씬)` = 씬 통째 리로드(사실상 Title부터 새 판). 토이라 Title 상태를 따로 그리지 않고 리로드로 단순화 (§11 인트로 화면이 첫 화면)
