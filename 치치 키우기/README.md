# AI 감정 케어 채팅 시스템

> Unity 기반 힐링 게임의 AI 대화 시스템. GPT-4o를 활용해 사용자의 감정을 분석하고, 캐릭터 페르소나를 유지한 공감 응답을 실시간으로 제공한다.

---

## 프로젝트 개요

| 항목 | 내용 |
|------|------|
| 장르 | 감정 케어 / 힐링 게임 |
| 엔진 | Unity (C#) |
| AI 모델 | GPT-4o (`gpt-4o`) |
| AI 캐릭터 | 치치 (Chichi) |
| 주요 기능 | 감정 분석, 공감 응답 생성, 조언 태그 추출 |

---

## 시스템 아키텍처

```
[UIChat]                  ← View / Input 처리
    ↓
[OpenAIManager]           ← Controller / Singleton 진입점
    ↓
[ChatService]             ← API 통신 및 응답 파싱
    ↓
[OpenAI GPT-4o]           ← LLM 응답 생성
    ↓
[ResponseContents]        ← 구조화된 결과 반환
  .Emotion / .Response / .AdviceTag
```

### 계층별 역할

- **View Layer** (`UIChat`, `ChatMessageBox`) — 사용자 입력 수신, 말풍선 UI 렌더링
- **Controller Layer** (`OpenAIManager`) — 싱글턴으로 외부 접근 단일화, 감정 코드 및 조언 태그 후처리 위임
- **Service Layer** (`ChatService`) — API 요청 구성, 응답 전처리 및 JSON 파싱
- **Data Layer** (`SystemDataManager`, `UserDataManager`) — 시스템 데이터 및 유저 데이터 분리 관리

---

## 주요 기술 구현

### 1. Structured Output 강제 및 응답 전처리

GPT-4o가 항상 파싱 가능한 JSON을 반환하도록 시스템 프롬프트에서 출력 형식을 명시적으로 제약한다.
GPT가 간헐적으로 삽입하는 마크다운 코드블록(` ```json `)을 `IndexOf` / `Substring`으로 제거한 뒤 파싱하여, 실환경에서 발생하는 LLM 응답 불규칙성에 대응한다.

```csharp
if (content.StartsWith("```"))
{
    int first = content.IndexOf("```", StringComparison.Ordinal);
    int last  = content.LastIndexOf("```", StringComparison.Ordinal);
    if (first != -1 && last != -1 && last > first)
        content = content.Substring(first + 3, last - first - 3);
}
content = content.Replace("json", "", StringComparison.OrdinalIgnoreCase).Trim();
```

### 2. 예외 이중 처리로 안정성 확보

API 통신 실패와 JSON 파싱 실패를 독립된 `try-catch` 블록으로 분리하여, 어느 단계에서 오류가 발생하더라도 게임 전체가 중단되지 않도록 설계했다.

```csharp
// API 통신 레벨
catch (Exception e) { Debug.LogError("Chat 요청 실패: " + e.Message); }

// JSON 파싱 레벨
catch (Exception parseEx) { Debug.LogError("JSON 파싱 실패: " + parseEx.Message); }
```

### 3. Partial Class를 활용한 관심사 분리

`OpenAIManager`와 `ChatService`를 `partial class`로 파일 분리하여 각 클래스의 책임을 명확히 했다. `OpenAIManager`는 외부 인터페이스와 싱글턴 생명주기를 담당하고, `ChatService`는 API 통신 로직에만 집중한다.

### 4. 타입 안전한 제네릭 데이터 접근 레이어

`SystemDataManager`와 `UserDataManager`는 제네릭 메서드로 타입별 데이터를 안전하게 조회한다. 새로운 데이터 타입이 추가되어도 매니저 코드 수정 없이 확장 가능하다.

```csharp
// 전체 조회
List<TestMissionData> missions = _systemDataM.GetAll<TestMissionData>(gameObject);

// ID 기반 단건 조회
TestMissionData first = _systemDataM.GetById<TestMissionData>(gameObject, 101);
```

### 5. 인터페이스 기반 권한 분리

데이터 접근 권한을 `ISystemDataReader`(읽기)와 `IUserDataUpdater`(쓰기)로 인터페이스를 분리하여, 각 컴포넌트가 필요한 최소 권한만 보유하도록 설계했다.

```csharp
// 읽기 전용 컴포넌트
public class SystemDataTest : MonoBehaviour, ISystemDataReader { ... }

// 읽기 + 쓰기 컴포넌트
public class UserDataTest : MonoBehaviour, ISystemDataReader, IUserDataUpdater { ... }
```

### 6. 동적 말풍선 레이아웃

`ChatMessageBox`는 텍스트 길이에 따라 말풍선 높이를 런타임에 자동 조정한다. `LayoutRebuilder.ForceRebuildLayoutImmediate()`로 TextMeshPro 레이아웃을 강제 갱신한 뒤 `sizeDelta`를 재계산하여, 짧은 메시지와 긴 메시지 모두 정확한 UI를 유지한다.

---

## 파일 구조

```
├── AI/
│   ├── OpenAIManager.cs       # Singleton 진입점, 후처리 훅
│   └── ChatService.cs         # API 통신, JSON 파싱 (partial class)
│
├── UI/
│   ├── UIChat.cs              # 채팅 화면 View, 입력 이벤트 처리
│   └── ChatMessageBox.cs      # 말풍선 컴포넌트, 동적 크기 조정
│
└── Data/
    ├── SystemDataTest.cs      # 시스템 데이터 조회 테스트
    └── UserDataTest.cs        # 유저 데이터 읽기/쓰기 테스트
```

---
