using UnityEngine;
using DG.Tweening;

public class UI_CrosshairComponent : UI_Component, IUIPlayerComponent
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
    #endregion

    #region Event Registration
    protected override void RegisterEvents()
    {
        if (_playerFire != null)
        {
            _playerFire.OnWeaponFired += ApplyRecoilEffect;
        }
    }

    protected override void UnregisterEvents()
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
    /// PlayerFire 참조를 설정합니다. (IUIPlayerComponent 구현)
    /// </summary>
    public void SetPlayerFire(PlayerFire playerFire)
    {
        // 기존 이벤트 연결 해제
        UnregisterEvents();

        // 새 참조 설정
        _playerFire = playerFire;

        // 새 이벤트 연결
        RegisterEvents();
    }

    /// <summary>
    /// 이전 버전과의 호환성을 위한 메서드
    /// </summary>
    public void SetupPlayerFire(PlayerFire playerFire)
    {
        SetPlayerFire(playerFire);
    }

    /// <summary>
    /// PlayerStat 참조를 설정합니다. (IUIPlayerComponent 구현)
    /// 이 컴포넌트는 PlayerStat을 사용하지 않으므로 빈 구현입니다.
    /// </summary>
    public void SetPlayerStat(PlayerStat playerStat)
    {
        // 이 컴포넌트는 PlayerStat을 사용하지 않으므로 아무 작업도 수행하지 않음
    }
    #endregion
}