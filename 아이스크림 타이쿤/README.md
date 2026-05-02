# 게임 데이터 시스템 포트폴리오

> Unity 기반 모바일 게임의 데이터 파이프라인 설계 및 구현

---

## 1. 시스템 개요

이 데이터 시스템은 크게 두 가지 데이터 흐름을 분리하여 설계되었습니다.

| 구분 | 클래스 | 설명 |
|------|--------|------|
| **정적 데이터** | `DefaultData` / `DefaultDataManager` | 기획자가 Google Sheets에서 관리하는 밸런스 데이터 (아이템, 맵, 업그레이드 등) |
| **동적 데이터** | `PlayerData` / `PlayerDataManager` | 플레이어의 진행 상황, 보유 아이템 등 런타임 중 변경되는 데이터 |

두 시스템은 역할과 생명주기가 완전히 다르기 때문에 명확히 분리하였고, 각각 독립적으로 확장 가능하도록 구성했습니다.

---

## 2. 전체 아키텍처

```
[Google Sheets]
      │
      │  (에디터 툴 실행)
      ▼
[CsvToJsonConverterEditor]   ← Unity Editor Window
      │  CSV 다운로드 & 파싱
      │  리플렉션으로 타입 매핑
      ▼
[Resources/JSON/*.json]      ← 빌드에 포함되는 정적 에셋
      │
      │  (런타임 Awake)
      ▼
[DefaultDataManager]         ← Singleton, Dictionary<Type, object> 캐싱
      │
      │  GetById<T>(id) / GetAll<T>()
      ▼
[게임 로직 (각 Manager, System)]


[PlayerDataManager]          ← Singleton, Dirty Flag 패턴
      │
      ├─ GetData<T>()         ← 최초 접근 시 JSON 파일 로드 (Lazy Load)
      ├─ Save()               ← Dirty Flag 체크 후 변경된 타입만 저장
      └─ SaveAll()            ← 앱 종료 등 강제 전체 저장
      │
      ▼
[Application.persistentDataPath/PlayerData/*.json]
```

---

## 3. DefaultData 시스템 — 정적 게임 데이터

### 3-1. 데이터 구조 설계

모든 정적 데이터는 `DefaultData` 추상 클래스를 상속합니다.

```csharp
[Serializable]
public abstract class DefaultData
{
    public int id;  // 모든 정적 데이터의 공통 식별자
}
```

id를 공통 인터페이스로 강제함으로써, `DefaultDataManager`가 **타입에 관계없이 동일한 방식으로 데이터를 색인**할 수 있습니다.

실제 데이터 클래스들은 이를 상속하여 각 도메인에 맞게 확장됩니다.

```csharp
// 맵 배경 타일 데이터 — 중첩 구조 포함
[Serializable]
public class MapBackgroundData : MapComponentData
{
    public Enums.TileArea tileArea;
    public HorizontalPointTile horizontalPoint;  // 중첩 클래스
    public VerticalPointTile verticalPoint;       // 중첩 클래스
}

// 업그레이드 데이터
[Serializable]
public class UpgradeData : DefaultData
{
    public int upgradeLevel;
    public Enums.StoreUpgradeType upgradeType;
    public float effectValue;
    public int cost;
}
```

### 3-2. DefaultDataManager — 리플렉션 기반 범용 로더

`DefaultDataManager`는 JSON 파일명을 기준으로 타입을 동적으로 추론하여 로드합니다. 새로운 데이터 타입이 추가되어도 **Manager 코드를 수정할 필요가 없습니다.**

```csharp
private void Init()
{
    var jsonFiles = Resources.LoadAll<TextAsset>("JSON");

    foreach (var file in jsonFiles)
    {
        // 파일명 → 타입명 자동 매핑
        string typeName = $"GameData.{file.name}, Assembly-CSharp";
        Type dataType = Type.GetType(typeName);

        if (dataType == null) { /* 경고 후 skip */ continue; }

        LoadDefaultData(dataType, file);
    }
}
```

내부적으로 `Dictionary<Type, object>`에 `Dictionary<int, T>` 형태로 중첩 저장하여, `GetById<T>(id)` 호출 시 O(1) 조회가 가능합니다.

```csharp
public T GetById<T>(int id) where T : DefaultData
{
    if (_dataDic.TryGetValue(typeof(T), out object dictObj) &&
        dictObj is Dictionary<int, T> dict &&
        dict.TryGetValue(id, out T result))
    {
        return result;
    }
    // ...
}
```

---

## 4. PlayerData 시스템 — 동적 플레이어 데이터

### 4-1. PlayerData 추상 클래스

저장이 필요한 모든 플레이어 데이터는 `PlayerData`를 상속합니다.

```csharp
[Serializable]
public abstract class PlayerData
{
    public event Action OnDirty;        // 데이터 변경 시 발생

    public abstract void Initialize();  // 로드 후 초기화 로직
    protected void MarkDirty() => OnDirty?.Invoke();  // 자식 클래스에서 호출
    public abstract void GetReadyForSave();  // 저장 직전 처리
}
```

**설계 의도:** 자식 클래스가 데이터를 변경할 때 `MarkDirty()`를 명시적으로 호출하도록 강제합니다. 이를 통해 Manager가 어떤 타입의 데이터가 변경되었는지를 추적할 수 있고, 불필요한 파일 I/O를 줄일 수 있습니다.

### 4-2. PlayerDataManager — Dirty Flag 패턴

```csharp
private Dictionary<Type, PlayerData> _dataDic = new();
private HashSet<Type> _dirtyFlag = new();
```

데이터가 변경되면 해당 타입이 `_dirtyFlag`에 등록됩니다. 저장 시에는 Dirty 상태인 타입만 파일로 씁니다.

```csharp
private void Save()
{
    foreach (var type in _dirtyFlag)
    {
        WriteFile(type);  // 변경된 타입만 저장
    }
    _dirtyFlag.Clear();
}
```

### 4-3. Lazy Loading

`GetData<T>()`는 처음 호출될 때 파일을 읽습니다. 게임 시작 시 모든 데이터를 한꺼번에 로드하지 않아 초기 로딩 부담을 줄입니다.

```csharp
public T GetData<T>() where T : PlayerData, new()
{
    if (_dataDic.TryGetValue(typeof(T), out var data))
        return (T)data;     // 캐시 히트

    return Load<T>();       // 최초 접근 시 파일 로드
}
```

---

## 5. CSV → JSON 자동 변환 파이프라인

### 5-1. 설계 배경

기획 데이터는 Google Sheets에서 관리됩니다. 기획자가 수치를 변경할 때마다 개발자가 수동으로 JSON을 편집하는 것은 비효율적이고 오류 가능성이 높습니다. 이를 해결하기 위해 **Unity Editor 내 원클릭 변환 툴**을 구현했습니다.

### 5-2. 변환 흐름

```
① 시트 목록 CSV 다운로드 (파일명, URL 쌍으로 구성)
        ↓
② 각 시트별 CSV 다운로드
        ↓
③ 헤더 파싱 → 리플렉션으로 C# 타입 필드에 매핑
        ↓
④ 중첩 필드 처리 (header: "horizontalPoint.rowIndex" 형태)
        ↓
⑤ JsonUtility.ToJson → Resources/JSON/{파일명}.json 저장
```

### 5-3. 중첩 필드 처리 — 핵심 구현

CSV 헤더에 `.` 구분자를 사용하면 중첩 구조를 표현할 수 있습니다.

| CSV 헤더 예시 | 매핑되는 C# 필드 |
|---------------|-----------------|
| `id` | `MapBackgroundData.id` |
| `horizontalPoint.rowIndex` | `MapBackgroundData.horizontalPoint.rowIndex` |
| `verticalPoint.tileName` | `MapBackgroundData.verticalPoint.tileName` |

```csharp
if (header.Contains('.'))
{
    var parts = header.Split('.');
    string parentFieldName = parts[0].Trim();
    string childFieldName  = parts[1].Trim();

    // 부모 객체 캐시 (같은 행 내 중복 생성 방지)
    if (!subFieldCache.TryGetValue(parentFieldName, out object parentValue))
    {
        parentValue = Activator.CreateInstance(parentField.FieldType);
        parentField.SetValue(obj, parentValue);
        subFieldCache[parentFieldName] = parentValue;
    }

    // 자식 필드에 값 설정
    childField.SetValue(parentValue, parsedChildValue);
}
```

`subFieldCache`를 사용해 같은 행(row)에서 동일한 부모 객체를 여러 번 생성하지 않도록 최적화했습니다.

---

## 6. MapData 구현 예시

`MapData`는 `PlayerData`를 상속한 실제 구현 예시로, 이 시스템의 설계 패턴이 어떻게 활용되는지 잘 보여줍니다.

### 6-1. 저장 데이터와 런타임 데이터의 분리

```csharp
// ── 저장용 임시 필드 (직렬화 대상) ──────────────────
public List<MapBackgroundData> BackgroundDatasForSave;
public List<MapProbData> ProbDatasForSave;
// ...

// ── 런타임 실제 데이터 (직렬화 제외) ──────────────────
private List<MapBackgroundData> _currentBackgrounds;
private List<MapProbData> _currentProbs;
```

**의도:** 런타임에는 private 필드로만 데이터를 조작하고, 저장 직전 `GetReadyForSave()`에서 public 필드로 복사합니다. 외부에서 직렬화 필드를 직접 수정해도 런타임 데이터에 영향을 주지 않습니다.

### 6-2. 초기화 로직 — 저장 데이터 우선, 없으면 기본값

```csharp
public override void Initialize()
{
    if (BackgroundDatasForSave != null)
    {
        // 저장된 데이터 복원
        _currentBackgrounds = BackgroundDatasForSave.ToList();
        BackgroundDatasForSave = null;  // 임시 필드 해제
    }
    else
    {
        // 최초 실행: DefaultDataManager에서 기본값 로드
        for (int i = 0; i < (int)Enums.TileArea.Max; i++)
        {
            int defaultId = (i + 1) * 100 + 1;
            _currentBackgrounds.Add(defaultDataManager.GetById<MapBackgroundData>(defaultId));
        }
    }
}
```

### 6-3. 공개 API

외부 시스템은 내부 구현을 알 필요 없이 명확한 메서드만 호출합니다.

```csharp
mapData.GetCurrentTileData(Enums.TileArea.Forest);   // 현재 적용된 타일 조회
mapData.IsTilePossessed(tileID);                      // 보유 여부 확인
mapData.ObtainTile(tileID);                           // 타일 획득 (→ MarkDirty 자동 호출)
mapData.ChangeTile(areaType, bgData);                 // 타일 변경
```

---

## 7. 설계 의도 및 고민 흔적

### ① 정적/동적 데이터의 완전한 분리
게임 데이터는 "변하지 않는 것(밸런스 수치)"과 "변하는 것(플레이어 진행 상황)"으로 나뉩니다. 이 둘을 같은 구조로 관리하면 저장 로직이 복잡해지고 실수가 발생하기 쉽습니다. 역할 별로 시스템을 분리하여 각각 단순하게 유지했습니다.

### ② 리플렉션을 통한 확장성 확보
`DefaultDataManager`와 `CsvToJsonConverter` 모두 타입을 하드코딩하지 않습니다. 새로운 데이터 타입(`WorkerData`, `LabUpgradeData` 등)이 추가될 때 **Manager나 Converter 코드를 수정하지 않아도** 자동으로 동작합니다. 기획-개발 간 협업 비용을 줄이기 위한 선택이었습니다.

### ③ Dirty Flag로 불필요한 I/O 방지
모바일 환경에서 파일 I/O는 비싼 연산입니다. 변경된 타입만 추적하여 저장함으로써 저장 비용을 최소화했습니다. 특히 여러 데이터 타입이 존재할 때 효과적입니다.

### ④ 저장용 필드와 런타임 필드의 이중 구조
`JsonUtility`는 public 필드만 직렬화합니다. 이 특성을 활용해 런타임 데이터는 private으로 보호하고, 저장 시점에만 public 필드로 이전하는 방식을 택했습니다. 캡슐화를 유지하면서 직렬화 요구사항도 충족합니다.


