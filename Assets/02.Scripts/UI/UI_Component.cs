using UnityEngine;

/// <summary>
/// UI 컴포넌트의 기본 추상 클래스입니다.
/// 모든 UI 관련 컴포넌트는 이 클래스를 상속받아 구현해야 합니다.
/// </summary>
public abstract class UI_Component : MonoBehaviour
{
    #region Unity 이벤트 함수
    /// <summary>
    /// 컴포넌트가 활성화될 때 이벤트를 등록합니다.
    /// </summary>
    protected virtual void OnEnable()
    {
        RegisterEvents();
    }

    /// <summary>
    /// 컴포넌트가 비활성화될 때 이벤트를 해제합니다.
    /// </summary>
    protected virtual void OnDisable()
    {
        try
        {
            // EventManager가 존재하는지 확인하는 로직 추가
            if (EventManager.HasInstance)
            {
                UnregisterEvents();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"이벤트 해제 중 오류 발생: {e.Message}");
        }
    }
    #endregion

    #region 이벤트 관리
    /// <summary>
    /// 이벤트 등록을 처리합니다.
    /// 하위 클래스에서 이 메서드를 구현하여 필요한 이벤트를 구독해야 합니다.
    /// </summary>
    protected abstract void RegisterEvents();

    /// <summary>
    /// 이벤트 해제를 처리합니다.
    /// 하위 클래스에서 이 메서드를 구현하여 구독한 이벤트를 해제해야 합니다.
    /// </summary>
    protected abstract void UnregisterEvents();
    #endregion
}