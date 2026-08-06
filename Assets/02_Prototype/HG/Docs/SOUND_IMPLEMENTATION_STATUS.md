# 사운드 구현 현황 및 작업 인수인계

작성일: 2026-08-06

대상 Unity 버전: `6000.3.20f1`

대상 브랜치: `SoundFeature`

작성 기준 커밋: `99f6cfa` (`Master AI 이벤트 기반 BGM 전환 구조로 개선`)

Pull Request: [#15 사운드 시스템 및 Master AI 기반 BGM 전환 추가](https://github.com/IIBluEll/Project_Hide-Seek/pull/15)

## 1. 문서 목적

이 문서는 현재 사운드 구현 범위, 실제 씬 연결 상태, 기획서와의 차이, 확인된 문제와 다음 작업 순서를 기록한다.

다른 컴퓨터에서 작업을 재개할 때는 이 문서와 `GAME_DESIGN_DOCUMENT.md`의 14장을 먼저 확인한다. 이 문서의 내용은 `SoundFeature` 브랜치 기준이며, PR이 병합된 뒤에는 `dev`의 최신 상태와 다시 비교해야 한다.

## 2. 현재 결론

현재 사운드는 다음 단계까지 진행되었다.

- BGM 재생기와 상태 전환 로직 구현
- Master AI 이벤트 기반 BGM 상태 갱신 구현
- 발전기 루프 및 이벤트 효과음 구현
- `Master / BGM / SFX` AudioMixer 그룹 구성
- BGM 및 발전기 프리팹에 사용할 오디오 클립 연결

아직 실제 게임 빌드 씬에는 사운드 시스템이 연결되지 않았다. 현재 상태는 **구조와 프리팹 구현 완료, 게임 씬 통합 및 전체 효과음 구현 전**으로 판단한다.

## 3. Git 및 PR 상태

문서 작성 시점의 상태는 다음과 같다.

| 항목 | 상태 |
| --- | --- |
| 로컬 브랜치 | `SoundFeature` |
| 원격 브랜치 | `origin/SoundFeature`와 동기화 |
| 작업 트리 | 변경 사항 없음 |
| PR | `SoundFeature` → `dev`, #15 열림 |
| 병합 가능 여부 | `MERGEABLE` |
| CI 검사 | 등록된 검사 없음 |
| `origin/dev...HEAD` 변경량 | 235개 파일, 5,422줄 추가, 1,603줄 삭제 |

사운드 관련 주요 커밋은 다음과 같다.

| 커밋 | 내용 |
| --- | --- |
| `7f6e640` | 발전기 사운드, AudioMixer, 오디오 에셋 도입 |
| `95aefea` | AI 상태에 따른 BGM 전환과 BGM Player 추가 |
| `fa9218c` | ENDING 상태와 게임 진행도 연동 추가 |
| `99f6cfa` | Master AI 공용 이벤트 기반 구조로 변경 |

## 4. 구현된 구조

### 4.1 BGM 재생

주요 파일:

- `Assets/01_Main/02_Scripts/Sound/BgmPlayer.cs`
- `Assets/01_Main/03_Prefabs/Sound/BGM Player.prefab`

`BgmPlayer`는 AudioSource 두 개를 교대로 사용하여 곡을 교차 페이드한다.

- 기본 페이드 시간: 2초
- 기본 페이드 방식: Equal Power
- BGM AudioSource: 2D
- AudioMixer 출력 그룹: `BGM`
- 실행 중 새로운 곡이 요청되거나 이전 곡으로 되돌아가는 경우 처리
- `AudioClip`에 `null`을 전달하면 현재 BGM만 페이드 아웃

### 4.2 BGM 상태 결정

주요 파일:

- `Assets/01_Main/02_Scripts/Integration/BgmStateConnector.cs`
- `Assets/01_Main/02_Scripts/AI/MasterAI/MasterAIProvider.cs`
- `Assets/01_Main/02_Scripts/Gameplay/GameProgressProvider.cs`

`BgmStateConnector`는 Chase AI를 직접 참조하지 않고 `MasterAIProvider`를 AI 공용 접근점으로 사용한다.

구독하는 이벤트:

- `MasterAIProvider.ChaseStateChanged`
- `MasterAIProvider.PlayerZoneChanged`
- `GameProgressProvider.AllGeneratorsCompleted`
- `GameProgressProvider.CompletedGeneratorCountChanged`

상태 변경 없이 AI가 Zone 경계를 통과하는 경우를 보완하기 위해 0.25초마다 AI 위치를 다시 판정한다.

현재 상태 판정은 다음과 같다.

| Master AI/Chase AI 조건 | BGM 상태 |
| --- | --- |
| `CHASE`, `ATTACK` | `CHASE` |
| `DORMANT`, `RETREAT` | `AMBIENT` |
| `PATROL`, `INVESTIGATE`, `SEARCH`이면서 AI가 플레이어와 같은 Zone | `TENSION` |
| 위 상태에서 AI가 다른 Zone | `AMBIENT` |
| 모든 발전기 완료 | `ENDING` 고정 |

전환 규칙:

- 긴장 상승은 단계를 건너뛸 수 있다. `AMBIENT → CHASE`가 가능하다.
- 긴장 하강은 한 단계씩 처리한다.
- `CHASE → TENSION → AMBIENT` 순서로 복귀한다.
- `TENSION`은 최소 6초 유지한다.
- `ENDING` 진입 후에는 AI 상태가 변해도 유지한다.
- 같은 씬에서 진행도가 0으로 초기화되면 ENDING 고정을 해제하고 현재 AI 상태를 다시 반영한다.

프리팹의 `MasterAIProvider`와 `GameProgressProvider` 참조는 비어 있다. 프리팹이 씬 오브젝트를 직접 참조할 수 없기 때문에 `Start()`에서 `FindFirstObjectByType`으로 찾는 구조다. 실제 씬에는 각 Provider가 하나씩 존재해야 한다.

### 4.3 현재 BGM 클립 연결

| 상태 | 연결된 클립 | 상태 |
| --- | --- | --- |
| `AMBIENT` | `Horror Elements/Ambient/Amb_Rumble.wav` | 연결됨 |
| `TENSION` | `Horror Elements/Ambient/Amb_Burn.wav` | 연결됨 |
| `CHASE` | `Horror Elements/Ambient/Amb_Run_2.wav` | 연결됨 |
| `ENDING` | 없음 | 미연결 |

`ENDING` 클립이 비어 있으므로 현재 모든 발전기가 완료되면 ENDING 상태에는 진입하지만 기존 BGM이 페이드 아웃된 뒤 무음이 된다.

### 4.4 발전기 효과음

주요 파일:

- `Assets/01_Main/02_Scripts/Generator/GeneratorSound.cs`
- `Assets/01_Main/03_Prefabs/Generator/Generator.prefab`

`GeneratorSound`는 `Generator`가 사운드 시스템을 직접 알지 않도록 발전기 이벤트를 구독하는 어댑터다.

| 발전기 이벤트/상태 | 동작 | 연결된 클립 |
| --- | --- | --- |
| 비활성 상태 | 기계 루프 | `Small Machine.ogg` |
| 수리 시작 | 수리 루프 | `Machine Bits 2.ogg` |
| 수리 중단 | 상태에 맞는 루프로 복귀 | 비활성 또는 완료 루프 |
| 완료 상태 | 완료 후 기계 루프 | `Machine Bits 4.ogg` |
| QTE 성공 | 단발음 | `Hit_Anvil.wav` |
| QTE 실패 | 단발음 | `Hit_Metal.wav` |
| 발전기 완료 | 단발음 | `Misc_breath.wav` |

발전기 루프와 단발음은 AudioMixer의 `SFX` 그룹으로 출력된다. 최대 감쇠 거리는 35m다.

### 4.5 AudioMixer

주요 파일:

- `Assets/01_Main/04_Resource/Sound/AudioMixer.mixer`

구성된 그룹과 노출 파라미터:

| 그룹 | 노출 파라미터 |
| --- | --- |
| `Master` | `MasterVolume` |
| `BGM` | `BgmVolume` |
| `SFX` | `SfxVolume` |

현재 파라미터를 설정 UI 또는 저장 데이터와 연결하는 코드는 없다.

## 5. 실제 씬 연결 상태

빌드 설정에 등록된 씬은 다음 하나다.

- `Assets/01_Main/01_Scene/SampleScene.unity`

확인 결과:

- `BGM Player.prefab`은 현재 어떤 씬이나 다른 프리팹에도 배치되어 있지 않다.
- `Generator.prefab`을 포함하는 `GeneratorTest.prefab`은 `Assets/02_Prototype/JW/Scene/PlayerTestScene.unity`에서만 사용된다.
- `PlayerTestScene`은 빌드 설정에 등록되어 있지 않다.
- `SampleScene`에는 BGM 또는 발전기 AudioSource가 없다.

따라서 현재 빌드를 실행해도 이번 브랜치에서 구현한 BGM과 발전기 효과음은 재생되지 않는다.

## 6. GDD와 현재 구현의 차이

기획 기준은 `GAME_DESIGN_DOCUMENT.md` 14.1과 14.2다.

| GDD 요구사항 | 현재 구현 | 후속 조치 |
| --- | --- | --- |
| `SAFE` 상태 | 상태 자체가 없음 | SAFE를 복구할지 현 4단계를 공식화할지 결정 |
| TENSION: 같은 Zone 또는 인접 Zone | 같은 Zone만 판정 | 인접 Zone 포함 여부 확정 |
| TENSION: 소음을 조사 중 | 조사 상태만으로는 켜지지 않음 | 위치 정보 노출 문제를 포함해 기획 확인 |
| AMBIENT: 긴장도가 낮음 | `GlobalStress` 미사용 | 사용할 값과 임계값 결정 |
| ENDING: 발전기 완료 및 탈출 장치 활성화 | 발전기 완료 즉시 진입 | 탈출 장치 구현 후 이벤트 이동 여부 결정 |
| 페이드 또는 레이어 전환 | 교차 페이드 구현 | 완료 |
| 추격 종료 후 TENSION 경유 | 구현 | 완료 |

코드 주석에는 2026-08-03 기획 확인을 거쳐 SAFE 제거와 ENDING 조건 변경을 결정했다고 기록되어 있다. 그러나 GDD가 갱신되지 않았으므로 병합 전 기획 기준을 하나로 통일해야 한다.

## 7. GDD 필수 효과음 진행도

| 영역 | 요구사항 | 진행 상태 |
| --- | --- | --- |
| 플레이어 | 걷기, 달리기, 앉아서 이동 | 미구현 |
| 플레이어 | 디코이 투척 | 미구현 |
| 플레이어 | 상호작용 | 미구현 |
| Chase AI | 발소리와 울음소리 | 미구현 |
| Chase AI | 수색과 공격 | 미구현 |
| Chase AI | Vent 출입 | 미구현 |
| 발전기 | 비활성 기계음과 수리음 | 구현 |
| 발전기 | QTE 성공·실패 | 구현 |
| 발전기 | 완료음 | 구현 |
| 발전기 | 전기 스파크 | 별도 클립·연출 미구현 |
| 환경 | 사이렌, 전기음, 환풍기 | 미구현 |
| 환경 | 금속 충격, 문, 조명 점멸 | 미구현 |
| 컷신 | 시작, 사망, 엔딩 사운드 연출 | 미구현 |

메인 코드에서 실제 AudioSource를 사용하는 사운드 시스템은 현재 `BgmPlayer`와 `GeneratorSound`뿐이다. 프로토타입의 `ImpactNoiseEmitter`는 별도 테스트 코드다.

## 8. 확인된 문제와 위험 요소

### P0: 씬 미배치

BGM Player가 씬에 없으므로 BGM 전환 로직이 실행되지 않는다. 게임용 씬이 확정되면 BGM Player를 배치하고 Provider 자동 검색 결과를 확인해야 한다.

### P0: ENDING 클립 누락

`BGM Player.prefab`의 `_endingClip`이 비어 있다. ENDING 상태 전환 시 `Play(null)`이 호출되어 BGM이 정지한다.

### P1: 발전기 단발 효과음의 3D 설정 불일치

`GeneratorSound.cs` 주석은 두 AudioSource 모두 Spatial Blend 1을 요구한다. 그러나 프리팹 직렬화 값은 다음과 같다.

- LoopSource: Spatial Blend 1
- EventSource: Spatial Blend 0

현재 설정에서는 QTE와 완료 단발음이 2D로 재생되어 맵 전체에서 같은 크기로 들릴 수 있다.

### P1: GDD와 상태 정의 불일치

SAFE, 인접 Zone, 소음 조사, GlobalStress, 탈출 장치 조건을 기획과 다시 맞춰야 한다. 코드를 먼저 변경하기보다 GDD 갱신 여부를 먼저 결정한다.

### P1: 필수 효과음 대부분 미구현

플레이어, Chase AI, 환경 효과음과 컷신 사운드가 없다. 기능별 담당 코드가 제공하는 이벤트를 확인한 뒤 `Integration` 계층 또는 전용 사운드 컴포넌트에서 연결한다.

### P2: BGM 임포트 설정

`BgmPlayer.cs`는 긴 곡의 Load Type을 Streaming으로 둘 것을 권장한다. 현재 AMBIENT, TENSION, CHASE WAV 클립은 `Decompress On Load`와 `Preload Audio Data`를 사용한다. 메모리 사용량과 전환 시 끊김을 Unity Profiler로 확인하고 Streaming 전환을 검토한다.

### P2: 볼륨 설정 경로 없음

AudioMixer 파라미터는 노출되어 있지만 이를 조작하거나 저장하는 공용 사운드 설정 구조가 없다.

### P2: 에셋 및 PR 정리 필요

- 오디오 파일 107개, 약 261MB가 추가되었다.
- 실제 연결된 클립은 BGM 3개와 발전기 6개다.
- 사용하지 않는 WAV/OGG 중복과 미사용 클립을 정리할지 검토한다.
- `Assets/TutorialInfo`와 `Assets/Readme.asset` 삭제가 이번 PR에 포함되어 있으므로 의도한 정리인지 확인한다.
- 에셋 `.meta`에는 Unity Asset Store의 productId와 packageName이 남아 있지만 GDD 16.3에서 요구하는 출처 URL, 제작자, 라이선스, 사용 날짜를 정리한 별도 목록은 없다.

## 9. 검증 상태

| 검증 항목 | 결과 |
| --- | --- |
| Git 작업 트리 | 깨끗함 |
| 원격 브랜치 동기화 | 완료 |
| PR 병합 가능 여부 | 가능 |
| BGM/발전기 직렬화 참조 확인 | 완료 |
| 빌드 씬 배치 확인 | 미배치 확인 |
| 사운드 자동화 테스트 | 없음 |
| PR CI | 없음 |
| Unity Play Mode 청취 테스트 | 미완료 |
| 전체 Unity 빌드 | 미완료 |

독립 `dotnet build Assembly-CSharp.csproj`는 Unity가 생성한 프로젝트에서 `UnityEngine.UI`와 `UnityEditor.UI` 참조를 찾지 못해 실패했다. 이는 사운드 코드 자체의 컴파일 오류를 증명하지 않으며 Unity Editor 컴파일을 대체할 수 없다.

Unity 배치 실행은 라이선스 클라이언트 재연결 문제로 전체 검증을 완료하지 못했다. 다음 작업자는 Unity Editor에서 Console 오류, Play Mode 전환, 실제 청취를 반드시 확인해야 한다.

## 10. 권장 작업 순서

1. 기획 담당자와 GDD/현재 BGM 상태 차이를 확정한다.
2. 사용할 게임 씬에 `BGM Player.prefab`을 배치한다.
3. 씬에 `MasterAIProvider`와 `GameProgressProvider`가 각각 하나씩 존재하는지 확인한다.
4. ENDING BGM 클립을 선정하고 프리팹에 할당한다.
5. AMBIENT, TENSION, CHASE, ENDING 전환을 Play Mode에서 순서대로 검증한다.
6. 발전기 EventSource의 Spatial Blend를 3D로 수정하고 거리 감쇠를 청취 검증한다.
7. 플레이어, Chase AI, 환경 순서로 필수 효과음을 구현한다.
8. AudioMixer 볼륨을 설정 UI 및 저장 데이터에 연결한다.
9. BGM Streaming 설정과 오디오 에셋 용량을 최적화한다.
10. 에셋 라이선스 목록과 최소 Play Mode 테스트를 추가한다.

## 11. 다른 컴퓨터에서 작업 재개

저장소를 받은 뒤 다음 순서로 확인한다.

```powershell
git fetch origin
git switch SoundFeature
git pull --ff-only origin SoundFeature
git status --short --branch
git log -5 --oneline
gh pr view 15
```

그다음 Unity Hub에서 프로젝트를 Unity `6000.3.20f1`로 연다.

작업 전 확인 문서:

1. `GAME_DESIGN_DOCUMENT.md` 14장과 16.3
2. 이 문서
3. `Assets/01_Main/02_Scripts/Integration/BgmStateConnector.cs`
4. `Assets/01_Main/02_Scripts/Sound/BgmPlayer.cs`
5. `Assets/01_Main/02_Scripts/Generator/GeneratorSound.cs`

씬 배치 여부 확인 명령:

```powershell
# BGM Player 프리팹 GUID가 씬/프리팹에서 사용되는지 확인
rg -n -F "48733292fc74a6a4cbcd7b26e02428af" Assets -g "*.unity" -g "*.prefab"

# GeneratorTest 프리팹 GUID가 사용되는 씬 확인
rg -n -F "ad63486779c723d46b952640bdae360c" Assets -g "*.unity" -g "*.prefab"
```

PR이 이미 병합되었거나 `dev`가 변경되었다면 현재 브랜치에 바로 추가 작업하지 말고 먼저 PR 상태와 `origin/dev`의 최신 커밋을 확인한다.

## 12. 완료 조건

사운드 기능은 최소한 다음 조건을 만족해야 완료로 판단할 수 있다.

- 빌드 대상 게임 씬에서 BGM이 실제 재생된다.
- AMBIENT, TENSION, CHASE, ENDING 전환 조건이 확정된 GDD와 일치한다.
- ENDING BGM이 무음으로 끝나지 않는다.
- 추격 종료 시 TENSION을 거쳐 AMBIENT로 복귀한다.
- 발전기 루프, QTE 성공·실패, 완료음이 거리 감쇠와 함께 정상 재생된다.
- GDD 14.2의 플레이어, Chase AI, 발전기, 환경 필수 효과음이 연결된다.
- Master/BGM/SFX 볼륨을 설정 화면에서 조절하고 저장할 수 있다.
- Unity Editor Console에 관련 오류가 없다.
- Play Mode 또는 자동화 테스트로 주요 상태 전환을 재현한 기록이 있다.
- 사용한 외부 오디오 에셋의 출처와 라이선스가 문서화되어 있다.
