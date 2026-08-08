# 사운드 구현 현황 및 작업 인수인계

작성일: 2026-08-08 (이전 판 2026-08-06 갱신)

대상 Unity 버전: `6000.3.20f1`

대상 브랜치: `SoundFeature`

작성 기준 커밋: `8ea122c` (`발전기 - 효과음 Spatial Blend 3D 고정`)

Pull Request: [#15 사운드 시스템 및 Master AI 기반 BGM 전환 추가](https://github.com/IIBluEll/Project_Hide-Seek/pull/15)

## 1. 문서 목적

이 문서는 현재 사운드 구현 범위, 담당 경계, 실제 씬 연결 상태, 기획서와의 차이, 확인된 문제와 다음 작업 순서를 기록한다.

다른 컴퓨터에서 작업을 재개할 때는 이 문서와 `GAME_DESIGN_DOCUMENT.md`의 14장을 먼저 확인한다.

## 2. 현재 결론

- BGM 재생기와 상태 전환 로직 구현 완료
- Master AI 이벤트 기반 BGM 상태 갱신 구현 완료
- 발전기 루프 및 이벤트 효과음 구현 완료, 3D 재생 문제 수정 완료
- `Master / BGM / SFX` AudioMixer 그룹 구성 완료
- Chase AI 효과음은 **다른 담당자가 구현**해 `dev`에서 합류했으나 클립과 믹서 그룹이 비어 있어 소리가 나지 않음

아직 실제 게임 빌드 씬에는 사운드 시스템이 연결되지 않았다. 현재 상태는 **구조와 프리팹 구현 완료, 씬 통합 및 전체 효과음 구현 전**이다.

## 3. Git 및 PR 상태

| 항목 | 상태 |
| --- | --- |
| 로컬 브랜치 | `SoundFeature` |
| 원격 브랜치 | `origin/SoundFeature`와 동기화 |
| `origin/dev` 대비 | **0 behind / 9 ahead** (dev 통합 완료) |
| PR | `SoundFeature` → `dev`, #15 열림 |
| 병합 가능 여부 | `MERGEABLE` / `CLEAN` |
| CI 검사 | 등록된 검사 없음 |

주요 커밋:

| 커밋 | 내용 |
| --- | --- |
| `7f6e640` | 발전기 사운드, AudioMixer, 오디오 에셋 도입 |
| `95aefea` | AI 상태에 따른 BGM 전환과 BGM Player 추가 |
| `fa9218c` | ENDING 상태와 게임 진행도 연동 추가 |
| `99f6cfa` | Master AI 공용 이벤트 기반 구조로 변경 |
| `69736bb` | `origin/dev` 병합 (충돌 0건, Chase AI 사운드 합류) |
| `8ea122c` | 발전기 효과음 Spatial Blend 3D 고정 |

`69736bb` 병합은 충돌이 없었다. 두 브랜치가 수정한 파일 집합의 교집합이 0개였기 때문이다. 병합 후 `MasterAIProvider`가 의존하는 `ChaseAIController.StateChanged`, `RetreatFailed`, `CurrentState`와 `CHASE_AI_STATE` 7개 값이 모두 유지됨을 확인했다.

## 4. 담당 경계

사운드는 한 사람이 전부 맡지 않는다. 이 문서 작성자(형곤)의 담당 범위는 다음과 같다.

| 영역 | 담당 | 비고 |
| --- | --- | --- |
| BGM 재생과 상태 전환 | 형곤 | |
| 발전기 효과음 | 형곤 | 발전기 기능 담당이 함께 소유 |
| AudioMixer 그룹 구성 | 형곤 | |
| 플레이어와 Chase AI 거리 기반 심장 소리 | 형곤 | 미착수, 설계 논의 필요 |
| Chase AI 효과음 | 다른 담당자 | `ChaseAISound.cs` |
| 플레이어 효과음 | 다른 담당자 | |
| 사운드 설정 UI와 저장 | UI 담당 | GDD 13.2, 아래 참고 |

**볼륨 설정은 UI 담당 영역이다.** GDD 13.2 "설정"은 13장 "UI와 사용자 경험"에 속하며 사운드 항목으로 전체 볼륨, 배경 음악, 효과음 세 가지를 요구한다. 사운드 쪽 책임은 **설정 UI가 호출할 훅을 노출하는 것까지**이며, 설정 화면과 저장 데이터는 UI 담당이 소유한다. `GeneratorQteProvider`가 QTE 키를 UI 담당 키 설정 시스템으로 넘기기로 한 것과 같은 방식이다.

## 5. 구현된 구조

### 5.1 BGM 재생

- `Assets/01_Main/02_Scripts/Sound/BgmPlayer.cs`
- `Assets/01_Main/03_Prefabs/Sound/BGM Player.prefab`

AudioSource 두 개를 교대로 사용해 교차 페이드한다.

- 기본 페이드 시간 2초, 기본 방식 Equal Power
- BGM AudioSource는 2D, 출력은 `BGM` 그룹
- 페이드 중 새 곡 요청과 이전 곡 복귀를 모두 처리한다
- `AudioClip`에 `null`을 전달하면 현재 BGM만 페이드 아웃한다
- `loop`, `playOnAwake`, 초기 볼륨은 `SetUpSource`가 코드에서 맞춘다

프리팹 검증 결과 두 AudioSource 모두 Spatial Blend 0, 출력 `BGM` 그룹, `playOnAwake` 꺼짐으로 정상이다. **ENDING 클립만 비어 있다.**

### 5.2 BGM 상태 결정

- `Assets/01_Main/02_Scripts/Integration/BgmStateConnector.cs`
- `Assets/01_Main/02_Scripts/AI/MasterAI/MasterAIProvider.cs`
- `Assets/01_Main/02_Scripts/Gameplay/GameProgressProvider.cs`

`BgmStateConnector`는 Chase AI를 직접 참조하지 않고 `MasterAIProvider`를 AI 공용 접근점으로 사용한다.

구독 이벤트: `MasterAIProvider.ChaseStateChanged`, `MasterAIProvider.PlayerZoneChanged`, `GameProgressProvider.AllGeneratorsCompleted`, `GameProgressProvider.CompletedGeneratorCountChanged`.

상태 변경 없이 AI가 Zone 경계를 통과하는 경우를 보완하기 위해 0.25초마다 AI 위치를 다시 판정한다.

| Chase AI 조건 | BGM 상태 |
| --- | --- |
| `CHASE`, `ATTACK` | `CHASE` |
| `DORMANT`, `RETREAT` | `AMBIENT` |
| `PATROL`, `INVESTIGATE`, `SEARCH`이면서 플레이어와 같은 Zone | `TENSION` |
| 위 상태에서 다른 Zone | `AMBIENT` |
| 모든 발전기 완료 | `ENDING` 고정 |

전환 규칙:

- 긴장 상승은 단계를 건너뛸 수 있다 (`AMBIENT → CHASE` 가능)
- 긴장 하강은 한 단계씩 처리한다 (`CHASE → TENSION → AMBIENT`)
- `TENSION`은 최소 6초 유지한다
- `ENDING` 진입 후에는 AI 상태가 변해도 유지한다
- 같은 씬에서 진행도가 0으로 초기화되면 ENDING 고정을 해제한다

프리팹의 Provider 참조는 비어 있다. 프리팹이 씬 오브젝트를 참조할 수 없어 `Start()`에서 `FindFirstObjectByType`으로 찾는다. 씬에 각 Provider가 하나씩 있어야 한다. `AI_Prototype.unity`에는 두 Provider가 각각 1개씩 존재함을 확인했다.

### 5.3 현재 BGM 클립 연결

| 상태 | 클립 | 상태 |
| --- | --- | --- |
| `AMBIENT` | `Horror Elements/Ambient/Amb_Rumble.wav` | 연결됨 |
| `TENSION` | `Horror Elements/Ambient/Amb_Burn.wav` | 연결됨 |
| `CHASE` | `Horror Elements/Ambient/Amb_Run_2.wav` | 연결됨 |
| `ENDING` | 없음 | **미연결** |

### 5.4 발전기 효과음

- `Assets/01_Main/02_Scripts/Generator/GeneratorSound.cs`
- `Assets/01_Main/03_Prefabs/Generator/Generator.prefab`

`GeneratorSound`는 `Generator`가 사운드를 직접 알지 않도록 이벤트를 구독하는 어댑터다.

| 이벤트/상태 | 동작 | 클립 |
| --- | --- | --- |
| 비활성 상태 | 기계 루프 | `Small Machine.ogg` |
| 수리 시작 | 수리 루프 | `Machine Bits 2.ogg` |
| 수리 중단 | 상태에 맞는 루프로 복귀 | 비활성 또는 완료 루프 |
| 완료 상태 | 완료 후 기계 루프 | `Machine Bits 4.ogg` |
| QTE 성공 | 단발음 | `Hit_Anvil.wav` |
| QTE 실패 | 단발음 | `Hit_Metal.wav` |
| 발전기 완료 | 단발음 | `Misc_breath.wav` |

출력은 `SFX` 그룹, 최대 감쇠 거리 35m, Rolloff는 Linear다.

**Spatial Blend는 `Awake`에서 두 AudioSource 모두 1로 강제한다.** 이전에는 인스펙터 값에 맡겼고 EventSource가 2D로 남아 QTE 판정음과 완료음이 거리와 무관하게 같은 크기로 들렸다. 프리팹 값도 3D로 고쳤으나, 값이 다시 틀어져도 재발하지 않도록 코드에서 보장한다.

### 5.5 Chase AI 효과음 (다른 담당자)

- `Assets/01_Main/02_Scripts/AI/ChaseAI/ChaseAISound.cs`

`74f432b`에서 합류했다. 걷기·중간 뜀·전력질주별 발소리 묶음, 접촉음, 상태별 울음소리, Pitch 무작위화를 제공하며 3D 재생과 `SFX` 출력을 코드에서 보장한다. 설계 방향은 이 문서의 구조와 일치한다.

**단, 현재 소리가 나지 않는다.** `AI_Prototype.unity`의 컴포넌트에서 `_outputMixerGroup`이 비어 있고 클립 배열 12개가 모두 비어 있다. `_chaseAIController`와 `_chaseAIAnimator` 참조는 정상이다. 클립 선정은 AI 연출 의도를 아는 담당자 몫이다.

### 5.6 AudioMixer

- `Assets/01_Main/04_Resource/Sound/AudioMixer.mixer`

| 그룹 | fileID | 노출 파라미터 |
| --- | --- | --- |
| `Master` | `24300002` | `MasterVolume` |
| `BGM` | `-6546280980258379326` | `BgmVolume` |
| `SFX` | `5812720547532978006` | `SfxVolume` |

에셋 GUID는 `7e32191a7ca4fd9499235a3620f0f980`이다. 파라미터를 설정 UI나 저장 데이터와 연결하는 코드는 아직 없다.

## 6. 실제 씬 연결 상태

빌드 설정에 등록된 씬은 `Assets/01_Main/01_Scene/SampleScene.unity` 하나다.

- `SampleScene`은 Main Camera, Directional Light, Global Volume만 있는 **사실상 빈 기본 씬**이다. AudioSource가 0개다.
- `BGM Player.prefab`은 어떤 씬이나 프리팹에도 배치되어 있지 않다.
- 플레이어는 `Assets/02_Prototype/JW/Scene/PlayerTestScene.unity`, Chase AI는 `Assets/01_Main/01_Scene/AI_Prototype.unity`에 있고 둘 다 빌드 미등록이다.

따라서 현재 빌드를 실행해도 BGM과 발전기 효과음은 재생되지 않는다. 이는 사운드만의 문제가 아니라 프로젝트 전체의 씬 통합 문제다.

**검증 방침:** 씬 통합은 다른 담당자의 작업이 끝나야 하므로, 사운드는 `AI_Prototype`에서 Play Mode로 검증만 하고 **씬 변경은 커밋하지 않는다.** 커밋 산출물은 프리팹과 코드로만 남긴다.

## 7. GDD와 현재 구현의 차이

기준은 `GAME_DESIGN_DOCUMENT.md` 14.1과 14.2다.

| GDD 요구사항 | 현재 구현 | 후속 조치 |
| --- | --- | --- |
| `SAFE` 상태 | 상태 자체가 없음 | SAFE 복구 여부 또는 현 4단계 공식화 결정 |
| TENSION: 같은 Zone 또는 인접 Zone | 같은 Zone만 판정 | 인접 Zone 포함 여부 확정 |
| TENSION: 소음을 조사 중 | 조사 상태만으로는 켜지지 않음 | 위치 정보 노출 문제 포함해 기획 확인 |
| AMBIENT: 긴장도가 낮음 | `GlobalStress` 미사용 | 사용할 값과 임계값 결정 |
| ENDING: 발전기 완료 및 탈출 장치 활성화 | 발전기 완료 즉시 진입 | 탈출 장치 구현 후 이벤트 이동 여부 결정 |
| 페이드 또는 레이어 전환 | 교차 페이드 구현 | 완료 |
| 추격 종료 후 TENSION 경유 | 구현 | 완료 |

코드 주석에는 2026-08-03 기획 확인으로 SAFE 제거와 ENDING 조건 변경을 결정했다고 기록되어 있으나 GDD가 갱신되지 않았다. 병합 전 기준을 하나로 통일해야 한다.

### 7.1 전기 스파크 조건이 GDD 안에서 엇갈린다

GDD가 전기 스파크를 세 곳에서 다르게 언급한다.

| 위치 | 서술 | 함의 |
| --- | --- | --- |
| 7.4 QTE 규칙 | "실패 시 붉은 피드백, **스파크**, 큰 소음을 제공한다" | QTE **실패** 연출 |
| 14.2 발전기 | "**완료음과 전기 스파크**" | **완료** 연출 |
| 12장 환경 사운드 | "전기 스파크와 금속 충격" | **환경** 상시 앰비언스 |

7.4는 스파크를 QTE 실패 피드백으로 정의하고, 14.2는 완료음과 한 줄에 묶어 완료 연출처럼 읽힌다. 현재 구현은 QTE 실패에 `Hit_Metal.wav` 단발음만 있고 스파크 전용 클립이나 연출은 없다.

**작업 전에 어느 쪽인지 확정해야 한다.** 실패 연출이라면 기존 실패음을 스파크음으로 교체하거나 겹쳐 쓰는 문제이고, 완료 연출이라면 완료음에 추가하는 별개 작업이다. 환경 스파크는 위 둘과 무관한 환경 담당 항목이다.

## 8. GDD 필수 효과음 진행도

| 영역 | 요구사항 | 진행 상태 |
| --- | --- | --- |
| 플레이어 | 걷기, 달리기, 앉아서 이동 | 미구현 |
| 플레이어 | 디코이 투척 | 미구현 |
| 플레이어 | 상호작용 | 미구현 |
| Chase AI | 발소리와 울음소리 | 코드 구현, **클립 미배선** |
| Chase AI | 수색과 공격 | 코드 구현, **클립 미배선** |
| Chase AI | Vent 출입 | 미구현 |
| 발전기 | 비활성 기계음과 수리음 | 구현 |
| 발전기 | QTE 성공·실패 | 구현 |
| 발전기 | 완료음 | 구현 |
| 발전기 | 전기 스파크 | 미구현, **조건 미확정** (7.1 참고) |
| 환경 | 사이렌, 전기음, 환풍기 | 미구현 |
| 환경 | 금속 충격, 문, 조명 점멸 | 미구현 |
| 컷신 | 시작, 사망, 엔딩 사운드 연출 | 미구현 |

## 9. 확인된 문제와 위험 요소

### P0: 씬 미배치

BGM Player가 씬에 없으므로 BGM 전환 로직이 실행되지 않는다. 씬 통합 일정과 무관하게 `AI_Prototype`에서 먼저 검증한다.

### P0: ENDING 클립 누락

`BGM Player.prefab`의 `_endingClip`이 비어 있다. ENDING 전환 시 `Play(null)`이 호출되어 BGM이 정지한다.

### P0: ChaseAISound 미배선 (다른 담당자)

`_outputMixerGroup`이 비어 있어 `SFX` 볼륨 제어 밖으로 빠지고, 클립 배열 12개가 비어 있어 실제 소리가 없다.

### P1: GDD와 상태 정의 불일치

SAFE, 인접 Zone, 소음 조사, GlobalStress, 탈출 장치 조건, 전기 스파크 조건을 기획과 맞춰야 한다. 코드를 먼저 바꾸기보다 GDD 갱신 여부를 먼저 결정한다.

### P1: 필수 효과음 대부분 미구현

플레이어, 환경 효과음과 컷신 사운드가 없다.

### P2: BGM 임포트 설정

BGM 3개 모두 Load Type이 `Decompress On Load`, `Preload Audio Data`가 켜져 있다.

| 클립 | 길이 | 파일 | 형식 |
| --- | --- | --- | --- |
| `Amb_Rumble` (AMBIENT) | 83초 | 14.0MB | 44.1kHz 스테레오 16bit |
| `Amb_Burn` (TENSION) | 119초 | 20.1MB | 44.1kHz 스테레오 16bit |
| `Amb_Run_2` (CHASE) | 32초 | 5.4MB | 44.1kHz 스테레오 16bit |

`Decompress On Load`는 로드 시점에 압축을 풀어 **원본 PCM 크기 그대로 메모리에 상주**시킨다. 세 클립 합계 약 40MB가 씬 로드 시점부터 계속 점유된다. BGM은 길고 2D이며 동시에 최대 두 개만 울리므로 `Streaming`이 표준 선택이다. `Streaming`은 디스크에서 조금씩 읽어 메모리를 거의 쓰지 않는 대신 디스크 접근과 약간의 CPU를 쓴다. Profiler로 확인 후 전환한다.

### P2: 볼륨 설정 훅 없음

AudioMixer 파라미터는 노출되어 있으나 이를 적용·저장하는 코드가 없다. 설정 UI는 UI 담당 영역이므로 사운드 쪽은 훅만 제공한다 (4장 참고).

### P2: 에셋 및 PR 정리 필요

- 오디오 파일 107개, 약 261MB가 추가되었다
- 실제 연결된 클립은 BGM 3개와 발전기 6개다
- 미사용 WAV/OGG 중복 정리 여부를 검토한다
- `.meta`에 Unity Asset Store productId와 packageName은 남아 있으나 GDD 16.3이 요구하는 출처 URL, 제작자, 라이선스, 사용 날짜 목록은 없다

## 10. 발전기 QTE 검증 시 주의

발전기 QTE 효과음을 확인할 때 다음을 알고 있어야 한다. 아래 내용은 다른 담당자 코드의 동작이다.

`GeneratorQteProvider._qteInputSource`에는 `PlayerQTEInputSource`가 인스펙터로 주입된다. 이 컴포넌트는 `PlayerInputReader.OnJumpEvent`를 구독하므로 **QTE 키는 점프 키(Space)와 같다.**

`GeneratorQteProvider`는 프리팹 안에 있고 `PlayerQTEInputSource`는 플레이어 오브젝트에 있어 **프리팹 에셋에서는 이 참조를 할당할 수 없다.** 씬 인스턴스에서만 할당된다. `PlayerTestScene`에는 이미 override로 연결되어 있으므로 QTE 검증은 그 씬에서 하는 것이 빠르다. 새로 구성한다면 `GeneratorTest.prefab`을 써야 한다. `Generator Provider.prefab`은 View 참조 두 개가 모두 비어 있어 `Awake`에서 오류가 난다.

**점프 후 첫 QTE가 즉시 실패한다.** `PlayerQTEInputSource.IsQteKeyDown()`은 읽으면 소비되는 래치인데, 플래그는 QTE와 무관하게 Space를 누를 때마다 켜지고 `Generator`는 QTE 활성 중에만 이 값을 읽는다. 평소 점프로 켜진 플래그가 남아 있다가 QTE 첫 프레임에 소비되며, 그 시점의 인디케이터가 0이라 실패로 판정된다. 실패음만 계속 들린다면 사운드 배선이 아니라 이 문제다. 검증 시 수리 시작 전에 점프하지 않는다.

`GeneratorQteProvider._qteKey`는 현재 동작하지 않는 필드다. `KeyboardInputSource` 생성이 주석 처리되어 이 값은 실제 키에도 UI 라벨에도 반영되지 않는다. 라벨은 `PlayerQTEInputSource.GetQteKeyLabel()`이 `"Space"`로 고정 반환한다.

## 11. 남은 작업

담당과 산출물 기준으로 나눈다.

**형곤 담당 (커밋 산출물 있음)**

| 작업 | 산출물 | 선행 조건 |
| --- | --- | --- |
| ENDING 클립 할당 | `BGM Player.prefab` | 클립 확정 |
| 전기 스파크음 | `GeneratorSound.cs`, 프리팹 | **조건 확정 필요** (7.1) |
| BGM Streaming 전환 | `.wav.meta` | Profiler 확인 |
| 볼륨 적용 훅 | 신규 코드 | UI 담당과 인터페이스 합의 |
| 심장 소리 | 신규 코드 | 설계 논의 |
| GDD 14.1 정리 | `GAME_DESIGN_DOCUMENT.md` | 기획 확정 |

**형곤 담당 (커밋 산출물 없음, 검증만)**

- `AI_Prototype`에 BGM Player 배치 후 상태 전환 청취
- 발전기 루프와 QTE 판정음 거리 감쇠 청취

**다른 담당자**

- 씬 통합
- `ChaseAISound` 클립과 믹서 그룹 배선
- 플레이어·환경·컷신 효과음
- 사운드 설정 UI와 저장
- QTE 입력 래치 문제 (10장)

## 12. 검증 상태

| 검증 항목 | 결과 |
| --- | --- |
| Git 작업 트리 | 깨끗함 |
| `origin/dev` 통합 | 완료, 충돌 0건 |
| 병합 후 API 정합성 | 정적 확인 완료 |
| PR 병합 가능 여부 | `MERGEABLE` / `CLEAN` |
| BGM/발전기 직렬화 참조 확인 | 완료 |
| 빌드 씬 배치 확인 | 미배치 확인 |
| 사운드 자동화 테스트 | 없음 |
| Unity Editor 컴파일 확인 | **미완료** |
| Unity Play Mode 청취 테스트 | **미완료** |
| 전체 Unity 빌드 | **미완료** |

독립 `dotnet build Assembly-CSharp.csproj`는 Unity가 생성한 프로젝트에서 `UnityEngine.UI` 참조를 찾지 못해 실패한다. 이는 환경 문제이며 사운드 코드의 컴파일 가능 여부를 증명하지도, 반증하지도 않는다. Unity Editor 컴파일을 대체할 수 없다.

`69736bb` 병합으로 `AI_AnimCtrl.controller`, 애니메이션 3종, `TagManager.asset`이 함께 들어왔다. Editor Console에 애니메이션 관련 경고가 보인다면 사운드 변경이 아니라 이쪽일 가능성이 높다.

## 13. 작업 재개 방법

```powershell
git fetch origin
git switch SoundFeature
git pull --ff-only origin SoundFeature
git status --short --branch
gh pr view 15
```

Unity Hub에서 Unity `6000.3.20f1`로 연다.

확인 문서:

1. `GAME_DESIGN_DOCUMENT.md` 14장과 16.3
2. 이 문서
3. `Assets/01_Main/02_Scripts/Integration/BgmStateConnector.cs`
4. `Assets/01_Main/02_Scripts/Sound/BgmPlayer.cs`
5. `Assets/01_Main/02_Scripts/Generator/GeneratorSound.cs`

배치 확인 명령:

```powershell
rg -n -F "48733292fc74a6a4cbcd7b26e02428af" Assets -g "*.unity" -g "*.prefab"
rg -n -F "ad63486779c723d46b952640bdae360c" Assets -g "*.unity" -g "*.prefab"
```

## 14. 완료 조건

- 빌드 대상 게임 씬에서 BGM이 실제 재생된다
- AMBIENT, TENSION, CHASE, ENDING 전환 조건이 확정된 GDD와 일치한다
- ENDING BGM이 무음으로 끝나지 않는다
- 추격 종료 시 TENSION을 거쳐 AMBIENT로 복귀한다
- 발전기 루프, QTE 성공·실패, 완료음이 거리 감쇠와 함께 정상 재생된다
- GDD 14.2의 플레이어, Chase AI, 발전기, 환경 필수 효과음이 연결된다
- Master/BGM/SFX 볼륨을 설정 화면에서 조절하고 저장할 수 있다
- Unity Editor Console에 관련 오류가 없다
- Play Mode 또는 자동화 테스트로 주요 상태 전환을 재현한 기록이 있다
- 사용한 외부 오디오 에셋의 출처와 라이선스가 문서화되어 있다
