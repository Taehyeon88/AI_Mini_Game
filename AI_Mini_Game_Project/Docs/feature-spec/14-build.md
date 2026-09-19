# 14. 빌드 (Build / Delivery)

> 완성된 게임을 **PC(Windows) 실행 파일**로 내보내기. 게임플레이가 아니라 배포 단계 — 챕터 **4-11**.
> 대부분 에디터 작업(분업: 본인 클릭). Claude는 순서 안내·빌드 스크립트 작성·빌드 에러 해결.

## 타깃
- 플랫폼: PC **Windows (StandaloneWindows64)**. (Mac/Linux·모바일·웹은 비스코프)
- 해상도: **1080×1920 (9:16)** — §3 그대로. Player Settings 기본 해상도.

## Build Settings (에디터 — 본인)
- `File ▸ Build Settings ▸ Platform = Windows`.
- **Scenes In Build에 `Main.unity` 등록** (누락 시 빈 화면이 빌드됨 — 가장 흔한 실수).
- 출력 폴더: `Build/` (저장소엔 미포함).

## Player Settings (체크)
- Product Name · Company Name · 아이콘.
- 기본 해상도 1080×1920, **Windowed**(시연·녹화 기준).
- (선택) 시작 시 Resolution Dialog 비활성.

## 빌드 에러 처리
- 빌드 에러는 **Editor.log**(플레이 중 콘솔과 별개)에 찍힘. **로그를 Claude에 던져** 원인·수정안.
- 흔한 원인: **Windows Build Support 모듈 미설치**, 씬 미등록, 누락 레퍼런스, 출력 경로 권한.

## 비스코프
- **빌드 자동화 스크립트(`BuildPipeline`)·CI** — 한 번 빌드라 불필요(반복 빌드가 필요해지면 그때). "반복되는 것만 자동화" 원칙(4-4·콘솔 에러 판단과 동일).
- 코드 사이닝, 인스톨러, Steam/스토어 패키징, 자동 업데이트 (토이 범위 밖).
