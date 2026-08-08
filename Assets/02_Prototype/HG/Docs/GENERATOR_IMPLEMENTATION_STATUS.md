# 발전기 구현 현황 및 작업 인수인계

작성일: 2026-08-08

대상 Unity 버전: `6000.3.20f1`

대상 브랜치: `GeneratorFeature`

작성 기준 커밋: `c7153ad` (`발전기 - 부속 컴포넌트에 RequireComponent 적용`)

Pull Request: 작성 예정

## 1. 문서 목적

이 문서는 발전기 구현 범위, 담당 경계, 합의됐으나 아직 구현하지 않은 사항, 확인된 위험을 기록한다. **기술 상세를 담는 문서이며, 회의에서는 이 문서를 펴지 않는다.**

`SOUND_IMPLEMENTATION_STATUS.md`와 같은 용도이며, 회의 안건은 그 문서의 표기 방식을 따라 별도로 분리한다. **GDD는 직접 수정하지 않는다.** 기획 담당이 소유하는 문서이므로 수정 요청은 회의 안건으로 올린다.

## 2. 현재 결론

- 발전기 수리·QTE·진행도 감소 로직 구현 완료
- 후보 지점 활성화 시스템(GDD 7.1) 구현 완료
- 위치 표시(붉은 실루엣)를 지속 시간·사용 횟수 기반 소모성 능력으로 구현 완료
- 발전기 프리팹을 `Generator` / `GeneratorUI` / `GeneratorSystem` 셋으로 분리 완료
- 등록 배관을 `GeneratorRegistry`로 통합 완료
- 담당자 확인: 에디터 플레이 검증 완료 (2026-08-08)

실제 게임 빌드 씬에는 아직 연결하지 않았다. 현재 상태는 **구조와 프리팹 구현 완료, 씬 통합 전**이다.

### 2.1 이번 브랜치 종료 시점

남은 작업 대부분이 다른 담당자나 회의 결정에 걸려 있다.

| 막고 있는 것 | 영향받는 작업 |
| --- | --- |
| 진우 담당 상호작용 이관 | `GeneratorInteraction`의 `01_Main` 이동, `GeneratorInteractionTester` 제거 |
| 진우 담당 InputActions 확정 | 위치 표시 키, QTE 키 이관 |
| 현민 담당 UI Manager 확정 | `GeneratorPresenterHost` 제거, 남은 횟수 HUD |
| 기획 확정 | 위치 표시 사용 횟수·지속 시간의 GDD 반영 |
| 회의 결정 | Provider 명명 규약, 인터페이스 이름 |

## 3. 구현된 구조

### 3.1 후보 지점 활성화 (GDD 7.1)

**런타임 생성이 아니라 미리 배치 + `SetActive`다.** GDD 7.1이 "선택된 발전기만 활성화"로 적고 있고, 라이트맵·NavMesh 베이크에 포함시켜야 하며, 발전기 모델이 벽 패널이라 벽면마다 배치 각도를 눈으로 맞춰야 하기 때문이다.

`Generator.Enabled` / `Disabled`가 `OnEnable` / `OnDisable`에서 발행되므로, 켜는 것만으로 Config 배포·QTE·위치 표시·완료 집계 등록이 전부 따라온다.

선택 절차는 다음과 같다.

```
후보 수집(비활성 포함) → IsAvailable 필터 → 셔플
→ 1차: Zone이 겹치지 않게 채움 → 2차: 모자라면 Zone 무시하고 채움
→ 선택분 SetActive(true) / 나머지 SetActive(false)
```

**Zone 분산은 소프트 제약이다.** Zone 수보다 필요 개수가 많으면 2차 패스가 Zone을 무시하고 채우며, 경고를 내지 않는다. GDD 7.1의 "몰리지 않도록 제한한다"가 금지가 아니기 때문이다.

**후보 부족은 하드 실패다.** 유효 후보가 요청 수보다 적으면 `GameProgressProvider.SetRequiredGeneratorCount`로 클리어 조건을 실제 수까지 낮추고 경고를 남긴다. 그러지 않으면 남은 발전기를 다 고쳐도 조건이 영원히 만족되지 않는다.

### 3.2 등록 창구

`GeneratorRegistry`(순수 C# 클래스)가 구독·초기 훑기·중복 방지·정리를 맡는다. 발전기를 지켜보는 컴포넌트는 `Attach` / `Detach` 두 줄만 쓴다. **`Generator.Enabled`를 직접 구독하는 코드는 이 클래스뿐이다.**

현재 구독자는 셋이다.

| 구독자 | 하는 일 |
| --- | --- |
| `GeneratorDirector` | Config 배포, 완료 집계, 후보 활성화 |
| `GeneratorPresenterHost` | QTE 입력 배포, Presenter 수명 관리 |
| `GeneratorHighlightController` | 실루엣 머티리얼 배포, 표시 상태 적용 |

### 3.3 위치 표시

키를 누르면 정해진 시간 동안만 보이고 스스로 꺼진다. 게임당 사용 횟수가 정해져 있고, 표시 중 재입력은 무시한다. **완료된 발전기는 표시하지 않는다.**

GDD 13.1 인게임 HUD 항목에 위치 표시가 없어 **이 기능 자체가 GDD에 근거 조항이 없다.** 회의 안건이다.

### 3.4 프리팹 구성

| 프리팹 | 내용 |
| --- | --- |
| `Generator.prefab` | 발전기 1기. 로직·오디오·소음·모델·콜라이더·후보 마커 |
| `GeneratorUI.prefab` | 진행도와 QTE View를 담은 캔버스 |
| `GeneratorSystem.prefab` | Director·PresenterHost·HighlightController + `GeneratorUI` 중첩 |

`GeneratorSystem`은 **씬에 끌어다 놓기만 하면 배선이 끝난다.** View 참조가 중첩 프리팹 내부를 가리키고, Config와 머티리얼은 에셋이라 프리팹에 담긴다. 씬 오브젝트인 `GameProgressProvider`와 QTE 입력 소스는 `Reset`과 `Awake` 폴백이 채운다.

## 4. 담당 경계

발전기 코드는 다른 담당 영역의 타입을 직접 참조하지 않는다. 연결은 이벤트 훅과 `Integration` 계층으로만 한다.

| 상대 | 연결 방식 |
| --- | --- |
| 진우(상호작용) | `TryBeginRepair` / `CancelRepair` 호출, `IInputSource` 주입 |
| 현민(소음·Anger) | `GeneratorAIConnector`가 `NoiseEmitter`로 변환 |
| 현민(UI) | 읽기 전용 인터페이스 두 개로 표시값만 전달 |

## 5. 합의됐으나 구현하지 않은 사항

### 5.1 튜토리얼용 발전기 제외 플래그

**합의 완료, 구현 대기.** 2026-08-08 논의에서 방식을 확정했다.

**문제.** `GeneratorDirector`는 등록된 모든 발전기의 완료를 집계한다. 후보 마커(`GeneratorCandidatePoint`)를 제거한 발전기도 등록은 되므로 **완료하면 진행도가 올라간다.** 지금 구조에는 "이 발전기는 클리어 개수에 넣지 않는다"를 말할 방법이 없다. 튜토리얼이 별도 씬이면 `GameProgressProvider`를 두지 않는 우회가 가능하지만, 본 게임 씬 안의 튜토리얼 구간이면 쓸 수 없다.

**확정한 방식.** `GeneratorCandidatePoint`에 `_isManagedByDirector`(기본 `true`) 플래그를 둔다. 끄면 Director가 켜고 끄지 않고 완료도 집계하지 않는다.

상태는 셋이 된다.

| 후보 마커 | Managed | Director가 켜고 끔 | 진행도 집계 | 의미 |
| --- | --- | --- | --- | --- |
| 있음 | O | O | O | 일반 후보 |
| 있음 | X | X (배치 그대로) | X | 튜토리얼·연출용 고정 |
| 없음 | — | X | **O** | 실수 → 경고 |

마지막 줄은 의도적이다. 마커가 없으면 여전히 집계되어야 "실수로 컴포넌트를 지웠다"가 게임에 영향을 주고 로그로 드러난다.

**인스펙터 이벤트(UnityEvent)는 두지 않기로 했다.** 이유는 넷이다.

1. 프로젝트 전체에서 `UnityEvent` 사용처가 0건이며 게임 코드는 모두 C# `event Action<T>`를 쓴다.
2. `Generator.Completed`가 이미 public C# 이벤트로 노출되어 있어 새 통로가 필요 없다.
3. 튜토리얼 연출 배선을 발전기 시스템의 마커가 들고 있게 되어 "훅만 노출하고 상대가 누구인지 알지 않는다"는 경계 원칙과 어긋난다.
4. 받는 쪽에 두면 된다. `TutorialController`가 `[SerializeField] Generator`를 잡고 `Completed`를 구독하는 방식이 `GeneratorSound`·`GeneratorAIConnector`와 같은 기존 패턴이다.

**구현 시 필요한 작업.**

1. `GeneratorCandidatePoint`에 `_isManagedByDirector` 추가
2. `CollectAvailableCandidates`에서 비관리 후보 제외
3. `ApplyActivation`이 관리 대상만 `SetActive` (비관리는 배치 그대로 둔다)
4. **완료 집계를 `Completed`에서 `RepairStopped`로 교체한다.** `Completed`는 인자 없는 `Action`이라 어느 발전기인지 알 수 없어 플래그를 확인할 수 없다. `RepairStopped`는 `Action<Generator>`이고 `Complete()`가 `State = COMPLETED`를 세운 직후 발행하므로 식별과 판정이 가능하다. `GeneratorHighlightController`가 같은 이유로 이미 같은 패턴을 쓴다.
5. `ReportUnmanagedGenerators`는 플래그로 뺀 발전기를 세지 않고 마커가 없는 것만 센다. 그러면 진짜 실수만 남으므로 `Debug.Log`를 `LogWarning`으로 올릴 수 있다.

이 방식이 도입되면 프리팹 배리언트나 인스턴스 단위 컴포넌트 제거는 필요 없다.

### 5.2 Zone 분산 2차 패스 균등화

현재 2차 패스는 남은 후보를 앞에서부터 채우며 Zone을 다시 고려하지 않는다. 필요 5 / Zone 3 / 후보가 `A×4, B×1, C×1`이면 `A 3 + B 1 + C 1`이 될 수 있고 `2+2+1`처럼 고르게 퍼지지 않는다.

1차 패스를 "필요 수를 채울 때까지 Zone 집합을 비우고 반복"하는 라운드로빈으로 바꾸면 해결된다. 현재 후보 수(6)와 필요 개수(3)에서는 1차에서 끝나 차이가 없어 보류했다.

### 5.3 GDD 7.1의 나머지 규칙

"시작 지점과 지나치게 가까운 후보는 제외한다", "탈출 지점과 인접한 후보의 수를 제한한다" 두 규칙은 시작·탈출 지점 시스템이 없어 구현하지 않았다. 판정 근거는 `GeneratorCandidatePoint`에 추가한다.

## 6. 확인된 문제와 위험 요소

### P1: `GeneratorDirector` 중복 배치

씬에 둘 이상 있으면 완료가 중복 집계되어 필요 개수보다 적게 고쳐도 클리어된다. `GeneratorSystem` 프리팹에 이 컴포넌트가 들어가면서 낱개로 놓인 것과 겹칠 위험이 생겼다. `[DisallowMultipleComponent]`는 같은 오브젝트만 막으므로 `Awake`에서 별도로 확인해 `LogError`를 남긴다.

### P1: 씬 미통합

발전기 시스템이 실제 게임 씬에 올라가 있지 않다. 기존 테스트 씬 두 개(`AI_Prototype`, `PlayerTestScene`)는 폐기 예정이며 이 브랜치에서 씬은 커밋하지 않았다.

### P2: `RequireComponent`의 자동 추가

부속 컴포넌트를 발전기 자식 오브젝트에 잘못 붙이면 그 자식에 `Generator`가 자동으로 하나 더 생긴다. `[DisallowMultipleComponent]`는 같은 오브젝트만 막으므로 걸러지지 않는다. `ReportUnmanagedGenerators`가 잡아내지만 로그를 읽어야 한다.

### 타 담당: 레거시 Input

`AIDebugPresenterHost`가 레거시 `Input.GetKeyDown`을 쓰고 있어 현재 주석 처리된 채 방치되어 있다. Active Input Handling이 Input System Package라 호출 시 예외가 발생한다. `Keyboard.current`로 교체가 필요하며 현민 담당이다.
