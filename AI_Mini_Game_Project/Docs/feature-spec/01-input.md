# 1. 입력 (Player Input)

> 뱀서류 토이프로젝트 기능명세서 — 챕터별 구현 시 Claude에 해당 섹션을 input으로 던진다.

| 키 | 동작 |
|----|------|
| W/A/S/D 또는 방향키 | 이동 (8방향) |
| ESC | 일시정지 토글 |
| Space | 인트로 시작(§11) · 결과 화면 재시작(§12) |
| 1 / 2 / 3 | 레벨업 카드 선택 (마우스 클릭 병행 — §9) |

- 무기는 **자동 발사** (입력 없음)
- **이동·전투에 마우스 사용 안 함** (무기 자동). **UI 선택(레벨업 카드·버튼)은 마우스 클릭 허용** — 키보드와 병행
- **입력 백엔드: 레거시 Input Manager** (`Input.GetAxisRaw("Horizontal"/"Vertical")`). Project Settings ▸ Player ▸ **Active Input Handling = Both**(또는 Old) — 신규 Input System 단독이면 `Input.GetAxis` 런타임 에러.
