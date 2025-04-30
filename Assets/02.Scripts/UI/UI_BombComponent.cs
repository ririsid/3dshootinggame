using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 플레이어 폭탄 UI를 관리하는 컴포넌트
/// </summary>
public class UI_BombComponent : UI_Component, IUIPlayerComponent
{
    #region 필드
    [Header("폭탄 UI")]
    /// <summary>
    /// 폭탄 개수를 표시하는 텍스트
    /// </summary>
    [SerializeField] private TextMeshProUGUI _bombCountText;

    /// <summary>
    /// 폭탄 아이콘 이미지
    /// </summary>
    [SerializeField] private Image _bombIcon;

    [Header("폭탄 충전 UI")]
    /// <summary>
    /// 폭탄 충전 상태를 표시하는 인디케이터
    /// </summary>
    [SerializeField] private Transform _bombChargeIndicator; // 충전 인디케이터 Transform

    /// <summary>
    /// 플레이어 스탯 참조
    /// </summary>
    private PlayerStat _playerStat;

    /// <summary>
    /// 플레이어 발사 컴포넌트 참조
    /// </summary>
    private PlayerFire _playerFire;
    #endregion

    #region Unity 이벤트 함수
    /// <summary>
    /// 컴포넌트 초기화 작업을 수행합니다.
    /// </summary>
    private void Start()
    {
        // 인디케이터 크기 초기화
        if (_bombChargeIndicator != null)
        {
            _bombChargeIndicator.localScale = Vector3.zero;
        }

        // 초기값 설정
        if (_playerStat != null)
        {
            UpdateBombCountUI(_playerStat.CurrentBombCount, _playerStat.MaxBombCount);
        }
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// PlayerStat 참조 설정 (IUIPlayerComponent 구현)
    /// </summary>
    /// <param name="playerStat">설정할 플레이어 스탯 컴포넌트</param>
    public void SetPlayerStat(PlayerStat playerStat)
    {
        // 기존 이벤트 해제
        if (_playerStat != null)
        {
            _playerStat.OnBombCountChanged -= UpdateBombCountUI;
        }

        _playerStat = playerStat;

        // 새 이벤트 등록
        if (_playerStat != null)
        {
            _playerStat.OnBombCountChanged += UpdateBombCountUI;
            // 초기값으로 UI 업데이트
            UpdateBombCountUI(_playerStat.CurrentBombCount, _playerStat.MaxBombCount);
        }
    }

    /// <summary>
    /// PlayerFire 참조 설정 (IUIPlayerComponent 구현)
    /// </summary>
    /// <param name="playerFire">설정할 플레이어 발사 컴포넌트</param>
    public void SetPlayerFire(PlayerFire playerFire)
    {
        // 기존 이벤트 해제
        if (_playerFire != null)
        {
            _playerFire.OnBombChargeChanged -= UpdateBombChargeUI;
            _playerFire.OnBombChargeStateChanged -= SetBombChargeUIActive;
        }

        _playerFire = playerFire;

        // 새 이벤트 등록
        if (_playerFire != null)
        {
            _playerFire.OnBombChargeChanged += UpdateBombChargeUI;
            _playerFire.OnBombChargeStateChanged += SetBombChargeUIActive;
        }
    }
    #endregion

    #region 이벤트 등록
    /// <summary>
    /// UI 컴포넌트의 이벤트를 등록합니다.
    /// </summary>
    protected override void RegisterEvents()
    {
        if (_playerStat != null)
        {
            _playerStat.OnBombCountChanged += UpdateBombCountUI;
        }

        if (_playerFire != null)
        {
            _playerFire.OnBombChargeChanged += UpdateBombChargeUI;
            _playerFire.OnBombChargeStateChanged += SetBombChargeUIActive;
        }
    }

    /// <summary>
    /// UI 컴포넌트의 이벤트를 해제합니다.
    /// </summary>
    protected override void UnregisterEvents()
    {
        if (_playerStat != null)
        {
            _playerStat.OnBombCountChanged -= UpdateBombCountUI;
        }

        if (_playerFire != null)
        {
            _playerFire.OnBombChargeChanged -= UpdateBombChargeUI;
            _playerFire.OnBombChargeStateChanged -= SetBombChargeUIActive;
        }
    }
    #endregion

    #region UI 업데이트 메서드
    /// <summary>
    /// 폭탄 개수 UI 업데이트
    /// </summary>
    /// <param name="currentBombCount">현재 폭탄 개수</param>
    /// <param name="maxBombCount">최대 폭탄 개수</param>
    private void UpdateBombCountUI(int currentBombCount, int maxBombCount)
    {
        if (_bombCountText != null)
        {
            _bombCountText.text = $"{currentBombCount}/{maxBombCount}";
        }

        // 폭탄 아이콘 표시/숨김 (폭탄이 없을 때 흐리게 표시 등의 효과)
        if (_bombIcon != null)
        {
            _bombIcon.color = currentBombCount > 0 ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }
    }

    /// <summary>
    /// 폭탄 충전 UI 업데이트
    /// </summary>
    /// <param name="currentCharge">현재 충전량</param>
    /// <param name="maxCharge">최대 충전량</param>
    private void UpdateBombChargeUI(float currentCharge, float maxCharge)
    {
        if (_bombChargeIndicator != null)
        {
            float ratio = currentCharge / maxCharge;
            _bombChargeIndicator.localScale = new Vector3(ratio, ratio, ratio);
        }
    }

    /// <summary>
    /// 폭탄 충전 UI 활성화/비활성화
    /// </summary>
    /// <param name="isActive">UI 활성화 여부</param>
    private void SetBombChargeUIActive(bool isActive)
    {
        // 비활성화 시 인디케이터 크기 초기화
        if (!isActive && _bombChargeIndicator != null)
        {
            _bombChargeIndicator.localScale = Vector3.zero;
        }
    }
    #endregion
}