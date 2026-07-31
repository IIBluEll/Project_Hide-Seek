# 에일리언 아이솔레이션형 추격 AI 포트폴리오 기획서

> PROJECT ISOLATION AI 시스템 설계 문서  
> GDD 연동 기준: v0.3  
> 갱신일: 2026-07-27

## 1. 문서 목적

본 문서는 **Alien: Isolation에서 사용된 AI 설계 철학을 참고하여**, 1년차 Unity 개발자가 포트폴리오로 완성할 수 있는 규모의 추격 AI 시스템을 정의합니다.

원작 내부 시스템을 그대로 복제하는 것이 아니라 다음 경험을 재현하는 것을 목표로 합니다.

> Director AI는 플레이어와 추격 AI가 마주칠 가능성과 전체 긴장도를 조절하고, 추격 AI는 자신의 감각과 기억을 기반으로 플레이어를 직접 사냥합니다.

기존 프로젝트의 코드는 직접 이어서 개발하지 않습니다.

기존 코드는 다음 목적으로만 사용합니다.

- 기존 설계의 장점 확인
- 이전에 발생했던 버그 확인
- 사용했던 수치와 플레이 테스트 결과 참고
- 구현해야 할 기능 목록 추출

신규 Unity 프로젝트에서 시스템을 처음부터 다시 구성합니다.

---

# 2. 프로젝트 목표

## 2.1 핵심 목표

플레이어가 폐쇄된 시설에서 미션을 수행하는 동안 추격 AI가 다음 행동을 수행하도록 구현합니다.

1. 주변을 순찰합니다.
2. 시야와 소음으로 플레이어의 흔적을 감지합니다.
3. 증거가 발생한 위치를 조사합니다.
4. 플레이어를 직접 발견하면 추격합니다.
5. 플레이어를 놓치면 마지막 목격 위치를 기억합니다.
6. 주변을 단계적으로 수색합니다.
7. 증거가 없으면 수색을 포기합니다.
8. Director AI가 전체 긴장도에 따라 추격 AI의 활동과 휴식을 조절합니다.

## 2.2 플레이 경험 목표

플레이어가 잡혔을 때 다음과 같이 느끼도록 설계합니다.

> AI가 내 위치를 무조건 알고 있었던 것이 아니라, 내가 남긴 소음이나 시야 노출 때문에 발각되었다.

반대로 플레이어가 올바르게 증거를 끊으면 추격 AI를 따돌릴 수 있어야 합니다.

---

# 3. 권장 프로젝트 범위

## 3.1 플레이 규모

- 예상 플레이타임: 10~15분
- 맵 규모: 5~7개 Zone
- 추격 AI 수: 1개
- 플레이어 미션: 후보 지점 5개 이상 중 발전기 3개 복구
- 은신처 종류: 책상 아래 1종
- 디코이: 1종
- 주요 소음: 이동, 문, 디코이, 발전기, QTE 실패
- Zone별 Vent 또는 출입구: 1개 이상
- 주요 추격 AI 상태: 6개
- 난이도: 최대 3단계

## 3.2 핵심 플레이 루프

```text
플레이어가 미션을 수행
→ 소음 또는 시야 노출 발생
→ 추격 AI가 증거 조사
→ 플레이어 발견
→ 추격
→ 시야 상실
→ 마지막 목격 위치 수색
→ 증거가 없으면 포기
→ Director가 휴식 또는 재압박 결정
```

---

# 4. 핵심 설계 원칙

## 4.1 Director는 플레이어를 사냥하지 않습니다

Director AI는 다음 항목만 관리합니다.

- 전체 긴장도
- 추격 AI의 활동 여부
- 추격 AI가 활동할 대략적인 Zone
- 조사 강도
- 휴식 구간

Director가 추격 AI의 상태를 직접 강제로 변경해서는 안 됩니다.

```csharp
// 비권장
_chaseAIController.ChangeState(CHASE_STATE.CHASE);
```

Director는 힌트만 전달합니다.

```csharp
// 권장
_chaseAIController.ReceiveDirectorHint(directorHint);
```

최종 행동은 추격 AI가 자신의 감각과 기억을 기준으로 결정합니다.

## 4.2 추격 AI가 실제 사냥의 주체입니다

추격 AI는 다음 행동을 직접 판단합니다.

- 플레이어가 보이는지 판단
- 소음이 들리는지 판단
- 어떤 증거를 우선할지 결정
- 플레이어 추격
- 마지막 목격 위치 이동
- 주변 수색
- 수색 종료
- Director 힌트 수용 여부 결정

## 4.3 정확한 플레이어 위치를 공유하지 않습니다

Director가 다음 값을 직접 전달해서는 안 됩니다.

```csharp
SearchAnchorPosition = playerTransform.position;
```

Director는 플레이어의 정확한 현재 좌표가 아닌 다음 정보만 전달합니다.

- 플레이어가 있을 가능성이 높은 Zone
- Zone 내부의 부정확한 조사 지점
- 조사 반경
- 조사 강도
- 힌트 유효 시간

## 4.4 직접 감각 정보가 가장 높은 우선순위를 가집니다

추격 AI의 정보 우선순위는 다음과 같습니다.

```text
1. 현재 직접 보고 있는 플레이어
2. 현재 직접 들은 소음
3. 최근 마지막 목격 위치
4. 최근 마지막 청각 위치
5. Director 힌트
6. 기본 순찰 위치
```

Director 힌트는 추격 AI가 직접적인 증거를 가지고 있지 않을 때만 사용합니다.

---

# 5. 전체 시스템 구조

```text
MasterAIProvider
├─ MasterAIDirector
├─ MasterAIGauge
├─ ZoneSelector
└─ MasterAIConfig

ChaseAIController
├─ ChaseAIMovement
├─ ChaseAIPerception
├─ ChaseAIMemory
├─ ChaseAIStateMachine
├─ ChaseAISearch
├─ ChaseAIAnger
└─ ChaseAIDebugView
```

---

# 6. 클래스별 책임

## 6.1 `MasterAIProvider`

Unity 컴포넌트와 AI 로직을 연결하는 진입점입니다.

### 담당 기능

- 플레이어 참조 보관
- 추격 AI 참조 보관
- Zone 목록 보관
- 설정 데이터 보관
- Director 초기화
- 시스템 생명주기 관리

### 담당하지 않는 기능

- 추격 AI 상태 직접 변경
- 시야 또는 소음 판정
- NavMesh 목적지 계산
- 수색 포인트 생성

`MasterAIProvider`는 시스템을 조립하고 연결하는 역할만 수행합니다.

---

## 6.2 `MasterAIDirector`

게임 전체 페이싱을 담당합니다.

### 담당 기능

- 추격 AI 활동 시작
- 추격 AI 퇴근 요청
- `GlobalStress` 관리
- 대략적인 활동 Zone 선택
- Director 힌트 생성
- 플레이어 휴식 구간 제공

### 핵심 질문

> 현재 플레이어의 압박 수준을 기준으로 추격 AI가 어느 구역에서 얼마나 적극적으로 활동해야 하는가?

---

## 6.3 `MasterAIGauge`

Director가 사용하는 게이지를 관리합니다.

### 초기 구현 게이지

- `GlobalStress`

필요한 경우 추후 다음 수치를 추가할 수 있습니다.

- `AreaAlert`

Director 게이지는 초기 MVP에서 `GlobalStress`만 구현합니다. `Anger`는 Director 게이지에 포함하지 않고 Chase AI 내부 공격성 시스템에서 별도로 관리합니다.

---

## 6.4 `ZoneSelector`

Director가 추격 AI에게 전달할 Zone과 조사 기준점을 선택합니다.

### 담당 기능

- 플레이어가 위치한 Zone 확인
- 인접 Zone 확인
- 가중치 기반 Target Zone 선택
- Zone 내부 조사 기준점 선택
- 유효한 NavMesh 위치 확인

---

## 6.5 `ChaseAIController`

추격 AI 내부 구성 요소를 연결합니다.

### 담당 기능

- Perception 결과 수신
- Memory 갱신
- StateMachine 실행
- Movement에 이동 명령 전달
- Director 힌트 수신
- 상태 변경 이벤트 전달

### 담당하지 않는 기능

- 직접적인 Raycast 처리
- NavMesh 위치 계산
- 수색 포인트 생성
- Director 페이싱 판단

---

## 6.6 `ChaseAIMovement`

NavMesh 이동만 담당합니다.

### 담당 기능

- 목적지 설정
- 이동 중단
- 이동 속도 설정
- 목적지 도착 여부 확인
- NavMesh 유효 좌표 보정
- 경로 실패 확인

### 핵심 원칙

`ChaseAIMovement`는 상태를 변경하지 않습니다.

이동 결과를 반환하거나 이벤트로 전달하고, 상태 전환은 `ChaseAIStateMachine`이 판단합니다.

---

## 6.7 `ChaseAIPerception`

현재 시점의 감각 정보만 계산합니다.

### 시야 감지

- 감지 거리
- 시야각
- 장애물 Raycast
- 플레이어 노출 시간
- 감지 누적 시간

### 소음 감지

- 소음 위치
- 소음 반경
- 소음 강도
- 소음 종류
- AI와 소음 사이의 거리

### 핵심 원칙

`ChaseAIPerception`는 과거 정보를 저장하지 않습니다.

현재 플레이어가 보이는지, 현재 소음이 들리는지만 판단합니다.

---

## 6.8 `ChaseAIMemory`

과거에 감지한 증거를 저장합니다.

### 저장 정보

- 마지막 목격 위치
- 마지막 목격 시간
- 마지막 청각 위치
- 마지막 청각 시간
- 증거 유효 여부
- 마지막으로 확인한 플레이어 이동 방향

### 예시 구조

```csharp
public sealed class ChaseAIMemory
{
    public Vector3 LastSeenPosition { get; private set; }
    public Vector3 LastHeardPosition { get; private set; }

    public float LastSeenTime { get; private set; }
    public float LastHeardTime { get; private set; }

    public bool HasVisualEvidence { get; private set; }
    public bool HasSoundEvidence { get; private set; }
}
```

### 필요한 이유

Memory가 없으면 다음 문제가 발생합니다.

- 플레이어를 놓치는 순간 AI가 즉시 순찰로 복귀함
- AI가 플레이어의 현재 위치를 계속 참조하여 치팅처럼 보임
- 마지막 목격 위치 기반 수색을 구현하기 어려움

---

## 6.9 `ChaseAIStateMachine`

추격 AI의 현재 상태와 상태 전환을 관리합니다.

### 초기 상태

```csharp
public enum CHASE_STATE
{
    DORMANT,
    PATROL,
    INVESTIGATE,
    CHASE,
    SEARCH,
    RETREAT
}
```

`ChaseAIStateMachine`은 추격 중 들어온 퇴근 요청을 `RetreatPending`으로 보관합니다. 이 값은 직접 시야가 끊긴 뒤 수색 종료 또는 안전한 이탈 조건에서만 `RETREAT` 전환에 사용합니다.

---

## 6.10 `ChaseAISearch`

플레이어를 놓친 뒤 수행할 수색 행동을 관리합니다.

### 담당 기능

- 마지막 목격 위치 확인
- 플레이어 이동 방향 앞쪽 조사
- 주변 NavMesh 포인트 생성
- 수색 포인트 순차 방문
- 유효한 시각·청각 증거가 있는 가까운 은신처 후보 확인
- 수색 종료 조건 판단

---

## 6.11 `ChaseAIAnger`

발전기 진행에 따른 Chase AI의 장기적인 공격성을 관리합니다.

### 담당 기능

- 현재 `Anger`와 `AngerFloor` 보관
- 발전기 완료 수에 맞는 하한선 적용
- `Anger` 행동 단계 계산
- 설정된 이동 속도 또는 수색 지속 시간 보정

### 담당하지 않는 기능

- Director의 출근·퇴근 판단
- `GlobalStress` 증감
- 증거가 없는 은신처를 조사 후보로 추가하는 판단

---

## 6.12 `ChaseAIDebugView`

AI의 판단 근거를 화면과 Scene View에 표시합니다.

### 런타임 HUD

- Director Phase
- Global Stress
- Target Zone
- Director Hint
- Chase State
- Anger / Anger Floor / Anger Phase
- Retreat Pending
- 마지막 증거
- 마지막 목격 시간
- 현재 목적지
- 수색 순서
- NavMesh 경로 유효 여부

### Scene View Gizmo

- 시야각
- 감지 거리
- 마지막 목격 위치
- 마지막 소음 위치
- Director Anchor
- 현재 이동 목적지
- 수색 반경
- 수색 포인트
- 실제 플레이어 위치

---

# 7. Director 힌트 설계

## 7.1 힌트 데이터

```csharp
public readonly struct DirectorHint
{
    public readonly int TargetZoneId;
    public readonly Vector3 SearchAnchorPosition;
    public readonly float SearchRadius;
    public readonly float Urgency;
    public readonly float ExpireTime;

    public DirectorHint(
        int targetZoneId,
        Vector3 searchAnchorPosition,
        float searchRadius,
        float urgency,
        float expireTime)
    {
        TargetZoneId = targetZoneId;
        SearchAnchorPosition = searchAnchorPosition;
        SearchRadius = searchRadius;
        Urgency = urgency;
        ExpireTime = expireTime;
    }
}
```

## 7.2 데이터 의미

| 데이터 | 설명 |
|---|---|
| `TargetZoneId` | 우선적으로 조사할 Zone |
| `SearchAnchorPosition` | Zone 내부의 대략적인 조사 시작점 |
| `SearchRadius` | 조사할 주변 범위 |
| `Urgency` | 이동 및 수색 강도 |
| `ExpireTime` | 힌트가 유효한 시간 |

## 7.3 조사 기준점 후보

`SearchAnchorPosition`은 다음 후보 중 하나를 선택합니다.

- Zone 중심
- Zone 내부의 랜덤 NavMesh 위치
- 주요 통로
- Zone 입구
- 미션 오브젝트 근처
- Vent 근처
- 플레이어가 이전에 자주 이동한 통로

플레이어의 현재 좌표는 사용하지 않습니다.

---

# 8. Director Zone 선택 규칙

Director가 항상 플레이어의 현재 Zone을 전달하면 추격 AI가 지나치게 정확해집니다.

따라서 `GlobalStress`에 따라 확률적으로 Zone을 선택합니다.

## 초기 예시 수치

| Global Stress | 플레이어 Zone | 인접 Zone | 다른 Zone |
|---:|---:|---:|---:|
| 낮음 | 30% | 50% | 20% |
| 중간 | 50% | 40% | 10% |
| 높음 | 70% | 30% | 0% |

이 값은 고정된 정답이 아니며 플레이 테스트로 조정합니다.

## 낮은 Stress

- 인접 Zone을 중심으로 조사
- 플레이어와 직접 조우할 가능성을 낮게 유지
- 발소리나 그림자 등 긴장 예고 역할

## 중간 Stress

- 플레이어 Zone과 인접 Zone을 균형 있게 선택
- 직접 조우 가능성 증가

## 높은 Stress

- 플레이어 Zone을 우선 선택
- 단, 정확한 위치는 전달하지 않음
- 플레이어 근처에서 적극적으로 활동

---

# 9. Director 힌트 갱신 규칙

Director 힌트는 매 프레임 갱신하지 않습니다.

```text
힌트 생성
→ 일정 시간 고정
→ 플레이어가 이동해도 기존 힌트 유지
→ 힌트 만료
→ 다음 힌트 계산
```

플레이어가 힌트 생성 직후 다른 Zone으로 이동했다면 추격 AI는 빈 장소를 조사할 수 있습니다.

이는 오류가 아니라 공정성을 위한 의도적인 정보 지연입니다.

---

# 10. 추격 AI 상태 정의

## 10.1 `DORMANT`

추격 AI가 Vent 또는 맵 외부에서 대기하는 상태입니다.

### 진입 조건

- 게임 시작 전
- Director가 휴식 구간 결정
- 퇴근 완료

### 전환

```text
Director 활동 요청
→ PATROL 또는 INVESTIGATE
```

---

## 10.2 `PATROL`

증거가 없을 때 주변을 순찰하는 상태입니다.

### 행동

- 현재 Zone 순찰
- Director 힌트가 있으면 대상 Zone 이동
- 시야와 소음 지속 감지

### 전환

```text
플레이어 발견 → CHASE
소음 감지 → INVESTIGATE
안전한 퇴근 요청 → RETREAT
```

---

## 10.3 `INVESTIGATE`

하나의 증거 위치를 조사하는 상태입니다.

### 조사 대상

- 소음 위치
- Director Anchor
- 의심 위치

### 행동

- 조사 위치까지 이동
- 도착 후 주변 확인
- 플레이어 또는 추가 증거 탐지

### 전환

```text
플레이어 발견 → CHASE
새로운 증거 발견 → INVESTIGATE 갱신
증거 없음 → SEARCH
안전한 퇴근 요청 → RETREAT
```

---

## 10.4 `CHASE`

플레이어를 직접 보고 추격하는 상태입니다.

### 행동

- 플레이어의 현재 위치로 이동
- 일정 주기로 목적지 갱신
- 공격 거리 확인
- 시야 유지 여부 확인

### 전환

```text
플레이어 공격 범위 진입 → 공격
시야 상실 → 마지막 목격 위치 이동
```

### 중요 규칙

추격 중에는 Director 힌트를 무시합니다.

Director의 퇴근 요청은 즉시 실행하지 않고 `RetreatPending`으로 예약합니다.

---

## 10.5 `SEARCH`

플레이어를 놓친 뒤 주변을 수색하는 상태입니다.

### 기본 수색 순서

```text
1. 마지막 목격 위치 이동
2. 플레이어의 마지막 이동 방향 앞쪽 조사
3. 주변 NavMesh 포인트 조사
4. 유효한 증거가 있는 가까운 은신처 후보 확인
5. 증거가 없으면 수색 종료
```

### 초기 구현 범위

- 수색 포인트 3개
- 직전 방문 지점 제외
- NavMesh 유효 위치만 사용
- 일정 반경 안에서만 생성
- 새로운 증거가 생기면 현재 수색 중단

### 전환

```text
플레이어 발견 → CHASE
새로운 소음 → INVESTIGATE
수색 완료 + 퇴근 예약 없음 → PATROL
수색 완료 + 퇴근 예약 있음 → RETREAT
```

---

## 10.6 `RETREAT`

Director가 휴식 구간을 만들기 위해 추격 AI를 철수시키는 상태입니다.

### 행동

- 이동 가능한 Vent 선택
- 가능하면 플레이어가 직접 보고 있지 않은 Vent 선택
- Vent까지 이동
- 도착 후 DORMANT 전환

### 추격 중 퇴근 요청

```text
CHASE 중 퇴근 요청
→ RetreatPending 저장
→ 시야 상실
→ SEARCH 수행
→ 수색 종료 또는 안전한 이탈 조건 만족
→ RETREAT
```

플레이어가 지나치게 오랫동안 압박받더라도 플레이어가 직접 보고 있는 상황에서 Chase AI를 즉시 비활성화하지 않습니다. 예외 처리가 필요하면 수색 시간을 단축하되, 시야가 끊긴 상태에서 도달 가능한 Vent로 이동하도록 합니다.

---

# 11. 상태 전환 전체 흐름

```text
DORMANT
→ PATROL
→ INVESTIGATE
→ CHASE
→ SEARCH
→ PATROL
→ RETREAT
→ DORMANT
```

주요 예외 전환은 다음과 같습니다.

```text
PATROL + 플레이어 발견 → CHASE
INVESTIGATE + 플레이어 발견 → CHASE
SEARCH + 플레이어 발견 → CHASE
SEARCH + 새로운 소음 → INVESTIGATE
PATROL + 안전한 퇴근 요청 → RETREAT
INVESTIGATE + 안전한 퇴근 요청 → RETREAT
CHASE + 퇴근 요청 → RetreatPending
SEARCH 완료 + RetreatPending → RETREAT
```

---

# 12. 시야 감지 시스템

## 12.1 감지 조건

플레이어 감지는 다음 조건을 모두 고려합니다.

- 플레이어와 AI 사이의 거리
- AI의 시야각
- 장애물 존재 여부
- 플레이어 노출 시간
- 플레이어 이동 상태
- 조명 상태가 필요한 경우 밝기 보정

## 12.2 누적 감지

플레이어가 한 프레임 보였다고 즉시 추격하지 않습니다.

```text
0.0초 → 감지 없음
0.3초 → 의심
0.8초 → 확정 감지
```

감지 시간은 거리와 플레이어 상태에 따라 조절할 수 있습니다.

예시:

- 가까운 거리: 빠르게 감지
- 먼 거리: 느리게 감지
- 플레이어 달리기: 빠르게 감지
- 플레이어 웅크리기: 느리게 감지

---

# 13. 소음 시스템

## 13.1 이벤트 기반 구조

소음은 AI가 매 프레임 주변 오브젝트를 검색하는 방식보다 이벤트 방식으로 구현합니다.

```csharp
public readonly struct NoiseData
{
    public readonly Vector3 Position;
    public readonly float Radius;
    public readonly float Intensity;
    public readonly NOISE_TYPE NoiseType;

    public NoiseData(
        Vector3 position,
        float radius,
        float intensity,
        NOISE_TYPE noiseType)
    {
        Position = position;
        Radius = radius;
        Intensity = intensity;
        NoiseType = noiseType;
    }
}
```

## 13.2 소음 종류

```csharp
public enum NOISE_TYPE
{
    FOOTSTEP,
    RUNNING,
    DOOR,
    MISSION,
    DECOY,
    IMPACT
}
```

## 13.3 소음 발생 예시

- 걷기
- 달리기
- 문 열기
- 물체 투척
- 발전기 또는 장치 작동
- 미션 실패
- 폭발 또는 충돌

## 13.4 소음 정보의 한계

소음을 들었다고 해서 소음을 발생시킨 주체가 플레이어라는 사실까지 자동으로 알아서는 안 됩니다.

```text
소음 감지
→ 소음 위치 조사
→ 플레이어 직접 발견
→ 추격
```

---

# 14. Memory 및 증거 유효 시간

## 14.1 시각 증거

플레이어를 직접 본 경우 다음 정보를 저장합니다.

- 마지막 목격 위치
- 마지막 목격 시간
- 마지막 이동 방향

시각 증거는 가장 신뢰도가 높은 기억입니다.

## 14.2 청각 증거

소음을 들은 경우 다음 정보를 저장합니다.

- 마지막 소음 위치
- 마지막 소음 시간
- 소음 강도
- 소음 종류

## 14.3 증거 만료

증거는 일정 시간이 지나면 만료되어야 합니다.

예시:

| 증거 | 초기 유효 시간 |
|---|---:|
| 직접 목격 | 10초 |
| 강한 소음 | 8초 |
| 약한 소음 | 4초 |
| Director 힌트 | 5초 |

수치는 초기 테스트 값이며 최종 값은 플레이 테스트로 조정합니다.

---

# 15. 수색 시스템

## 15.1 핵심 목표

추격 AI가 플레이어를 놓친 뒤에도 합리적인 행동을 이어가도록 합니다.

### 단순 AI

```text
시야 상실
→ 즉시 순찰 복귀
```

### 치팅 AI

```text
시야 상실
→ 플레이어 현재 위치 계속 추적
```

### 권장 AI

```text
시야 상실
→ 마지막 목격 위치 이동
→ 마지막 이동 방향 조사
→ 주변 수색
→ 유효한 증거가 있는 은신 가능 위치 확인
→ 증거가 없으면 포기
```

## 15.2 수색 포인트 생성 규칙

- NavMesh 위에 있어야 함
- 도달 불가능한 위치 제외
- 직전에 방문한 지점 제외
- 수색 중심점에서 일정 반경 내 생성
- 서로 지나치게 가까운 지점 제외
- 플레이어 현재 위치는 사용하지 않음
- 벽 반대편의 의미 없는 좌표 제외
- 은신처는 마지막 목격 위치 또는 강한 소음 위치가 충분히 가까울 때만 후보에 포함
- 높은 `Anger`, 높은 `Confidence` 또는 Director 힌트만으로 특정 은신처를 후보에 포함하지 않음

## 15.3 수색 종료 조건

다음 중 하나를 만족하면 수색을 종료합니다.

- 지정한 수색 포인트를 모두 방문
- 최대 수색 시간 초과
- 증거 유효 시간 만료
- 예약된 퇴근 요청이 있고 안전한 이탈 조건 만족
- 새로운 더 높은 우선순위 증거 발생

---

# 16. Global Stress 시스템

## 16.1 의미

`GlobalStress`는 추격 AI의 감정이 아닙니다.

> 플레이어가 최근 얼마나 오랫동안, 얼마나 강하게 압박받았는지를 나타내는 페이싱 수치입니다.

발전기 완료에 따른 장기 난이도 상승에는 `GlobalStress` 하한선을 사용하지 않습니다. 해당 역할은 Chase AI의 `AngerFloor`가 담당하며, `GlobalStress`는 휴식 구간을 만들 수 있도록 계속 감소할 수 있어야 합니다.

## 16.2 증가 조건

- 추격 AI가 플레이어를 직접 추격 중
- 추격 AI가 플레이어와 가까움
- 플레이어가 반복적으로 발각됨
- 플레이어가 은신 상태로 오래 고립됨
- 미션 수행 중 추격 AI가 근처에 있음

## 16.3 감소 조건

- 추격 AI가 DORMANT 상태
- 추격 AI가 플레이어와 멀리 떨어져 있음
- 일정 시간 직접적인 위협이 없음
- 플레이어가 안전한 구간에 진입

## 16.4 활용

```text
Global Stress 낮음
→ 추격 AI 활동 시작 가능

Global Stress 중간
→ 추격 AI 압박 유지

Global Stress 높음
→ 퇴근 준비

Global Stress 최대
→ 추격 AI 퇴근 요청 예약

CHASE 중
→ 즉시 퇴근하지 않고 시야 상실 후 안전한 전환 시점까지 보류

Global Stress 감소
→ 재출근 가능
```

## 16.5 목표 리듬

```text
불안
→ 조우
→ 추격
→ 강한 긴장
→ 탈출
→ 짧은 휴식
→ 다시 불안
```

Director의 목적은 플레이어를 계속 공격하는 것이 아니라 이 긴장 리듬을 유지하는 것입니다.

---

# 17. 추격 AI 공격성과 확장 변수

`Anger`의 하한선과 최소 행동 보정은 발전기 진행에 필요한 MVP 범위에 포함합니다. `Confidence`와 `Boredom`은 기본 추격과 수색이 안정된 이후 추가합니다.

## 17.1 `Confidence`

현재 가진 증거를 얼마나 신뢰하는지 나타냅니다.

### 증가 조건

- 플레이어 직접 목격
- 강한 소음 감지
- 동일한 위치에서 반복 증거 확인
- 추적 중 새로운 증거 발견

### 감소 조건

- 증거가 오래됨
- 빈 위치를 반복 조사
- 수색 실패
- 소음이 미끼로 판명됨

### 활용

| Confidence | 행동 |
|---|---|
| 낮음 | Director 힌트를 적극 수용 |
| 중간 | 자체 증거와 Director 힌트 비교 |
| 높음 | 자체 기억을 우선 |
| 직접 추격 중 | Director 힌트 무시 |

`Confidence`는 플레이어의 위치를 아는 수치가 아니라 현재 추론을 얼마나 신뢰하는지를 나타냅니다.

---

## 17.2 `Anger`

추격 지속성과 공격성을 조절하는 Chase AI 내부 수치입니다. Director의 출근·퇴근 판단에는 사용하지 않습니다.

### 발전기 진행에 따른 하한선

발전기가 완료될 때마다 설정 데이터에 정의된 `AngerFloor`를 적용합니다.

```text
AngerFloor = 발전기 완료 수에 대응하는 설정값
Anger = Max(현재 Anger, AngerFloor)
```

- 발전기가 완료될수록 하한선이 단계적으로 상승합니다.
- `Anger`가 감소하더라도 현재 하한선 아래로는 내려가지 않습니다.
- 하한선이 최고 공격 단계 기준을 자동으로 넘지 않도록 설정합니다.
- `Anger`가 높아도 `GlobalStress` 감소와 Director의 휴식 구간은 유지됩니다.

### 증가 조건

- 플레이어 발견
- 공격 실패
- 플레이어가 도구로 AI를 방해
- 반복적으로 플레이어를 놓침

### 감소 조건

- 일정 시간 플레이어를 발견하지 못함
- 수색을 종료하고 순찰 상태로 복귀함
- 감소하더라도 현재 `AngerFloor` 아래로 내려가지 않음

### 활용

- 이동 속도 소폭 증가
- 수색 지속 시간 증가
- 초기 MVP에서는 위 항목 중 한두 개만 적용
- 시야, 증거 유지 시간과 수색 포인트 수를 동시에 강화하지 않음
- 증거가 없는 은신처를 조사 후보로 추가하는 데 사용하지 않음

---

## 17.3 `Boredom`

한 지역에 지나치게 오래 머무르지 않도록 합니다.

### 증가 조건

- 증거 없이 같은 Zone에 장시간 머무름
- 동일한 수색 행동 반복
- 플레이어를 오랫동안 발견하지 못함

### 활용

- 현재 수색 종료
- 다른 Zone으로 이동
- Director 힌트 재수용
- 순찰 패턴 변경

초기 MVP에서는 `AngerFloor`와 한두 개의 명확한 행동 보정만 구현합니다.

기본 추격과 수색이 안정된 뒤 `Confidence`, 필요하면 `Boredom` 순서로 추가합니다.

---

# 18. 플레이어 미션 시스템과 AI 연결

추격 AI만 존재하고 플레이어가 수행할 일이 없다면 게임이 아니라 AI 테스트 장면에 가깝습니다.

## 18.1 미션 예시

MVP 미션은 발전기 복구 한 종류만 사용합니다.

- 후보 지점 5개 이상 배치
- 한 게임에서 발전기 3개 활성화
- 같은 Zone에 발전기가 몰리지 않도록 제한

## 18.2 미션과 소음

미션 수행 중 다음 상황이 발생하도록 설계합니다.

- 일정 시간 동안 한 위치에 머물러야 함
- 진행 중 소음 발생
- 추격 AI가 가까우면 작업 중단 가능
- 작업 중단 후 짧은 유예 시간이 지나면 진행도가 서서히 감소
- QTE 실패 시 진행도 즉시 감소와 더 큰 소음 발생
- 추격 AI가 퇴근하면 작업 재개 가능

## 18.3 발전기 완료와 Anger 연동

발전기 시스템은 완료된 발전기 수를 이벤트로 전달합니다. `ChaseAIAnger`는 완료 수에 대응하는 `AngerFloor`를 적용합니다.

```text
발전기 완료
→ 완료 수 갱신
→ AngerFloor 갱신
→ 현재 Anger가 하한선보다 낮으면 하한선까지 상승
```

발전기 시스템은 Chase AI의 상태를 직접 변경하지 않으며, `GlobalStress`에도 하한선을 설정하지 않습니다.

## 18.4 목표

플레이어가 다음 판단을 하도록 만듭니다.

- 지금 미션을 계속 수행할 것인가
- 소음을 감수하고 빠르게 끝낼 것인가
- 추격 AI가 지나갈 때까지 숨을 것인가
- 미끼를 사용해 다른 Zone으로 유도할 것인가

---

# 19. 디버그 시스템

## 19.1 런타임 HUD 예시

```text
Director
Phase: ACTIVE
Global Stress: 72 / 100
Target Zone: Engineering
Hint Urgency: 0.65
Hint Remaining: 3.2 sec

Chase AI
State: SEARCH
Current Zone: Medical
Anger: 36 / 100
Anger Floor: 30
Anger Phase: STALK
Retreat Pending: True
Evidence: Last Seen
Last Seen Time: 5.4 sec
Search Point: 2 / 3
Destination Valid: True
```

## 19.2 Scene View 표시

- AI 시야각
- 시야 거리
- 소음 감지 반경
- 마지막 목격 위치
- 마지막 소음 위치
- Director Anchor
- 현재 이동 목적지
- 수색 반경
- 생성된 수색 포인트
- Target Zone
- 실제 플레이어 위치

## 19.3 디버그 로그 예시

```text
[ChaseAI] PATROL → INVESTIGATE
Reason: Noise detected
Position: (12.4, 0.0, -8.2)

[ChaseAI] INVESTIGATE → CHASE
Reason: Player visually confirmed

[ChaseAI] CHASE → SEARCH
Reason: Line of sight lost
LastSeenPosition: (18.1, 0.0, -2.7)

[Director] Retreat requested
Reason: GlobalStress reached maximum

[ChaseAI] Retreat pending
Reason: Direct chase has priority

[ChaseAI] SEARCH → RETREAT
Reason: Search completed with retreat pending
```

---

# 20. 신규 프로젝트 구현 순서

## 20.1 1단계: Graybox 맵

### 구현 항목

- 5개 Zone
- Zone 연결 통로
- Vent 배치
- 순찰 지점
- NavMesh Bake

### 완료 기준

- 모든 Zone 간 이동 가능
- Vent까지 유효한 NavMesh 경로 존재
- 막힌 지역과 이동 가능 지역이 명확함

---

## 20.2 2단계: 이동 시스템

### 구현 항목

- 목적지 설정
- 이동 중단
- 목적지 도착
- NavMesh 유효 위치 보정
- 경로 실패 처리

### 완료 기준

- 10분 이상 순찰해도 멈추지 않음
- 잘못된 목적지가 들어와도 예외가 발생하지 않음
- 도착 이벤트가 중복 발생하지 않음

---

## 20.3 3단계: Perception

### 구현 항목

- 시야 거리
- 시야각
- 장애물 Raycast
- 누적 감지
- 소음 이벤트

### 완료 기준

- 벽 뒤 플레이어를 감지하지 않음
- 시야 밖의 플레이어를 감지하지 않음
- 소음 반경 밖의 소리를 무시함
- 약한 노출은 즉시 추격으로 이어지지 않음

---

## 20.4 4단계: Memory

### 구현 항목

- 마지막 목격 위치
- 마지막 청각 위치
- 증거 발생 시간
- 증거 만료

### 완료 기준

- 시야 상실 후 플레이어 현재 위치를 참조하지 않음
- 마지막 목격 위치까지 이동함
- 오래된 증거가 정상적으로 만료됨

---

## 20.5 5단계: 기본 FSM

### 구현 상태

```text
PATROL
→ INVESTIGATE
→ CHASE
→ SEARCH
→ PATROL
```

### 완료 기준

- Director 없이 추격 AI가 독립적으로 동작함
- 각 상태의 진입과 종료 조건이 명확함
- 상태 고착이 발생하지 않음

---

## 20.6 6단계: 수색 시스템

### 구현 항목

- 마지막 목격 위치 조사
- 수색 포인트 3개
- 새로운 증거 우선 처리
- 수색 종료 조건

### 완료 기준

- 플레이어를 놓친 뒤 즉시 순찰로 돌아가지 않음
- 플레이어 현재 위치를 치팅으로 추적하지 않음
- 수색 후 증거가 없으면 정상적으로 포기함

---

## 20.7 7단계: Director

### 구현 항목

- ACTIVE / DORMANT
- `GlobalStress`
- Zone 힌트
- 힌트 유효 시간
- 출근 및 퇴근
- Vent 이동
- 추격 중 퇴근 요청 예약

### 완료 기준

- Director 없이 동작하던 추격 AI 기능이 깨지지 않음
- Director가 정확한 플레이어 좌표를 전달하지 않음
- 직접 감각 정보가 Director 힌트보다 우선됨
- CHASE 중 퇴근 요청이 즉시 실행되지 않고 안전한 전환 시점까지 예약됨

---

## 20.8 8단계: Anger 기본 구현

### 구현 항목

- 현재 `Anger`
- 발전기 완료 수별 `AngerFloor`
- `Anger` 감소와 하한선 제한
- 이동 속도 또는 수색 지속 시간 중 한두 항목 보정
- 디버그 HUD 표시

`Confidence`와 `Boredom`은 MVP 이후 확장 항목으로 둡니다.

---

## 20.9 9단계: 미션 루프

### 구현 항목

- 후보 발전기 5개 이상 중 3개 활성화
- 수리 중 소음과 QTE 실패 소음
- 작업 중단 후 유예 시간과 진행도 감소
- 재시도와 남은 진행도 반영
- 발전기 완료 이벤트와 `AngerFloor` 갱신
- 모든 발전기 완료 조건

---

## 20.10 10단계: 디버그 및 안정화

### 구현 항목

- 런타임 HUD
- Scene View Gizmo
- 상태 전환 로그
- NavMesh 경로 실패 로그
- Director 결정 로그

---

# 21. 권장 개발 일정

실제 일정은 개인별 개발 시간에 따라 달라질 수 있으며 아래 일정은 추정입니다.

| 주차 | 작업 |
|---|---|
| 1주차 | 신규 프로젝트 설정, Graybox 맵, NavMesh |
| 2주차 | 이동 및 순찰 |
| 3주차 | 시야 및 소음 감지 |
| 4주차 | Memory, 추격, 시야 상실 |
| 5주차 | 단계적 수색 |
| 6주차 | Director, Global Stress, Zone 힌트 |
| 7주차 | Vent, 출퇴근, 디버그 HUD |
| 8주차 | 발전기 미션, Anger 하한선, 연출, 안정화 |
| 9주차 | Anger 밸런스와 선택적 적응 행동 플레이 테스트 |
| 10주차 | 포트폴리오 문서 및 영상 제작 |

---

# 22. 테스트 항목

## 22.1 이동 테스트

- 목적지가 NavMesh 밖일 때 처리
- 경로가 중간에 끊겼을 때 처리
- 문이 닫혀 경로가 변경됐을 때 처리
- Vent까지 경로가 없을 때 처리
- 목적지가 반복 변경될 때 처리

## 22.2 시야 테스트

- 벽 뒤 플레이어
- 시야각 경계에 있는 플레이어
- 가까운 거리와 먼 거리
- 웅크린 플레이어
- 달리는 플레이어
- 짧게 노출된 플레이어

## 22.3 소음 테스트

- AI 바로 근처의 약한 소음
- 먼 거리의 강한 소음
- 벽 너머 소음
- 동시에 발생한 여러 소음
- 미끼 소음
- 미션 장치 소음

## 22.4 수색 테스트

- 마지막 목격 위치에 플레이어가 없는 경우
- 플레이어가 바로 옆 방에 숨은 경우
- 수색 중 새로운 소음이 발생한 경우
- 수색 중 플레이어를 재발견한 경우
- 수색 포인트 생성이 실패한 경우
- 퇴근 요청이 들어온 경우
- 유효한 증거 없이 책상 근처를 수색하는 경우
- `Anger`와 `Confidence`가 높지만 은신처 근거가 없는 경우

## 22.5 Director 테스트

- Global Stress가 최대에 도달한 경우
- 퇴근 후 다시 출근하지 않는 문제
- 플레이어 Zone만 반복 선택하는 문제
- 동일한 Director Hint가 계속 유지되는 문제
- CHASE 중 퇴근 요청이 `RetreatPending`으로 예약되는지 확인
- 시야 상실 후 수색 종료 또는 안전한 조건에서 RETREAT로 전환되는지 확인
- 플레이어가 보고 있는 상태에서 Chase AI가 즉시 비활성화되지 않는지 확인
- DORMANT 상태에서 소음이 발생한 경우

## 22.6 발전기와 Anger 테스트

- 발전기 완료 수에 맞는 `AngerFloor` 적용
- `Anger` 감소 시 하한선 아래로 내려가지 않음
- 발전기 완료가 `GlobalStress` 하한선을 변경하지 않음
- `Anger`가 높아도 Director의 휴식 구간이 발생함
- 수리 중단 후 유예 시간과 진행도 감소가 정상 동작함

---

# 23. 포트폴리오 완료 기준

다음 조건을 만족하면 프로젝트 범위로 충분합니다.

- 추격 AI가 시야와 소음으로 플레이어를 감지함
- 플레이어가 추격 AI를 따돌릴 수 있음
- 시야를 놓치면 마지막 목격 위치를 조사함
- 플레이어 현재 위치를 지속적으로 참조하지 않음
- Director가 정확한 플레이어 좌표를 전달하지 않음
- 직접 감각 정보가 Director 힌트보다 우선됨
- 수색 후 증거가 없으면 포기함
- 근거 없이 특정 은신처를 조사하지 않음
- CHASE 중 퇴근 요청이 예약되고 안전한 시점에 실행됨
- `GlobalStress`가 높아지면 퇴근이 유도되고 휴식 구간이 발생함
- 발전기 완료 수에 따라 `AngerFloor`가 상승함
- `AngerFloor`가 상승해도 `GlobalStress` 감소와 휴식 구간이 유지됨
- 같은 상황에서도 수색 경로가 일부 달라짐
- 30분 이상 상태 고착 없이 동작함
- 디버그 화면으로 판단 근거를 확인할 수 있음
- 3~5분 영상으로 시스템을 설명할 수 있음

---

# 24. 포트폴리오 영상 구성

## 24.1 영상 권장 길이

3~5분

## 24.2 영상 순서

### 1. 시스템 개요

```text
Director AI
→ 전체 긴장도와 활동 지역 관리

Chase AI
→ 감각과 기억으로 플레이어 추적
```

### 2. 시야 및 소음 감지

- 시야각 표시
- 장애물 판정
- 소음 위치 조사

### 3. 추격 및 시야 상실

- 플레이어 발견
- 추격
- 시야 상실
- 마지막 목격 위치 표시

### 4. 수색

- 수색 포인트 생성
- 순차 조사
- 플레이어 재발견 또는 수색 포기

### 5. Director 페이싱

- Global Stress 증가
- 추격 중 퇴근 요청 예약
- 시야 상실 후 안전한 RETREAT 전환
- Vent 이동
- DORMANT
- Stress 감소 후 재출근

### 6. 발전기 진행과 Anger

- 발전기 완료
- Anger Floor 상승
- 후반 수색 또는 이동 보정 변화

### 7. 디버그 화면

- 현재 상태
- 증거 종류
- Director Hint
- 상태 전환 이유

---

# 25. 제외할 기능

현재 포트폴리오 범위에서는 다음 기능을 제외합니다.

- 원작 규모의 대형 Behavior Tree
- 머신러닝 및 강화학습
- 다수 적 AI의 협동
- 복잡한 플레이어 경로 예측
- 절차적 맵 생성
- 수십 종류의 감각 센서
- 범용 AI 프레임워크 제작
- 모든 행동의 난이도별 세분화
- 지나치게 복잡한 은신처 상호작용
- 애니메이션 완성도를 위한 과도한 투자

---

# 26. 최종 구조 요약

```text
Director AI
├─ 플레이어 압박 수준 확인
├─ Global Stress 관리
├─ 추격 AI 활동 여부 결정
├─ 대략적인 Zone과 조사 강도 제공
└─ 휴식과 긴장 리듬 조절

Chase AI
├─ 직접 시야 및 소음 감지
├─ 감지한 증거 저장
├─ 정보 우선순위 판단
├─ 자체 상태 전환
├─ 플레이어 추격
├─ 마지막 위치 수색
├─ 증거가 없으면 포기
└─ 발전기 완료 수에 따른 Anger 하한선 관리
```

---

# 27. 최종 원칙

1. Director는 만남을 유도하지만 플레이어를 직접 사냥하지 않습니다.
2. 추격 AI는 자신의 감각과 기억으로 직접 사냥합니다.
3. 플레이어의 정확한 위치는 직접 감지한 동안에만 사용합니다.
4. 플레이어를 놓치면 마지막 증거를 기반으로 수색합니다.
5. 증거가 없으면 추격 AI는 반드시 포기할 수 있어야 합니다.
6. 높은 `Anger`나 Director 힌트만으로 증거 없는 은신처를 조사해서는 안 됩니다.
7. 추격 중 퇴근 요청은 예약하고 시야 상실 후 안전한 시점에 실행합니다.
8. 발전기 완료는 `AngerFloor`를 높이되 `GlobalStress`의 휴식 기능을 막지 않습니다.
9. 플레이어가 적절하게 행동하면 추격 AI를 따돌릴 수 있어야 합니다.
10. 모든 AI 판단은 디버그 화면으로 설명할 수 있어야 합니다.
11. 기능 개수보다 동작의 일관성과 플레이 경험을 우선합니다.
12. 기존 프로젝트는 코드 복사 대상이 아니라 설계 참고 자료로 사용합니다.
13. 구현 순서는 이동, 감지, 기억, 상태 머신, 수색, Director, Anger, 미션 연동, 디버그 순서로 진행합니다.
