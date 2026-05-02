using System;
using UnityEngine;

/// <summary>
/// 저장이 필요한 플레이어 데이터의 기반 클래스.
///
/// 사용 규칙:
/// - public 필드만 직렬화 대상에 포함됩니다. (property 제외)
/// - 컬렉션은 List&lt;T&gt;만 지원합니다. (Dictionary 등 불가)
/// - 중첩 클래스를 저장하려면 해당 클래스에 [Serializable]을 선언해야 합니다.
/// - 자식 클래스에서 데이터를 변경할 때는 반드시 MarkDirty()를 호출해야 합니다.
/// </summary>
[Serializable]
public abstract class PlayerData
{
    public event Action OnDirty;

    /// <summary>
    /// 파일 로드 후 런타임 데이터를 초기화합니다.
    /// </summary>
    public abstract void Initialize();

    /// <summary>
    /// 데이터 변경 시 호출하여 저장 대상으로 등록합니다.
    /// </summary>
    protected void MarkDirty() => OnDirty?.Invoke();

    /// <summary>
    /// 저장 직전에 런타임 데이터를 직렬화 필드로 복사하는 작업을 정의합니다.
    /// </summary>
    public abstract void GetReadyForSave();
}
