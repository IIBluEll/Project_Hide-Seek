# Project_Hide-Seek

> **PROJECT ISOLATION (가제)**
>
> NHN 공모전 참가를 위해 제작 중인 1인칭 잠입 생존 공포 게임입니다.

[WebGL로 플레이](https://iibluell.github.io/Project_Hide-Seek/) · [게임 기획서](./GAME_DESIGN_DOCUMENT.md)

## 게임 소개

플레이어는 전원이 차단된 폐쇄 시설을 탐색하며 활성화된 발전기를 복구하고 탈출해야 합니다. 시설을 배회하는 Chase AI와 싸울 수는 없으며, 시야 노출과 소음을 관리하고 은신처를 활용해 추적을 피해야 합니다.

| 항목 | 내용 |
| --- | --- |
| 장르 | 1인칭 생존 공포, 잠입, 추격 |
| 플레이 방식 | 싱글 플레이 |
| 목표 플레이 시간 | 1회 약 10~15분 |
| 핵심 목표 | 기본 3개의 활성 발전기 복구 후 탈출 |
| 전투 | 없음 |
| 난이도 | Easy / Normal / Hard |
| 현재 버전 | 0.1.0 |

현재는 MVP 통합 및 안정화 단계입니다. 기획서에는 개발 목표와 미정 사항도 포함되어 있으므로, 아래 내용은 `dev` 브랜치에서 코드와 씬 연결이 확인된 범위를 기준으로 합니다.

## 핵심 플레이

1. 현재 맵의 10개 후보 지점 중 무작위로 활성화된 3개 발전기를 찾습니다.
2. 이동 속도와 자세를 조절해 발생하는 소음을 관리합니다.
3. AI가 접근하면 책상이나 사물함에 숨어 시야를 끊고 수색을 피합니다.
4. 발전기를 수리하면서 무작위로 발생하는 QTE를 수행합니다.
5. 모든 발전기를 복구해 열린 탈출구로 이동합니다.

AI에게 붙잡히면 즉시 게임 오버가 되며, 탈출에 성공하면 엔딩과 결과 화면으로 이어집니다.

## 현재 구현

- **게임 흐름:** 타이틀, 튜토리얼, 난이도 선택, 로딩, 인게임, 사망·탈출 결과 흐름
- **플레이어:** 1인칭 이동과 시점, 달리기 스태미나, 앉기, Raycast 상호작용, 책상·사물함 은신
- **디코이 시스템:** 획득, 투척 강도 충전, 충돌 소음과 관련 튜토리얼 미션
- **증거 기반 AI:** 시야·소음 감지, 마지막 목격·청각 증거, 우선순위 기반 조사와 제한된 수색
- **계층형 AI:** Master AI(Director)의 `GlobalStress` 페이싱과 Chase AI의 `Anger` 기반 공격성 분리
- **상태 머신:** `DORMANT`, `PATROL`, `INVESTIGATE`, `CHASE`, `ATTACK`, `SEARCH`, `RETREAT`
- **공간 탐색:** 현재 맵의 12개 Zone 기반 순찰·수색, NavMesh 이동, 12개 Vent를 이용한 출현과 이탈
- **발전기:** 10개 후보 중 3개 무작위 선택, Zone 분산, 수리 진행도, 중단 후 감소, QTE 성공·실패와 소음 페널티
- **연출과 사운드:** 발전기·AI 효과음, 상황 기반 4단계 BGM, 사망·탈출 컷신, 목표 실루엣 표시
- **UI 구조:** `HM.CodeBase`의 `AView`·`APresenter`를 사용하는 MVP 기반 UI
- **배포:** `docs/` 디렉터리의 GitHub Pages용 WebGL 빌드

현재 메인 인게임 맵에는 디코이 아이템이 배치되어 있지 않아 튜토리얼에서만 관련 기능을 확인할 수 있습니다. 설정·일시 정지 메뉴와 결과 화면의 플레이 시간·완료 발전기 수 표시는 아직 구현되어 있지 않습니다.

## 조작법

키보드와 마우스를 기준으로 합니다.

| 입력 | 동작 |
| --- | --- |
| `WASD` / 방향키 | 이동 |
| 마우스 이동 | 시점 회전 |
| `Left Shift` 유지 | 달리기 |
| `C` | 앉기 / 일어서기 |
| `E` | 상호작용 |
| `E` 유지 | 발전기 수리 |
| `Space` | 발전기 QTE 입력 |
| 마우스 왼쪽 버튼 유지 후 놓기 | 디코이 보유 시 조준·충전 후 투척 |
| 마우스 오른쪽 버튼 | 디코이 조준 취소 |
| `H` | 미완료 발전기 위치 표시(기본 5초, 최대 3회) |

## 개발 환경

| 구분 | 버전 / 구성 |
| --- | --- |
| Unity | `6000.3.20f1` |
| 언어 | C# |
| Render Pipeline | Universal Render Pipeline `17.3.0` |
| Input | Unity Input System `1.19.0` |
| Navigation | AI Navigation `2.0.13` / NavMesh |
| UI | Unity UI `2.0.0` / HM CodeBase MVP |
| 연출 | Timeline `1.8.12` |
| 배포 대상 | WebGL / GitHub Pages |

## 프로젝트 실행

### 준비 사항

- Unity Hub
- Unity Editor `6000.3.20f1`
- Git
- WebGL 빌드가 필요할 경우 WebGL Build Support 모듈

### 실행 순서

```bash
git clone https://github.com/IIBluEll/Project_Hide-Seek.git
cd Project_Hide-Seek
git switch dev
```

1. Unity Hub에서 저장소 루트 폴더를 엽니다.
2. Package Manager가 의존성을 복원할 때까지 기다립니다.
3. `Assets/01_Main/01_Scene/TitleScene.unity`를 엽니다.
4. Play 버튼을 눌러 실행합니다.

Build Profiles에는 다음 씬이 등록되어 있습니다.

```text
TitleScene → LoadingScene → InGame_Map
     └────→ TutorialScene
```

## 프로젝트 구조

```text
Assets/
├─ 01_Main/
│  ├─ 01_Scene/       # 타이틀, 로딩, 인게임 및 AI 검증 씬
│  ├─ 02_Scripts/     # AI, 발전기, 게임 진행, 연동, 로딩, 사운드, UI
│  ├─ 03_Prefabs/     # AI, 발전기, 은신처, 컷신, 사운드 프리팹
│  ├─ 04_Resource/    # UI와 사운드 리소스
│  └─ 06_Data/        # 난이도별 발전기 설정 데이터
└─ 02_Prototype/
   ├─ HG/             # 발전기 연출, 컷신, 탈출, 결과 UI
   └─ JW/             # 플레이어, 상호작용, 튜토리얼

Packages/             # Unity 패키지 의존성
ProjectSettings/      # Unity 프로젝트 및 빌드 설정
docs/                 # 배포된 WebGL 빌드
```

## 문서

- [GAME_DESIGN_DOCUMENT.md](./GAME_DESIGN_DOCUMENT.md): 게임 기획과 MVP 기준
- [Alien_Isolation_AI_Portfolio_Design.md](./Alien_Isolation_AI_Portfolio_Design.md): AI 구조 설계 참고 문서
- [AGENTS.md](./AGENTS.md): 저장소 작업 및 협업 규칙

## 기준 버전

이 README는 2026-08-10 기준 원격 `dev` 최신 커밋 `9b0da759`를 바탕으로 작성되었습니다.