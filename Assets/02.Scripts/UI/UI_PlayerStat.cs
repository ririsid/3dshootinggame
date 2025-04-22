using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// UI 컴포넌트 참조 관리를 위한 인터페이스
/// </summary>
public interface IUIPlayerComponent
{
    /// <summary>
    /// PlayerStat 참조를 설정하는 메서드
    /// </summary>
    void SetPlayerStat(PlayerStat playerStat);

    /// <summary>
    /// PlayerFire 참조를 설정하는 메서드
    /// </summary>
    void SetPlayerFire(PlayerFire playerFire);
}

/// <summary>
/// 플레이어 스탯 UI 관리자 - 각 UI 컴포넌트를 관리하는 파사드(Facade) 역할
/// </summary>
public class UI_PlayerStat : UI_Component
{
    [Header("참조")]
    [SerializeField] private PlayerStat _playerStat;
    [SerializeField] private PlayerFire _playerFire;

    // 모든 UI 컴포넌트를 관리할 리스트
    private List<IUIPlayerComponent> _uiComponents = new List<IUIPlayerComponent>();

    #region Unity Event Functions
    private void Awake()
    {
        // UI 컴포넌트 자동 수집
        CollectUIComponents();
    }

    private void Start()
    {
        InitializeComponents();
    }
    #endregion

    #region Component Collection
    /// <summary>
    /// 하위 UI 컴포넌트들을 수집하는 메서드
    /// </summary>
    private void CollectUIComponents()
    {
        // 자식 게임오브젝트에서 IUIPlayerComponent를 구현한 모든 컴포넌트 찾기
        IUIPlayerComponent[] childComponents = GetComponentsInChildren<IUIPlayerComponent>(true);
        foreach (var component in childComponents)
        {
            if (!_uiComponents.Contains(component))
            {
                _uiComponents.Add(component);
            }
        }

        Debug.Log($"[UI_PlayerStat] {_uiComponents.Count}개의 UI 컴포넌트가 수집되었습니다.");
    }

    /// <summary>
    /// UI 컴포넌트 초기화
    /// </summary>
    private void InitializeComponents()
    {
        // 수집된 모든 컴포넌트에 참조 전달
        foreach (var component in _uiComponents)
        {
            if (_playerStat != null)
            {
                component.SetPlayerStat(_playerStat);
            }

            if (_playerFire != null)
            {
                component.SetPlayerFire(_playerFire);
            }
        }
    }
    #endregion

    #region Event Callbacks
    protected override void RegisterEvents()
    {
        // 이 클래스에서는 직접적인 이벤트 등록이 필요 없음
        // 각 컴포넌트가 자체적으로 이벤트를 처리함
    }

    protected override void UnregisterEvents()
    {
        // 이 클래스에서는 직접적인 이벤트 해제가 필요 없음
        // 각 컴포넌트가 자체적으로 이벤트를 처리함
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// PlayerStat 참조 설정 및 하위 컴포넌트에 전달
    /// </summary>
    public void SetupPlayerStat(PlayerStat playerStat)
    {
        _playerStat = playerStat;

        // 모든 컴포넌트에 참조 전달
        foreach (var component in _uiComponents)
        {
            component.SetPlayerStat(playerStat);
        }
    }

    /// <summary>
    /// PlayerFire 참조 설정 및 하위 컴포넌트에 전달
    /// </summary>
    public void SetupPlayerFire(PlayerFire playerFire)
    {
        _playerFire = playerFire;

        // 모든 컴포넌트에 참조 전달
        foreach (var component in _uiComponents)
        {
            component.SetPlayerFire(playerFire);
        }
    }

    /// <summary>
    /// 특정 타입의 UI 컴포넌트를 가져옵니다.
    /// </summary>
    /// <typeparam name="T">가져올 컴포넌트 타입</typeparam>
    /// <returns>찾은 컴포넌트, 없으면 null 반환</returns>
    public T GetUIComponent<T>() where T : MonoBehaviour, IUIPlayerComponent
    {
        foreach (var component in _uiComponents)
        {
            if (component is T typedComponent)
            {
                return typedComponent;
            }
        }
        return null;
    }
    #endregion
}