using UnityEngine;
using TMPro;

/// <summary>
/// 총알 정보를 표시하는 UI 컴포넌트
/// </summary>
public class UI_AmmoComponent : UI_Component, IUIPlayerComponent
{
    #region 필드
    [Header("UI 참조")]
    /// <summary>
    /// 총알 정보를 표시할 텍스트 컴포넌트
    /// </summary>
    [SerializeField] private TextMeshProUGUI _ammoText;

    [Header("플레이어 참조")]
    /// <summary>
    /// 플레이어 발사 컴포넌트 참조
    /// </summary>
    [SerializeField] private PlayerFire _playerFire;

    /// <summary>
    /// 플레이어 스탯 컴포넌트 참조
    /// </summary>
    [SerializeField] private PlayerStat _playerStat;

    [Header("텍스트 설정")]
    /// <summary>
    /// 총알 텍스트 표시 형식 (예: "{0} / {1}")
    /// </summary>
    [SerializeField] private string _ammoTextFormat = "{0} / {1}";

    /// <summary>
    /// 일반 상태 텍스트 색상
    /// </summary>
    [SerializeField] private Color _normalColor = Color.white;

    /// <summary>
    /// 탄약 부족 상태 텍스트 색상
    /// </summary>
    [SerializeField] private Color _lowAmmoColor = Color.red;

    /// <summary>
    /// 탄약 부족 기준 비율 (0 ~ 1)
    /// </summary>
    [SerializeField] private float _lowAmmoThreshold = 0.25f; // 탄약이 최대의 25% 이하일 때 색상 변경
    #endregion

    #region Unity 이벤트 함수
    /// <summary>
    /// 컴포넌트 초기화 및 참조 설정
    /// </summary>
    private void Start()
    {
        // Inspector에서 참조가 주입되지 않은 경우 자동으로 찾기
        if (_playerFire == null)
        {
            _playerFire = FindFirstObjectByType<PlayerFire>();
            if (_playerFire == null)
            {
                if (Debug.isDebugBuild)
                {
                    Debug.LogError("PlayerFire 컴포넌트를 찾을 수 없습니다!", this);
                }
                return;
            }
        }

        // PlayerStat 참조가 없으면 PlayerFire에서 가져오기
        if (_playerStat == null && _playerFire != null)
        {
            _playerStat = _playerFire.GetComponent<PlayerStat>();
        }

        // 초기 UI 업데이트
        if (_playerStat != null)
        {
            UpdateAmmoDisplay(_playerStat.CurrentAmmo, _playerStat.MaxAmmo);
        }
    }
    #endregion

    #region 이벤트 등록
    /// <summary>
    /// UI 컴포넌트의 이벤트를 등록합니다.
    /// </summary>
    protected override void RegisterEvents()
    {
        if (_playerFire != null)
        {
            _playerFire.OnAmmoChanged += UpdateAmmoDisplay;
        }
    }

    /// <summary>
    /// UI 컴포넌트의 이벤트를 해제합니다.
    /// </summary>
    protected override void UnregisterEvents()
    {
        if (_playerFire != null)
        {
            _playerFire.OnAmmoChanged -= UpdateAmmoDisplay;
        }
    }
    #endregion

    #region IUIPlayerComponent 구현
    /// <summary>
    /// PlayerFire 참조 설정 (IUIPlayerComponent 구현)
    /// </summary>
    /// <param name="playerFire">설정할 PlayerFire 컴포넌트</param>
    public void SetPlayerFire(PlayerFire playerFire)
    {
        // 기존 이벤트 해제
        UnregisterEvents();

        _playerFire = playerFire;

        // 새 이벤트 등록
        RegisterEvents();

        // PlayerStat 참조도 업데이트
        if (_playerFire != null)
        {
            _playerStat = _playerFire.GetComponent<PlayerStat>();
            if (_playerStat != null)
            {
                UpdateAmmoDisplay(_playerStat.CurrentAmmo, _playerStat.MaxAmmo);
            }
        }
    }

    /// <summary>
    /// PlayerStat 참조 설정 (IUIPlayerComponent 구현)
    /// </summary>
    /// <param name="playerStat">설정할 PlayerStat 컴포넌트</param>
    public void SetPlayerStat(PlayerStat playerStat)
    {
        _playerStat = playerStat;

        if (_playerStat != null)
        {
            UpdateAmmoDisplay(_playerStat.CurrentAmmo, _playerStat.MaxAmmo);
        }
    }
    #endregion

    #region 비공개 메서드
    /// <summary>
    /// 총알 정보 표시 업데이트
    /// </summary>
    /// <param name="currentAmmo">현재 총알 수</param>
    /// <param name="maxAmmo">최대 총알 수</param>
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