# 형곤 작업 현황

갱신 2026-08-09 · 브랜치 `dev` · 기준 커밋 `eb56f605` · Unity `6000.3.20f1`

결정이 필요한 항목은 [`DECISIONS.md`](DECISIONS.md)에 있고 `D-n`으로 참조한다. **GDD는 직접 수정하지 않는다.** 수정 요청도 그 문서로 모은다.

## 한눈에 보기

| 영역 | 상태 | 남은 것 |
| --- | --- | --- |
| 발전기 수리·QTE·진행도 | 완료 | 난이도 Config 주입 경로 |
| 발전기 후보 활성화 | 완료 | 시작·탈출 지점 거리 규칙 |
| 발전기 위치 표시 | 완료 | GDD 문구 반영 |
| 발전기 효과음·VFX | 완료 | — |
| BGM 4상태 전환 | 완료 | 상태 정의 확정 (D-1) |
| 사망·탈출 컷신 | 완료 | 사운드 연출 |
| 시작 컷신 | **미착수** | 전부 |
| 결과 화면 | 부분 | 플레이 시간·완료 수 표시 |
| UI 연출 (페이드·전환·버튼) | **미착수** | 전부 |
| 볼륨 설정 훅 | 미착수 | 인터페이스 합의 (D-3) |
| 심장 소리 | 미착수 | 설계 논의 (D-5) |
| 웹 빌드 오디오 설정 | 미착수 | 용량 상한 확정 (D-2) |

**빌드하면 아무것도 확인할 수 없다.** Build Settings에 등록된 씬은 `SampleScene`(빈 씬) 하나뿐이고 `InGame_Map`이 없다. 에디터 플레이로만 검증 가능하다.

---

## 1. 발전기와 QTE

수리·QTE·진행도 감소 로직, 후보 지점 활성화, 위치 표시, 스파크 VFX까지 구현 완료. `InGame_Map`에 `GeneratorSystem` 배치 완료(`72c42503`), 에디터 플레이 검증 완료.

**후보 활성화는 런타임 생성이 아니라 미리 배치 + `SetActive`다.** 라이트맵·NavMesh 베이크에 포함시켜야 하고, 발전기 모델이 벽 패널이라 배치 각도를 눈으로 맞춰야 하기 때문이다.

```
후보 수집(비활성 포함) → IsAvailable 필터 → 셔플
→ 1차: Zone이 겹치지 않게 채움 → 2차: 모자라면 Zone 무시하고 채움
→ 선택분 SetActive(true) / 나머지 SetActive(false)
```

- **Zone 분산은 소프트 제약이다.** 2차 패스는 Zone을 다시 보지 않고 경고도 내지 않는다. GDD 7.1의 "몰리지 않도록 제한한다"가 금지가 아니기 때문이며, **추가 균등화는 하지 않기로 확정했다**(2026-08-09). 후보/필요 개수가 늘어 쏠림이 실제로 보이면 그때 라운드로빈으로 바꾼다.
- **후보 부족은 하드 실패다.** 유효 후보가 요청 수보다 적으면 `GameProgressProvider.SetRequiredGeneratorCount`로 클리어 조건을 실제 수까지 낮춘다. 그러지 않으면 남은 발전기를 다 고쳐도 조건이 영원히 만족되지 않는다.

**등록 창구는 `GeneratorRegistry` 하나다.** `Generator.Enabled`를 직접 구독하는 코드는 이 클래스뿐이고, 나머지는 `Attach` / `Detach` 두 줄만 쓴다. 구독자는 `GeneratorDirector`(Config 배포·완료 집계·후보 활성화), `GeneratorPresenterHost`(QTE 입력·Presenter 수명), `GeneratorHighlightController`(실루엣 배포) 셋이다.

**프리팹 셋.** `Generator`(발전기 1기) / `GeneratorUI`(진행도·QTE 캔버스) / `GeneratorSystem`(앞의 셋 + `GeneratorUI` 중첩). `GeneratorSystem`은 씬에 끌어다 놓기만 하면 배선이 끝난다.

**위치 표시.** 키 입력으로 정해진 시간만 보이고 스스로 꺼진다. 게임당 사용 횟수 제한, 표시 중 재입력 무시, 완료된 발전기는 표시 안 함. 기능은 합의됐고 GDD 13.1 반영만 남았다.

### 구현하지 않는 것

| 항목 | 사유 |
| --- | --- |
| 튜토리얼용 발전기 제외 플래그 | 튜토리얼이 별도 씬이 되어 불필요해졌다. 그 씬에 `GameProgressProvider`를 두지 않으면 끝난다. **본 게임 씬에 튜토리얼이 합쳐질 때만 재검토한다** |
| Zone 분산 2차 패스 균등화 | 위 참고 |

### GDD 7.1 미구현 규칙

"시작 지점과 지나치게 가까운 후보 제외", "탈출 지점과 인접한 후보 수 제한" 둘 다 미구현이다. `EscapeTrigger`가 생겨 **탈출 지점 기준은 확보됐고**, 시작 지점은 아직 명시적 오브젝트가 없다. 판정 근거는 `GeneratorCandidatePoint`에 추가한다.

---

## 2. 사운드

### BGM

`BgmPlayer`가 AudioSource 두 개를 교대로 써서 교차 페이드한다(기본 2초, Equal Power). `BgmStateConnector`가 `MasterAIProvider`를 AI 공용 창구로 삼아 상태를 정한다. Chase AI를 직접 참조하지 않는다.

| Chase AI 조건 | BGM 상태 |
| --- | --- |
| `CHASE`, `ATTACK` | `CHASE` |
| `DORMANT`, `RETREAT` | `AMBIENT` |
| `PATROL`/`INVESTIGATE`/`SEARCH` + 플레이어와 같은 Zone | `TENSION` |
| 위 상태에서 다른 Zone | `AMBIENT` |
| 모든 발전기 완료 | `ENDING` 고정 |

긴장 상승은 단계를 건너뛸 수 있고(`AMBIENT → CHASE`), 하강은 한 단계씩 처리한다(`CHASE → TENSION → AMBIENT`). `TENSION`은 최소 6초 유지. 상태 변경 없이 AI가 Zone 경계를 넘는 경우를 위해 0.25초마다 위치를 재판정한다.

**클립 4상태 모두 연결 완료.** `InGame_Map`에 BGM Player 배치 완료. **전환 조건별 청취 검증 4건 완료**(2026-08-09) — ENDING 무음 종료 여부, TENSION 경유 복귀, TENSION 6초 유지, 거리 감쇠.

### 발전기 효과음과 VFX

`GeneratorSound`는 `Generator`가 사운드를 알지 않도록 이벤트를 구독하는 어댑터다. 비활성 루프 / 수리 루프 / 완료 루프와 QTE 성공·실패·완료 단발음을 재생한다. 출력 `SFX`, 최대 감쇠 35m, Linear rolloff.

**Spatial Blend는 `Awake`에서 두 AudioSource 모두 1로 강제한다.** 인스펙터 값에 맡겼더니 EventSource가 2D로 남아 QTE 판정음이 거리와 무관하게 들린 적이 있다. 값이 다시 틀어져도 재발하지 않게 코드에서 보장한다.

**전기 스파크는 VFX로 확정됐다.** `GeneratorVfx`가 상시 스파크 루프와 QTE 실패 폭발을 담당하고, 소리는 기존 QTE 실패음(`Hit_Metal.wav`)이 맡는다. **전용 사운드는 만들지 않는다.**

### AudioMixer

`Master` / `BGM` / `SFX` 세 그룹에 `MasterVolume` / `BgmVolume` / `SfxVolume`이 노출되어 있다. 에셋 GUID `7e32191a7ca4fd9499235a3620f0f980`. **적용·저장하는 코드는 아직 없다**(D-3).

### 웹 빌드 제약

빌드 타겟은 WebGL이고 **이 플랫폼은 오디오 `Streaming`을 지원하지 않는다.** 클립을 통째로 받아야 재생이 시작되므로 BGM 길이가 곧 초기 로딩 시간이다. 압축은 AAC를 쓴다(Vorbis는 WebGL 대상이 아니다).

현재 BGM 4개 모두 플랫폼 오버라이드가 없고 합계 약 54MB다. ENDING 클립만 24bit인데 최종 인코딩이 AAC라 이점이 없고 용량만 1.5배다.

권장 방향은 **BGM과 SFX를 반대로 튜닝하는 것**이다. BGM은 `Compressed In Memory` + Preload 끄기 + Load In Background 켜기(첫 재생 지연은 2초 페이드인이 흡수한다). 짧은 SFX는 `Decompress On Load`와 Preload를 유지한다 — 압축을 걸면 QTE 판정음이 늦어 판정과 어긋나 들린다. Force To Mono는 호러 앰비언스의 공간감을 잃으므로 BGM에 쓰지 않는다.

총 용량 상한이 없어 어느 수준까지 낮출지 정할 수 없다(D-2).

---

## 3. 컷신과 UI

`CutscenePlayer` 하나를 사망 컷신과 탈출 컷신이 공유하고, 차이는 전부 인스펙터 설정이다. 연출 타이밍은 Timeline이 소유하고 이 클래스는 재생과 완료 통보만 한다.

**플레이어 컴포넌트를 건드리지 않는다.** 화면은 컷신 카메라의 Priority가 더 높고 배경을 불투명하게 지우는 것으로 덮는다. 조작 정지는 `Started` 구독자가 처리해야 하는데 **아직 아무도 구독하지 않는다**(4장 참고).

**월드 오브젝트를 Timeline에 바인딩하는 컷신은 프리팹으로 만들면 안 된다.** 바인딩이 Director 인스턴스에 저장되어 프리팹 에셋이 씬 오브젝트를 참조할 수 없다.

결과 화면은 재시작·타이틀 버튼만 구현되어 있고 **플레이 시간과 완료한 발전기 수 표시가 없다**(GDD 13.1). 두 버튼은 대상 씬이 Build Settings에 있어야 동작하며, 없으면 아예 비활성으로 표시하고 경고를 남긴다. 씬 등록만 되면 코드 수정 없이 동작한다.

시작 컷신, 화면 전환, 페이드, 버튼 애니메이션은 미착수다.

---

## 4. 담당 경계

발전기·컷신 코드는 다른 담당 영역의 타입을 직접 참조하지 않는다. 연결은 이벤트 훅과 `Integration` 계층으로만 한다.

| 상대 | 연결 방식 |
| --- | --- |
| 진우 (상호작용) | `TryBeginRepair` / `CancelRepair` 호출, `IInputSource` 주입 |
| 현민 (소음·Anger) | `GeneratorAIConnector`가 발전기 이벤트를 `NoiseEmitter`로 변환 |
| 현민 (UI) | 읽기 전용 인터페이스 두 개로 표시값만 전달 |
| AI 전반 | `MasterAIProvider`가 단일 외부 창구. `ChaseAIController`를 직접 참조하지 않는다 |

```text
Generator.RepairNoiseOccurred / QteFailureNoiseOccurred
→ GeneratorAIConnector → NoiseEmitter.EmitNoiseAt → NoiseProvider → ChaseAIPerception

Generator.Completed
→ GeneratorAIConnector → GameProgressProvider.NotifyGeneratorCompleted()
→ CompletedGeneratorCountChanged → ChaseAIAnger (Anger 하한선)

ChaseAIController.PlayerCaught → MasterAIProvider.PlayerCaught
   ├ (플레이어 담당) 조작 정지        ← 미구현
   └ PlayerDeathConnector → 사망 컷신 → 게임 오버 화면   ← 완료
```

**포획 시 플레이어 조작이 멈추지 않는다.** 컷신 카메라가 화면은 덮지만 그 아래에서 플레이어는 계속 움직인다. 연출 쪽에서 필요한 건 "멈춘다는 보장"뿐이고 방식은 플레이어 담당이 정한다. 전달할 것은 셋이다 — `MasterAIProvider.PlayerCaught`에 붙일 것, 입력 컴포넌트만 꺼서는 `MoveController`에 남은 이동 방향 때문에 부족하다는 것, Animator는 사망 연출에 필요하니 끄지 말 것.

---

## 5. 남은 작업

| 작업 | 선행 조건 |
| --- | --- |
| 시작 컷신과 사운드 연출 | 없음 |
| 사망·탈출 컷신 사운드 연출 | 없음 |
| 결과 화면에 플레이 시간·완료 수 표시 | 없음 |
| 화면 전환·페이드·버튼 애니메이션 | 없음 |
| 미사용 오디오 에셋 정리와 라이선스 문서화 | 없음 (GDD 16.3) |
| GDD 7.1 시작·탈출 지점 거리 규칙 | 시작 지점 오브젝트 |
| 웹 빌드 임포트 설정, ENDING 클립 16bit 변환 | D-2 |
| 볼륨 적용 훅 | D-3 |
| 심장 소리 | D-5 |
| GDD 문구 반영 요청 | D-1 |

---

## 6. 위험 요소

**P1 · `InGame_Map`이 Build Settings에 없다.** 프로젝트 전체 문제지만 결과 화면 버튼 두 개가 여기 걸려 비활성으로 뜬다. 씬 등록만으로 풀린다.

**P1 · 난이도 Config 주입 경로가 없다.** `01_Main/06_Data/Generator`에 `GeneratorConfig_Easy/Normal/Hard` 셋이 있으나 선택해서 넣는 흐름이 없어 GDD 12장의 "QTE 성공 판정 범위" 차이가 실제로 생기지 않는다.

**P1 · 필수 효과음 대부분이 없다.** 플레이어·환경 효과음과 컷신 사운드 미구현. BGM과 발전기가 닫힌 지금 사운드 완료를 막는 가장 큰 덩어리다.

**P1 · `ChaseAISound`가 배선되지 않았다.** 코드는 완성됐으나 `01_Main/03_Prefabs/AI/ChaseAI.prefab`에서 `_outputMixerGroup`이 비어 있고 클립 배열 12개가 전부 비어 있다. 타 담당(D-4).

**P2 · `GeneratorDirector` 중복 배치.** 씬에 둘 이상 있으면 완료가 중복 집계되어 필요 개수보다 적게 고쳐도 클리어된다. `[DisallowMultipleComponent]`는 같은 오브젝트만 막으므로 `Awake`에서 따로 확인해 `LogError`를 남긴다.

**P2 · `RequireComponent`의 자동 추가.** 부속 컴포넌트를 발전기 자식 오브젝트에 잘못 붙이면 그 자식에 `Generator`가 하나 더 생긴다. `ReportUnmanagedGenerators`가 잡아내지만 로그를 읽어야 한다.

**P2 · 오디오 에셋 261MB / 107개 중 실제 사용은 9개다.** 미사용 중복 정리와 GDD 16.3이 요구하는 출처·라이선스 목록 작성이 필요하다.

---

## 7. 작업 재개

```powershell
git fetch origin
git switch dev
git pull --ff-only origin dev
```

Unity Hub에서 `6000.3.20f1`로 연다. 웹 빌드를 만들려면 **WebGL Build Support**를 따로 추가해야 한다(기본 설치에 없다).

배치 확인:

```powershell
rg -n -F "48733292fc74a6a4cbcd7b26e02428af" Assets -g "*.unity" -g "*.prefab"
```

### QTE 검증 시 주의

`GeneratorPresenterHost._qteInputSource`에 `PlayerQTEInputSource`가 주입되고, 이 컴포넌트는 `PlayerInputReader.OnJumpEvent`를 구독하므로 **QTE 키는 점프 키(Space)와 같다.** 비어 있으면 `_qteKey`(기본 `Space`)로 만든 `KeyboardInputSource`가 폴백으로 쓰인다. 프리팹 에셋에서는 씬 오브젝트를 참조할 수 없어 `_qteInputSource`는 씬 인스턴스에서만 할당된다.

`Generator Provider.prefab`(`02_Prototype/HG/Prefab`)은 View 참조 두 개가 비어 `Awake`에서 오류가 난다. 검증에 쓰지 않는다. `GeneratorSystem.prefab`이 대체한다.

**점프로 켜진 입력 래치가 QTE 첫 프레임에 소비되어 즉시 실패로 판정되던 문제**는 2026-08-09 기준 해결로 확인했다. 실패음만 계속 들린다면 사운드 배선이 아니라 입력 쪽을 먼저 본다.
