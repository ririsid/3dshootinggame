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
    /// <summary>
    /// 플레이어 스탯 컴포넌트 참조
    /// </summary>
    [SerializeField] private PlayerStat _playerStat;
    
    /// <summary>
    /// 플레이어 발사 컴포넌트 참조
    /// </summary>
    [SerializeField] private PlayerFire _playerFire;

    /// <summary>
    /// 모든 UI 컴포넌트를 관리할 리스트
    /// </summary>
    private List<IUIPlayerComponent> _uiComponents = new List<IUIPlayerComponent>();

    #region Unity 이벤트 함수
    /// <summary>
    /// 초기화 작업을 수행합니다.
    /// </summary>
    private void Awake()
    {
        // UI 컴포넌트 자동 수집
        CollectUIComponents();
    }

    /// <summary>
    /// 시작 시 컴포넌트 초기화
    /// </summary>
    private void Start()
    {
        InitializeComponents();
    }
    #endregion

    #region 컴포넌트 수집
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

        // 개발 빌드에서만 로그 출력
        if (Debug.isDebugBuild)
        {
            Debug.Log($"UI 컴포넌트가 {_uiComponents.Count}개 수집되었습니다.");
        }
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

    #region 이벤트 콜백
    /// <summary>
    /// 이벤트를 등록합니다.
    /// </summary>
    protected override void RegisterEvents()
    {
        // 이 클래스에서는 직접적인 이벤트 등록이 필요 없음
        // 각 컴포넌트가 자체적으로 이벤트를 처리함
    }

    /// <summary>
    /// 이벤트를 해제합니다.
    /// </summary>
    protected override void UnregisterEvents()
    {
        // 이 클래스에서는 직접적인 이벤트 해제가 필요 없음
        // 각 컴포넌트가 자체적으로 이벤트를 처리함
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// PlayerStat 참조 설정 및 하위 컴포넌트에 전달
    /// </summary>
    /// <param name="playerStat">설정할 플레이어 스탯 컴포넌트</param>
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
    /// <param name="playerFire">설정할 플레이어 발사 컴포넌트</param>
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