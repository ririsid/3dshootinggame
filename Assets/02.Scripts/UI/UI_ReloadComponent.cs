using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// 재장전 진행상황을 표시하는 UI 컴포넌트
/// </summary>
public class UI_ReloadComponent : UI_Component, IUIPlayerComponent
{
    #region 필드
    [Header("UI 참조")]
    [SerializeField] private Image _circularProgressImage;
    [SerializeField] private TextMeshProUGUI _reloadTimeText;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("플레이어 참조")]
    [SerializeField] private PlayerFire _playerFire;
    [SerializeField] private PlayerStat _playerStat;

    [Header("애니메이션 설정")]
    [SerializeField] private float _fadeInDuration = 0.2f;
    [SerializeField] private float _fadeOutDuration = 0.3f;

    [Header("텍스트 설정")]
    [SerializeField] private string _reloadTextFormat = "{0:0.0}s";
    [SerializeField] private Color _normalColor = Color.black;
    [SerializeField] private Color _completeColor = Color.green;

    private float _reloadDuration;
    private bool _isVisible;
    private Coroutine _fadeCoroutine;
    #endregion

    #region Unity 이벤트 함수
    private void Awake()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        _canvasGroup.alpha = 0f;
        _isVisible = false;
    }

    private void Start()
    {
        // PlayerFire 컴포넌트 참조가 없으면 찾기
        if (_playerFire == null)
        {
            _playerFire = FindFirstObjectByType<PlayerFire>();
            if (_playerFire == null)
            {
                Debug.LogError("PlayerFire 컴포넌트를 찾을 수 없습니다!", this);
                return;
            }
        }

        // PlayerStat 참조가 없으면 PlayerFire에서 가져오기
        if (_playerStat == null && _playerFire != null)
        {
            _playerFire.TryGetComponent(out _playerStat);
        }

        // 재장전 시간 초기화
        UpdateReloadTime();
    }
    #endregion

    #region 이벤트 등록
    protected override void RegisterEvents()
    {
        if (_playerFire != null)
        {
            _playerFire.OnReloadStateChanged += HandleReloadStateChanged;
            _playerFire.OnReloadProgressChanged += HandleReloadProgressChanged;
            _playerFire.OnReloadCancelled += HandleReloadCancelled;
        }
    }

    protected override void UnregisterEvents()
    {
        if (_playerFire != null)
        {
            _playerFire.OnReloadStateChanged -= HandleReloadStateChanged;
            _playerFire.OnReloadProgressChanged -= HandleReloadProgressChanged;
            _playerFire.OnReloadCancelled -= HandleReloadCancelled;
        }
    }
    #endregion

    #region IUIPlayerComponent 구현
    /// <summary>
    /// PlayerStat 참조 설정 (IUIPlayerComponent 구현)
    /// </summary>
    public void SetPlayerStat(PlayerStat playerStat)
    {
        _playerStat = playerStat;
        UpdateReloadTime();
    }

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

        // PlayerStat 참조가 없으면 PlayerFire에서 가져오기
        if (_playerStat == null && _playerFire != null)
        {
            _playerFire.TryGetComponent(out _playerStat);
        }

        UpdateReloadTime();
    }
    #endregion

    #region 비공개 메서드
    /// <summary>
    /// 재장전 시간 정보 업데이트
    /// </summary>
    private void UpdateReloadTime()
    {
        if (_playerStat != null)
        {
            _reloadDuration = _playerStat.ReloadTime;
        }
    }

    /// <summary>
    /// 재장전 상태 변경 이벤트 핸들러
    /// </summary>
    private void HandleReloadStateChanged(bool isReloading)
    {
        if (isReloading)
        {
            ShowUI();
        }
        else
        {
            HideUI();
        }
    }

    /// <summary>
    /// 재장전 진행도 변경 이벤트 핸들러
    /// </summary>
    private void HandleReloadProgressChanged(float progress)
    {
        if (_circularProgressImage != null)
        {
            _circularProgressImage.fillAmount = progress;
        }

        if (_reloadTimeText != null)
        {
            float remainingTime = _reloadDuration * (1 - progress);
            _reloadTimeText.text = string.Format(_reloadTextFormat, remainingTime);

            // 진행도가 완료되면 텍스트 색상 변경
            if (progress >= 0.99f)
            {
                _reloadTimeText.color = _completeColor;
            }
            else
            {
                _reloadTimeText.color = _normalColor;
            }
        }
    }

    /// <summary>
    /// 재장전 취소 이벤트 핸들러
    /// </summary>
    private void HandleReloadCancelled()
    {
        HideUI();
    }

    /// <summary>
    /// UI 표시
    /// </summary>
    private void ShowUI()
    {
        if (_isVisible) return;

        _isVisible = true;

        // 진행 중인 페이드 코루틴이 있다면 중지
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        // 페이드 인 애니메이션
        _fadeCoroutine = StartCoroutine(FadeCanvasGroup(_canvasGroup, 1f, _fadeInDuration));
    }

    /// <summary>
    /// UI 숨기기
    /// </summary>
    private void HideUI()
    {
        if (!_isVisible) return;

        _isVisible = false;

        // 진행 중인 페이드 코루틴이 있다면 중지
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        // 페이드 아웃 애니메이션
        _fadeCoroutine = StartCoroutine(FadeCanvasGroup(_canvasGroup, 0f, _fadeOutDuration));
    }

    /// <summary>
    /// CanvasGroup 페이드 애니메이션 코루틴
    /// </summary>
    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float targetAlpha, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        _fadeCoroutine = null;
    }
    #endregion
}