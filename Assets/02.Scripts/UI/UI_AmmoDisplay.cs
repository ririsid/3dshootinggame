using UnityEngine;
using TMPro;

public class UI_AmmoDisplay : MonoBehaviour
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
        _playerFire = FindFirstObjectByType<PlayerFire>();
        if (_playerFire == null)
        {
            Debug.LogError("PlayerFire 컴포넌트를 찾을 수 없습니다!", this);
            return;
        }

        // 이벤트 구독
        _playerFire.OnAmmoChanged += UpdateAmmoDisplay;

        // 초기 UI 업데이트
        PlayerStat playerStat = _playerFire.GetComponent<PlayerStat>();
        if (playerStat != null)
        {
            UpdateAmmoDisplay(playerStat.CurrentAmmo, playerStat.MaxAmmo);
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (_playerFire != null)
        {
            _playerFire.OnAmmoChanged -= UpdateAmmoDisplay;
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