using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 플레이어 스태미너 UI를 관리하는 컴포넌트
/// </summary>
public class UI_StaminaComponent : UI_Component, IUIPlayerComponent
{
    #region 필드
    [Header("스태미너 UI")]
    /// <summary>
    /// 스태미너 양을 표시하는 슬라이더
    /// </summary>
    [SerializeField] private Slider _staminaSlider;

    /// <summary>
    /// 스태미너 수치를 표시하는 텍스트
    /// </summary>
    [SerializeField] private TextMeshProUGUI _staminaText;

    /// <summary>
    /// 스태미너 슬라이더의 채움 이미지
    /// </summary>
    [SerializeField] private Image _staminaFillImage;

    [Header("스태미너 색상 설정")]
    /// <summary>
    /// 일반 상태의 스태미너 색상
    /// </summary>
    [SerializeField] private Color _normalColor = Color.green;

    /// <summary>
    /// 낮은 스태미너 상태의 색상
    /// </summary>
    [SerializeField] private Color _lowColor = Color.red;

    /// <summary>
    /// 낮은 스태미너로 간주하는 비율 기준값 (0~1)
    /// </summary>
    [SerializeField] private float _lowStaminaThreshold = 0.3f; // 스태미너가 30% 이하면 색상 변경

    /// <summary>
    /// 플레이어 스탯 컴포넌트 참조
    /// </summary>
    private PlayerStat _playerStat;
    #endregion

    #region Unity 이벤트 함수
    /// <summary>
    /// 시작 시 초기화를 수행합니다.
    /// </summary>
    private void Start()
    {
        // 플레이어 스탯이 설정된 경우 초기 UI 업데이트
        if (_playerStat != null)
        {
            UpdateStaminaUI(_playerStat.Stamina, _playerStat.MaxStamina);
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
            _playerStat.OnStaminaChanged -= UpdateStaminaUI;
        }

        _playerStat = playerStat;

        // 새 이벤트 등록
        if (_playerStat != null)
        {
            _playerStat.OnStaminaChanged += UpdateStaminaUI;
            // 초기값으로 UI 업데이트
            UpdateStaminaUI(_playerStat.Stamina, _playerStat.MaxStamina);
        }
    }

    /// <summary>
    /// PlayerFire 참조 설정 (IUIPlayerComponent 구현)
    /// 이 컴포넌트는 PlayerFire를 사용하지 않으므로 구현은 비어있습니다.
    /// </summary>
    /// <param name="playerFire">설정할 플레이어 발사 컴포넌트</param>
    public void SetPlayerFire(PlayerFire playerFire)
    {
        // 이 컴포넌트는 PlayerFire를 사용하지 않으므로 아무 작업도 수행하지 않음
    }
    #endregion

    #region 이벤트 등록
    /// <summary>
    /// 이벤트 구독을 등록합니다.
    /// </summary>
    protected override void RegisterEvents()
    {
        if (_playerStat != null)
        {
            _playerStat.OnStaminaChanged += UpdateStaminaUI;
        }
    }

    /// <summary>
    /// 이벤트 구독을 해제합니다.
    /// </summary>
    protected override void UnregisterEvents()
    {
        if (_playerStat != null)
        {
            _playerStat.OnStaminaChanged -= UpdateStaminaUI;
        }
    }
    #endregion

    #region UI 업데이트 메서드
    /// <summary>
    /// 스태미너 UI 업데이트
    /// </summary>
    /// <param name="currentStamina">현재 스태미너 값</param>
    /// <param name="maxStamina">최대 스태미너 값</param>
    private void UpdateStaminaUI(float currentStamina, float maxStamina)
    {
        if (_staminaSlider != null)
        {
            float ratio = currentStamina / maxStamina;
            _staminaSlider.value = ratio;

            // 스태미너 텍스트 업데이트 (있는 경우)
            if (_staminaText != null)
            {
                _staminaText.text = $"{Mathf.RoundToInt(currentStamina)} / {Mathf.RoundToInt(maxStamina)}";
            }

            // 스태미너 색상 변경 (있는 경우)
            if (_staminaFillImage != null)
            {
                _staminaFillImage.color = ratio <= _lowStaminaThreshold ? _lowColor : _normalColor;
            }
        }
    }
    #endregion
}