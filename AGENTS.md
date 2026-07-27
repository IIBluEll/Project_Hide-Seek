# Project_Hide-Seek Codex 협업 규칙

이 문서는 이 저장소 전체에 적용되는 Codex 작업 규칙이다. 모든 참여자는 동일한 규칙을 사용한다.

## 1. 기본 원칙

- 사용자 요청과 이 문서를 우선하여 따른다.
- 작업 전 `GAME_DESIGN_DOCUMENT.md`와 요청에 관련된 코드 및 설정을 먼저 확인한다.
- 확인하지 않은 내용을 기존 구현인 것처럼 단정하지 않는다.
- 기존 구조와 충돌하는 요구사항을 발견하면 근거가 되는 파일을 제시하고 사용자에게 확인한다.
- 아래에 정의되지 않은 명명 규칙은 관련 기존 코드의 일관된 스타일을 우선한다. 기존 기준도 없으면 임의로 확정하지 않고 질문한다.

## 2. C# 및 Unity 코드 컨벤션

다음 규칙을 반드시 지킨다.

### 2.1 변수

| 구분 | 규칙 | 예시 |
| --- | --- | --- |
| `protected` 멤버 변수 | `_camelCase` | `_exampleVariable` |
| `private` 멤버 변수 | `_camelCase` | `_exampleVariable` |
| 매개 변수 | `camelCase` | `exampleVariable` |
| 정적 변수 | `s_camelCase` | `s_exampleVariable` |
| 상수 및 `readonly` | `UPPER_SNAKE_CASE` | `EXAMPLE_VARIABLE` |

### 2.2 함수

- 일반 함수는 파스칼 케이스를 사용한다: `ExampleFunction()`
- 코루틴 함수는 `_cor` 접미사를 사용한다: `ExampleFunction_cor()`
- UniTask 반환 함수는 기본적으로 파스칼 케이스를 사용한다: `ExampleFunction()`
- 실제 비동기 실행을 수행하는 UniTask 함수에는 `_async` 접미사를 사용한다: `ExampleFunction_async()`
- 이벤트 바인딩 함수는 `OnExampleActioned()` 형식을 사용한다.

### 2.3 클래스

| 구분 | 규칙 | 예시 |
| --- | --- | --- |
| 일반 클래스 | 파스칼 케이스 | `ExampleClass` |
| 인터페이스 | `I` + 파스칼 케이스 | `IExampleClass` |
| 추상 클래스 | `A` + 파스칼 케이스 | `AExampleClass` |
| 싱글톤 | 기능명 + `Provider` | `ExampleClassProvider` |
| UI Model | 기능명 + `Model_model` | `ExampleModel_model` |
| UI View | 기능명 + `Class_view` | `ExampleClass_view` |
| UI Presenter | 기능명 + `Presenter_presenter` | `ExamplePresenter_presenter` |
| UI 컴포넌트 | 기능 + UI 자체명 | `ExampleButton` |

MVP 명명 규칙은 UI 코드에만 적용한다. 게임플레이 시스템에 불필요하게 MVP 구조나 `_model`, `_view`, `_presenter` 접미사를 적용하지 않는다.

### 2.4 구조체

- 파스칼 케이스를 사용한다: `ExampleStruct`

### 2.5 열거형

- 열거형 타입과 모든 요소에 `UPPER_SNAKE_CASE`를 사용한다.

```csharp
public enum EXAMPLE_ENUM
{
    FIRST_VALUE,
    SECOND_VALUE
}
```

### 2.6 Unity UI 변수 접미사

| 자료형 | 접미사 | 예시 |
| --- | --- | --- |
| `GameObject` | `Obj` | `exampleObj` |
| `Transform` | `Trans` | `exampleTrans` |
| `Text` | `Txt` | `exampleTxt` |
| `Button` | `Btn` | `exampleBtn` |
| `Image` | `Img` | `exampleImg` |
| `InputField` | `Input` | `exampleInput` |
| `Dropdown` | `Drop` | `exampleDrop` |
| `Sprite` | `Sprite` | `exampleSprite` |
| `Animator` | `Animator` | `exampleAnimator` |
| `RectTransform` | `RectTrans` | `exampleRectTrans` |

## 3. UI 아키텍처

UI는 예외 없이 MVP 패턴으로 작성한다. 기준 구현은 프로젝트에 등록된 `com.hm.codebase` 패키지다.

### 3.1 기준 클래스

- namespace: `HM.CodeBase`
- View는 `AView`를 상속한다.
- Presenter는 `APresenter`를 상속한다.
- View는 표시와 사용자 입력 전달만 담당한다.
- Presenter는 View와 Model을 연결하고 UI 흐름을 조정한다.
- Model은 UI에 필요한 상태와 데이터 규칙을 담당하며 Unity UI 컴포넌트를 직접 제어하지 않는다.

### 3.2 수명주기

- View는 `Open()`, `Close()`, 필요 시 `Clear()`를 구현한다.
- Presenter는 `Open()`, `Close()`, `Dispose()`를 구현한다.
- 이벤트를 구독했다면 `Dispose()` 또는 명확한 종료 지점에서 반드시 해제한다.
- `Open()`을 반복 호출해도 이벤트가 중복 구독되지 않도록 한다.
- View의 활성화와 비활성화는 `AView.Open()`과 `AView.Close()`를 기준으로 처리한다.

### 3.3 역할 분리

**Model**

- UI에 표시할 데이터와 상태를 보관한다.
- 데이터 검증과 UI 관련 상태 변경 규칙을 담당한다.
- `Button`, `Text`, `Image`, `GameObject` 등의 View 컴포넌트를 참조하지 않는다.

**View**

- Unity UI 컴포넌트를 참조한다.
- 텍스트, 이미지, 버튼 상태 등 화면 표현을 갱신한다.
- 버튼 입력을 Presenter가 받을 수 있도록 이벤트로 전달한다.
- 게임 규칙, 저장, AI 판단 같은 핵심 로직을 직접 처리하지 않는다.

**Presenter**

- Model의 데이터를 View에 반영한다.
- View 입력을 받아 Model 또는 관련 시스템에 요청한다.
- 화면 열기, 닫기, 데이터 바인딩, 이벤트 구독 해제를 관리한다.
- 가능한 경우 Presenter 간 직접 참조 대신 `EventProvider` 사용을 검토한다.

### 3.4 기본 형태

```csharp
using System;
using HM.CodeBase;

public sealed class ExampleModel_model
{
    public int Value { get; private set; }

    public void SetValue(int value)
    {
        Value = value;
    }
}

public sealed class ExampleClass_view : AView
{
    public event Action ValueChanged;

    public override void Clear()
    {
        base.Clear();
    }

    private void OnExampleActioned()
    {
        ValueChanged?.Invoke();
    }
}

public sealed class ExamplePresenter_presenter : APresenter
{
    private readonly ExampleModel_model EXAMPLE_MODEL;
    private readonly ExampleClass_view EXAMPLE_VIEW;

    public ExamplePresenter_presenter(
        ExampleModel_model exampleModel,
        ExampleClass_view exampleView)
    {
        EXAMPLE_MODEL = exampleModel;
        EXAMPLE_VIEW = exampleView;
    }

    public override void Open()
    {
        EXAMPLE_VIEW.ValueChanged -= OnValueChangedActioned;
        EXAMPLE_VIEW.ValueChanged += OnValueChangedActioned;
        EXAMPLE_VIEW.Open();
    }

    public override void Close()
    {
        EXAMPLE_VIEW.Close();
    }

    public override void Dispose()
    {
        EXAMPLE_VIEW.ValueChanged -= OnValueChangedActioned;
    }

    private void OnValueChangedActioned()
    {
        // Model 갱신 후 View에 결과를 반영한다.
    }
}
```

예시 코드는 구조 설명용이다. 실제 구현에서는 해당 화면의 요구사항과 기존 코드 연결 방식을 먼저 확인한다.

## 4. 답변 및 작업 지침

답변을 제시하기 전에 다음 규칙을 따른다.

1. 잘 모르는 경우에는 `모르겠습니다`라고 명확히 밝힌다.
2. 추측한 내용은 `추측입니다`라고 표시한다.
3. 출처가 불분명한 정보는 `확실하지 않음`으로 표시한다.
4. 근거 없이 단정하지 않는다. 근거가 있다면 관련 파일, 코드, 문서 또는 출처를 함께 제시한다.
5. 요청이 애매하고 결과가 크게 달라질 수 있으면 먼저 맥락이나 상황을 질문한다.
6. 출처나 참고 자료를 사용했다면 핵심 내용만 간단히 요약한다.
7. 항상 존댓말을 사용하고 친절하게 답한다.
8. 장황하게 설명하지 않고 결론과 핵심 근거만 전달한다.
9. 충분히 검토하고 깊이 판단하되, 답변에는 결론과 검증 가능한 근거만 제시한다.
10. 사용자가 제공한 자료와 저장소에서 작업에 직접 관련된 파일을 검토한 후 답한다.
11. 돌려 말하지 않고 사실대로 답한다.
12. 공적이고 전문적인 문체를 사용한다.

## 5. 근거 자료

- 게임 기획 기준: `GAME_DESIGN_DOCUMENT.md`
- 패키지 등록 확인: `Packages/manifest.json`의 `com.hm.codebase`
- HM CodeBase 패키지명: `com.hm.codebase`
- HM CodeBase namespace: `HM.CodeBase`
- MVP 기준 클래스: `AView`, `APresenter`
- HM CodeBase 저장소: `https://github.com/IIBluEll/HM_CodeBase`

HM CodeBase의 실제 API가 변경되면 추측으로 맞추지 말고, 현재 프로젝트에 설치된 패키지 코드를 다시 확인한 뒤 이 문서와 구현을 함께 갱신한다.
