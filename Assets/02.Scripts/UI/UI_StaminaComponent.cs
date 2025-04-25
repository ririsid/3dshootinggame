using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 플레이어 스태미너 UI를 관리하는 컴포넌트
/// </summary>
public class UI_StaminaComponent : UI_Component, IUIPlayerComponent
{
    [Header("스태미너 UI")]
    [SerializeField] private Slider _staminaSlider;
    [SerializeField] private TextMeshProUGUI _staminaText;
    [SerializeField] private Image _staminaFillImage;

    [Header("스태미너 색상 설정")]
    [SerializeField] private Color _normalColor = Color.green;
    [SerializeField] private Color _lowColor = Color.red;
    [SerializeField] private float _lowStaminaThreshold = 0.3f; // 스태미너가 30% 이하면 색상 변경

    private PlayerStat _playerStat;

    #region 공개 메서드
    /// <summary>
    /// PlayerStat 참조 설정 (IUIPlayerComponent 구현)
    /// </summary>
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
    public void SetPlayerFire(PlayerFire playerFire)
    {
        // 이 컴포넌트는 PlayerFire를 사용하지 않으므로 아무 작업도 수행하지 않음
    }
    #endregion

    #region 이벤트 등록
    protected override void RegisterEvents()
    {
        if (_playerStat != null)
        {
            _playerStat.OnStaminaChanged += UpdateStaminaUI;
        }
    }

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