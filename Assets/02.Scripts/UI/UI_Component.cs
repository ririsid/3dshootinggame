using UnityEngine;

/// <summary>
/// UI 컴포넌트의 기본 추상 클래스
/// </summary>
public abstract class UI_Component : MonoBehaviour
{
    protected virtual void OnEnable()
    {
        RegisterEvents();
    }

    protected virtual void OnDisable()
    {
        UnregisterEvents();
    }

    /// <summary>
    /// 이벤트 등록
    /// </summary>
    protected abstract void RegisterEvents();

    /// <summary>
    /// 이벤트 해제
    /// </summary>
    protected abstract void UnregisterEvents();
}