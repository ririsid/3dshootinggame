using UnityEngine;

/// <summary>
/// 플레이어 체력 UI를 관리하는 클래스입니다.
/// 화면에 고정된 DelayedHealthBar를 통해 플레이어 체력을 표시합니다.
/// </summary>
public class UI_PlayerHealthBar : UI_Component, IUIPlayerComponent
{
    [Header("참조")]
    [SerializeField] private DelayedHealthBar _healthBar;

    private PlayerStat _playerStat;

    #region Unity 이벤트 함수
    private void Awake()
    {
        // DelayedHealthBar 참조 확인 및 가져오기
        if (_healthBar == null)
        {
            _healthBar = GetComponentInChildren<DelayedHealthBar>();
            if (_healthBar == null && Debug.isDebugBuild)
            {
                Debug.LogWarning("UI_PlayerHealthBar: DelayedHealthBar 컴포넌트를 찾을 수 없습니다.");
            }
        }
    }
    #endregion

    #region IUIPlayerComponent 구현
    /// <summary>
    /// PlayerStat 참조를 설정합니다.
    /// </summary>
    public void SetPlayerStat(PlayerStat playerStat)
    {
        // 기존 이벤트 연결 해제
        UnregisterEvents();

        _playerStat = playerStat;

        if (_playerStat != null)
        {
            // 초기 체력 설정
            SetupHealthBar();
            // 이벤트 등록
            RegisterEvents();
        }
    }

    /// <summary>
    /// PlayerFire 참조를 설정합니다. (체력바에서는 사용하지 않음)
    /// </summary>
    public void SetPlayerFire(PlayerFire playerFire)
    {
        // 체력바에서는 PlayerFire를 사용하지 않음
    }
    #endregion

    #region 이벤트 관리
    /// <summary>
    /// 이벤트를 등록합니다.
    /// </summary>
    protected override void RegisterEvents()
    {
        // 이제 PlayerStat에서 체력 변경 이벤트를 구독
        if (_playerStat != null)
        {
            _playerStat.OnHealthChanged += UpdateHealthBar;
        }
    }

    /// <summary>
    /// 이벤트를 해제합니다.
    /// </summary>
    protected override void UnregisterEvents()
    {
        // PlayerStat 이벤트 구독 해제
        if (_playerStat != null)
        {
            _playerStat.OnHealthChanged -= UpdateHealthBar;
        }
    }
    #endregion

    #region 비공개 메서드
    /// <summary>
    /// 체력바를 초기화합니다.
    /// </summary>
    private void SetupHealthBar()
    {
        if (_healthBar != null && _playerStat != null)
        {
            _healthBar.MaxHealth = _playerStat.MaxHealth;
            _healthBar.CurrentHealth = _playerStat.CurrentHealth;
        }
    }

    /// <summary>
    /// 체력바를 업데이트합니다.
    /// </summary>
    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (_healthBar != null)
        {
            _healthBar.CurrentHealth = currentHealth;

            // 최대 체력이 변경되었을 경우 업데이트
            if (_healthBar.MaxHealth != maxHealth)
            {
                _healthBar.MaxHealth = maxHealth;
            }
        }
    }
    #endregion
}
