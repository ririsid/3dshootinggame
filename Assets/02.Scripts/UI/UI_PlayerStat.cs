using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_PlayerStat : MonoBehaviour
{
    [Header("스태미너 UI")]
    [SerializeField] private Slider _staminaSlider;
    [SerializeField] private TextMeshProUGUI _staminaText;
    [SerializeField] private Image _staminaFillImage;

    [Header("스태미너 색상 설정")]
    [SerializeField] private Color _normalColor = Color.green;
    [SerializeField] private Color _lowColor = Color.red;
    [SerializeField] private float _lowStaminaThreshold = 0.3f; // 스태미너가 30% 이하면 색상 변경

    [Header("참조")]
    [SerializeField] private PlayerStat _playerStat;

    private void OnEnable()
    {
        if (_playerStat != null)
        {
            _playerStat.OnStaminaChanged += UpdateStaminaUI;
        }
    }

    private void Start()
    {
        // 시작 시 PlayerStat이 있다면 현재 값으로 UI 초기화
        if (_playerStat != null)
        {
            UpdateStaminaUI(_playerStat.Stamina, _playerStat.MaxStamina);
        }
    }

    private void OnDisable()
    {
        if (_playerStat != null)
        {
            _playerStat.OnStaminaChanged -= UpdateStaminaUI;
        }
    }

    // 스태미너 UI 업데이트
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

    // 참조 설정 메서드
    public void SetupPlayerStat(PlayerStat playerStat)
    {
        // 이전 참조가 있으면 이벤트 구독 취소
        if (_playerStat != null)
        {
            _playerStat.OnStaminaChanged -= UpdateStaminaUI;
        }

        // 새 참조 설정
        _playerStat = playerStat;

        // 새 참조에 이벤트 구독
        if (_playerStat != null)
        {
            _playerStat.OnStaminaChanged += UpdateStaminaUI;
            // 초기 UI 업데이트
            UpdateStaminaUI(_playerStat.Stamina, _playerStat.MaxStamina);
        }
    }
}