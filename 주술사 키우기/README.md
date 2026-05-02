# 확장 가능한 퀘스트/리워드 시스템

> 다양한 보상 타입을 하나의 구조로 통합한 데이터 기반 보상 아키텍처

---

## 개요

게임 내 퀘스트, 던전 클리어, 출석 보상 등 다양한 상황에서 발생하는 **재화, 장비, 스킬, 스탯** 등 이질적인 보상 데이터를 단일 인터페이스로 처리하는 시스템입니다.

---

## 문제 정의

초기에 각 보상 타입을 개별 로직으로 구현할 경우 다음과 같은 문제가 발생합니다.

```
// 기존 방식: 분기문 증가로 인한 유지보수 악화
if (type == RewardType.Currency)   { ... }
else if (type == RewardType.Equipment) { ... }
else if (type == RewardType.Skill) { ... }
// 새로운 타입 추가 시 여기에 계속 추가...
```

| 문제 | 원인 |
|------|------|
| **코드 중복** | 각 타입마다 유사한 지급/슬롯 생성 로직 반복 |
| **확장 어려움** | 신규 보상 타입 추가 시 여러 파일 동시 수정 필요 |
| **테스트 복잡도 증가** | 분기마다 별도 테스트 케이스 작성 필요 |

---

## 설계

### 핵심 원칙

- **공통 인터페이스 기반 구조** — `RewardBaseSO` 추상 클래스로 모든 보상 타입의 계약 정의
- **데이터 기반 분리** — `ScriptableObject`를 활용해 보상 로직을 데이터 에셋 단위로 분리
- **느슨한 결합** — `RewardManager`는 구체적인 보상 타입을 알지 못하며, 인터페이스를 통해서만 동작

### 클래스 구조

```
RewardBaseSO (abstract)
│   ├── GiveReward(BigInteger amount, string title)
│   ├── AddSlot(string title)
│   └── GetRewardSlot(Reward reward)
│
├── CurrencyRewardSO     — 재화 지급 (Gold, Gem 등)
├── StatRewardBaseSO     — 플레이어 스탯 수치 증가
├── EquipmentRewardSO    — 장비 소환 (랜덤 타입/등급)
└── SkillRewardSO        — 스킬 소환 (랜덤 타입/등급)
```

### 시스템 흐름

```
GiveReward 호출
    │
    ▼
RewardManager
    │── RewardType 기반으로 RewardBaseSO 에셋 조회 (캐싱)
    │── Resources.Load로 동적 로딩
    │
    ▼
RewardBaseSO.GiveReward(amount, title)
    │── 각 구현체가 자신의 로직 실행 (재화 지급, 스탯 증가, 소환 등)
    │── AddSlot() → UI에 결과 슬롯 등록
    │
    ▼
UI_Rewards.ShowUI()
```

---

## 구현 포인트

### 1. 추상 기반 클래스 — `RewardBaseSO`

```csharp
public abstract class RewardBaseSO : ScriptableObject
{
    public abstract void GiveReward(BigInteger amount, string title);
    public abstract void AddSlot(string title);
    public abstract RewardSlot GetRewardSlot(Reward reward);
}
```

모든 보상 구현체는 이 세 가지 메서드를 반드시 구현합니다.  
`RewardManager`는 `RewardBaseSO` 타입만 알고 있어 **OCP(개방-폐쇄 원칙)** 을 준수합니다.

---

### 2. 다형성 기반 보상 처리 — `RewardManager`

```csharp
public void GiveReward(List<Reward> rewards, string title)
{
    foreach (Reward reward in rewards)
    {
        dataDic.TryGetValue(type, out RewardBaseSO data);
        if (!data)
        {
            // 타입 이름 기반 동적 로딩 — 새 타입 추가 시 이 코드 수정 불필요
            data = Resources.Load<RewardBaseSO>($"ScriptableObjects/RewardDataSO/{type}RewardData");
            dataDic[type] = data;
        }

        data.GiveReward(amount, title); // 구체 타입 분기 없이 호출
    }
}
```

**`Dictionary<RewardType, RewardBaseSO>` 캐싱**으로 반복 호출 시 `Resources.Load` 비용을 제거합니다.

---

### 3. 구현 예시 — `EquipmentRewardSO`

```csharp
public override void GiveReward(BigInteger amount, string title)
{
    this.amount = BigInteger.ToInt32(amount);
    SummonEquipment(this.amount, title);
}

private void SummonEquipment(int quantity, string title)
{
    for (int i = 0; i < quantity; i++)
    {
        // 랜덤 장비 타입/종류 결정
        Enums.EquipType equipType = Enums.equipTypes[GetRandomInt(Enums.equipTypes.Length)];
        Enums.EquipmentType type = equipmentTypeData[GetRandomInt(equipmentTypeData.Length)];

        // 중복 제거 후 일괄 업데이트 (HashSet + Dictionary 활용)
        summonedItems.Add(equipment);
        if (summonedCounts.ContainsKey(equipment)) summonedCounts[equipment]++;
        else summonedCounts[equipment] = 1;
    }

    foreach (EquipmentData equipment in summonedItems)
        EquipmentManager.Instance.UpdateEquipmentCount(equipment, summonedCounts[equipment]);
}
```

`HashSet<EquipmentData>`와 `Dictionary<EquipmentData, int>`를 조합해 동일 아이템이 여러 개 소환될 때 **UpdateEquipmentCount 호출을 1회로 최소화**합니다.

---

### 4. 보상 슬롯 UI 분리 — `GetRewardSlot`

```csharp
// StatRewardBaseSO
public override RewardSlot GetRewardSlot(Reward reward)
{
    StatRewardSlot slotPrefab = Resources.Load<StatRewardSlot>("Prefabs/RewardSlots/StatRewardSlot");
    StatRewardSlot slot = Instantiate(slotPrefab);
    slot.SetUI(dataType, reward.amount.ChangeMoney());
    return slot;
}
```

각 보상 타입이 자신에게 맞는 UI 슬롯 프리팹을 직접 생성·반환합니다.  
UI 레이어는 보상 타입을 몰라도 `RewardSlot` 기반 클래스로 동일하게 처리합니다.

---

## 트러블슈팅

### 보상 타입 증가 시 분기문 폭발 문제

**문제 상황**

```
리워드 타입: 4종 → 10종 → 20종...
분기문: if-else if 체인이 계속 길어짐
신규 타입 추가 시: RewardManager, UI 코드, 기타 참조처 모두 수정 필요
```

**해결 방법 — 다형성 기반 리팩토링**

| Before | After |
|--------|-------|
| `RewardManager`가 각 타입 처리 방법을 직접 알고 있음 | `RewardManager`는 `GiveReward()` 인터페이스만 호출 |
| 타입 추가 시 `RewardManager` 수정 필요 | `RewardBaseSO`를 상속한 새 클래스 + 에셋 생성으로 완결 |
| 단위 테스트 어려움 | 각 구현체를 독립적으로 테스트 가능 |

**타입 기반 동적 에셋 로딩**으로 `Resources.Load` 경로를 컨벤션화해 새 타입 추가 시 매니저 코드 수정 없이 에셋만 추가하면 됩니다.

```
Resources/ScriptableObjects/RewardDataSO/
├── CurrencyRewardData.asset
├── EquipmentRewardData.asset
├── SkillRewardData.asset
├── StatRewardData.asset
└── [NewType]RewardData.asset  ← 파일만 추가하면 자동 동작
```

---

## 결과

| 항목 | 개선 내용 |
|------|----------|
| **확장성** | 신규 보상 타입 추가 시 기존 코드 수정 없이 에셋 단위 확장 가능 |
| **유지보수** | 각 보상 로직이 독립 클래스에 캡슐화되어 변경 영향 범위 최소화 |
| **재사용성** | `GetRewardSlot()`을 통해 퀘스트 미리보기, 보상 확인 등 다양한 UI 맥락에서 재사용 |
| **성능** | Dictionary 캐싱으로 `Resources.Load` 반복 호출 제거, 소환 시 일괄 업데이트로 Manager 호출 최소화 |

---

## 파일 구조

```
Scripts/
├── Managers/
│   └── RewardManager.cs          — 보상 요청 진입점, 캐싱, UI 연결
└── ScriptableObjects/
    ├── RewardBaseSO.cs            — 추상 기반 클래스 (인터페이스 계약)
    ├── CurrencyRewardSO.cs        — 재화 보상
    ├── StatRewardBaseSO.cs        — 스탯 보상 (int/BigInteger 지원)
    ├── EquipmentRewardSO.cs       — 장비 소환 (등급/랜덤 타입)
    └── SkillRewardSO.cs           — 스킬 소환 (등급/랜덤 타입)
```
