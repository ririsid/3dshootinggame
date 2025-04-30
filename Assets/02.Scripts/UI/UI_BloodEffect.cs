using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

/// <summary>
/// 플레이어 피격 시 화면에 혈흔 효과를 표시하는 UI 컴포넌트
/// </summary>
public class UI_BloodEffect : UI_Component
{
    [Header("혈흔 효과 설정")]
    [SerializeField] private Image _bloodOverlay;
    [SerializeField] private float _fadeInDuration = 0.1f;
    [SerializeField] private float _fadeOutDuration = 1.0f;
    [SerializeField] private float _maxAlpha = 0.8f;

    [Header("피해량 텍스트 설정")]
    [SerializeField] private bool _showDamageText = true;
    [SerializeField] private TextMeshProUGUI _damageText;
    [SerializeField] private float _damageTextDuration = 1.0f;

    // 현재 진행 중인 트윈 애니메이션
    private Tween _currentFadeTween;
    private Coroutine _damageTextCoroutine;

    #region Unity 이벤트 함수
    private void Awake()
    {
        // 초기화 시 혈흔 이미지 투명도 0으로 설정
        if (_bloodOverlay != null)
        {
            Color initialColor = _bloodOverlay.color;
            initialColor.a = 0f;
            _bloodOverlay.color = initialColor;
        }

        // 텍스트가 있다면 초기 숨김 처리
        if (_damageText != null)
        {
            _damageText.gameObject.SetActive(false);
        }
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// 플레이어가 피격됐을 때 호출되어 혈흔 효과를 표시합니다
    /// </summary>
    /// <param name="damage">받은 피해량</param>
    public void ShowBloodEffect(int damage)
    {
        // 이전 애니메이션이 있다면 중단
        _currentFadeTween?.Kill();

        // 새로운 페이드 인/아웃 애니메이션 시작
        Color targetColor = _bloodOverlay.color;

        // 페이드 인
        targetColor.a = _maxAlpha;
        _currentFadeTween = _bloodOverlay.DOColor(targetColor, _fadeInDuration)
            .OnComplete(() =>
            {
                // 페이드 아웃
                targetColor.a = 0f;
                _currentFadeTween = _bloodOverlay.DOColor(targetColor, _fadeOutDuration);
            });

        // 설정에 따라 피해 텍스트도 표시
        if (_showDamageText && _damageText != null)
        {
            if (_damageTextCoroutine != null)
            {
                StopCoroutine(_damageTextCoroutine);
            }
            _damageTextCoroutine = StartCoroutine(ShowDamageTextCoroutine(damage));
        }
    }
    #endregion

    #region 비공개 메서드
    /// <summary>
    /// 받은 피해량을 화면에 표시하는 코루틴
    /// </summary>
    private IEnumerator ShowDamageTextCoroutine(int damage)
    {
        _damageText.gameObject.SetActive(true);
        _damageText.text = $"-{damage}";

        // 텍스트 페이드 아웃 효과
        _damageText.alpha = 1f;
        _damageText.transform.localScale = Vector3.one;

        // 크기 증가 및 페이드 아웃 효과 적용
        _damageText.transform.DOScale(1.2f, _damageTextDuration);
        _damageText.DOFade(0f, _damageTextDuration);

        yield return new WaitForSeconds(_damageTextDuration);

        _damageText.gameObject.SetActive(false);
    }
    #endregion

    #region 이벤트 처리
    protected override void RegisterEvents()
    {
        if (EventManager.HasInstance)
        {
            EventManager.Instance.OnPlayerDamaged += HandlePlayerDamaged;
        }
    }

    protected override void UnregisterEvents()
    {
        if (EventManager.HasInstance)
        {
            EventManager.Instance.OnPlayerDamaged -= HandlePlayerDamaged;
        }
    }

    private void HandlePlayerDamaged(Damage damage)
    {
        ShowBloodEffect(damage.Amount);
    }
    #endregion
}