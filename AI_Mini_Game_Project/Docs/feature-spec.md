# 뱀서류 토이프로젝트 — 기능명세서 (목차)

> 시스템별 구체 동작·수치 정의. 게임 기획서의 후속.
> 챕터별 구현 시 Claude에 **해당 섹션 파일**을 input으로 던진다.
> (2026-06-06 섹션별 파일로 분할 → `feature-spec/`)

> **코드 규약·인터페이스 계약·제공 인프라는 별도 문서:** [convention.md](convention.md) (코드 작성 전 필독)

| # | 문서 | 내용 |
|---|------|------|
| 1 | [01-input.md](feature-spec/01-input.md) | 입력 (Player Input) |
| 2 | [02-player.md](feature-spec/02-player.md) | 플레이어 (Player) |
| 3 | [03-camera-world.md](feature-spec/03-camera-world.md) | 카메라 & 월드 (무한 평원 + Cinemachine) |
| 4 | [04-enemy.md](feature-spec/04-enemy.md) | 적 (Enemy) — 잡몹 3종 + 유령 + 보스(네크로맨서) |
| 5 | [05-weapon.md](feature-spec/05-weapon.md) | 무기 (Weapon) — 3종 + 강화 |
| 6 | [06-exp-level.md](feature-spec/06-exp-level.md) | 경험치 & 레벨업 (EXP & Level) |
| 7 | [07-upgrade.md](feature-spec/07-upgrade.md) | 업그레이드 시스템 (Upgrade) |
| 8 | [08-wave-spawner.md](feature-spec/08-wave-spawner.md) | 웨이브 시스템 (Wave / Spawner) |
| 9 | [09-ui.md](feature-spec/09-ui.md) | UI |
| 10 | [10-game-sound.md](feature-spec/10-game-sound.md) | 게임 사운드 (SFX·BGM) |
| 11 | [11-intro-screen.md](feature-spec/11-intro-screen.md) | 인트로 화면 (Title) |
| 12 | [12-game-flow.md](feature-spec/12-game-flow.md) | 게임 흐름 (Game Flow / State) |
| 13 | [13-folder-naming.md](feature-spec/13-folder-naming.md) | 폴더 구조 & 씬 초기 구성 (네이밍은 convention §1) |
| 14 | [14-build.md](feature-spec/14-build.md) | 빌드 (PC/Windows 실행 파일) |
