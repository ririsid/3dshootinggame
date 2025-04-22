using UnityEngine;
using DG.Tweening;

public class UI_Crosshair : MonoBehaviour
{
    [Header("크로스헤어 설정")]
    [SerializeField] private RectTransform _crosshairTransform;
    [SerializeField] private float _recoilScale = 1.2f;         // 반동 시 확대 배율
    [SerializeField] private float _recoilDuration = 0.05f;     // 반동 지속 시간
    [SerializeField] private float _returnDuration = 0.1f;      // 원래 크기로 돌아오는 시간
    [SerializeField] private Ease _recoilEase = Ease.OutQuad;   // 반동 이징
    [SerializeField] private Ease _returnEase = Ease.InOutQuad; // 복귀 이징

    [Header("참조")]
    [SerializeField] private PlayerFire _playerFire;

    private Vector3 _originalScale;
    private Tweener _currentTween;

    #region Unity Event Functions
    private void Awake()
    {
        // 크로스헤어 없으면 현재 게임오브젝트의 RectTransform 사용
        if (_crosshairTransform == null)
            _crosshairTransform = GetComponent<RectTransform>();

        _originalScale = _crosshairTransform.localScale;
    }

    private void OnEnable()
    {
        SetupEvents();
    }

    private void OnDisable()
    {
        UnregisterEvents();
    }
    #endregion

    #region Event Registration
    private void SetupEvents()
    {
        if (_playerFire != null)
        {
            _playerFire.OnWeaponFired += ApplyRecoilEffect;
        }
    }

    private void UnregisterEvents()
    {
        if (_playerFire != null)
        {
            _playerFire.OnWeaponFired -= ApplyRecoilEffect;
        }
    }
    #endregion

    #region Effect Methods
    /// <summary>
    /// 총 발사 시 크로스헤어에 반동 효과를 적용합니다.
    /// </summary>
    private void ApplyRecoilEffect()
    {
        // 기존 트윈 중지
        _currentTween?.Kill();

        // 크로스헤어 확대 (반동 효과)
        _currentTween = _crosshairTransform.DOScale(_originalScale * _recoilScale, _recoilDuration)
            .SetEase(_recoilEase)
            .OnComplete(() =>
            {
                // 원래 크기로 돌아오기
                _currentTween = _crosshairTransform.DOScale(_originalScale, _returnDuration)
                    .SetEase(_returnEase);
            });
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// PlayerFire 참조를 설정합니다.
    /// </summary>
    public void SetupPlayerFire(PlayerFire playerFire)
    {
        // 기존 이벤트 연결 해제
        UnregisterEvents();

        // 새 참조 설정
        _playerFire = playerFire;

        // 새 이벤트 연결
        SetupEvents();
    }
    #endregion
}