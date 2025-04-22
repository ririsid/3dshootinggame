using UnityEngine;
using TMPro;

/// <summary>
/// 총알 정보를 표시하는 UI 컴포넌트
/// </summary>
public class UI_AmmoComponent : UI_Component, IUIPlayerComponent
{
    #region Fields
    [Header("UI 참조")]
    [SerializeField] private TextMeshProUGUI _ammoText;

    [Header("텍스트 설정")]
    [SerializeField] private string _ammoTextFormat = "{0} / {1}";
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _lowAmmoColor = Color.red;
    [SerializeField] private float _lowAmmoThreshold = 0.25f; // 탄약이 최대의 25% 이하일 때 색상 변경

    private PlayerFire _playerFire;
    #endregion

    #region Unity Event Functions
    private void Start()
    {
        // PlayerFire 컴포넌트 찾기
        if (_playerFire == null)
        {
            _playerFire = FindFirstObjectByType<PlayerFire>();
            if (_playerFire == null)
            {
                Debug.LogError("PlayerFire 컴포넌트를 찾을 수 없습니다!", this);
                return;
            }
        }

        // 초기 UI 업데이트
        PlayerStat playerStat = _playerFire.GetComponent<PlayerStat>();
        if (playerStat != null)
        {
            UpdateAmmoDisplay(playerStat.CurrentAmmo, playerStat.MaxAmmo);
        }
    }
    #endregion

    #region Event Registration
    protected override void RegisterEvents()
    {
        if (_playerFire != null)
        {
            _playerFire.OnAmmoChanged += UpdateAmmoDisplay;
        }
    }

    protected override void UnregisterEvents()
    {
        if (_playerFire != null)
        {
            _playerFire.OnAmmoChanged -= UpdateAmmoDisplay;
        }
    }
    #endregion

    #region IUIPlayerComponent Implementation
    /// <summary>
    /// PlayerFire 참조 설정 (IUIPlayerComponent 구현)
    /// </summary>
    public void SetPlayerFire(PlayerFire playerFire)
    {
        // 기존 이벤트 해제
        UnregisterEvents();

        _playerFire = playerFire;

        // 새 이벤트 등록
        RegisterEvents();

        // 초기값으로 UI 업데이트
        if (_playerFire != null)
        {
            PlayerStat playerStat = _playerFire.GetComponent<PlayerStat>();
            if (playerStat != null)
            {
                UpdateAmmoDisplay(playerStat.CurrentAmmo, playerStat.MaxAmmo);
            }
        }
    }

    /// <summary>
    /// PlayerStat 참조 설정 (IUIPlayerComponent 구현)
    /// 이 컴포넌트는 PlayerStat을 직접 사용하지 않고 PlayerFire를 통해 간접적으로 사용합니다.
    /// </summary>
    public void SetPlayerStat(PlayerStat playerStat)
    {
        // 이 컴포넌트는 PlayerStat을 직접 사용하지 않으므로 아무 작업도 수행하지 않음
        // PlayerFire에서 PlayerStat 참조를 가져옴
        if (_playerFire != null && playerStat != null)
        {
            UpdateAmmoDisplay(playerStat.CurrentAmmo, playerStat.MaxAmmo);
        }
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// 총알 정보 표시 업데이트
    /// </summary>
    private void UpdateAmmoDisplay(int currentAmmo, int maxAmmo)
    {
        if (_ammoText == null) return;

        // 텍스트 업데이트
        _ammoText.text = string.Format(_ammoTextFormat, currentAmmo, maxAmmo);

        // 탄약이 적을 때 색상 변경
        float ammoRatio = (float)currentAmmo / maxAmmo;
        if (ammoRatio <= _lowAmmoThreshold)
        {
            _ammoText.color = _lowAmmoColor;
        }
        else
        {
            _ammoText.color = _normalColor;
        }
    }
    #endregion
}