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
    /// <summary>
    /// 재장전 진행도를 표시하는 원형 이미지
    /// </summary>
    [SerializeField] private Image _circularProgressImage;

    /// <summary>
    /// 재장전 남은 시간을 표시하는 텍스트
    /// </summary>
    [SerializeField] private TextMeshProUGUI _reloadTimeText;

    /// <summary>
    /// UI 페이드 효과를 위한 캔버스 그룹
    /// </summary>
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("플레이어 참조")]
    /// <summary>
    /// 플레이어 발사 컴포넌트 참조
    /// </summary>
    [SerializeField] private PlayerFire _playerFire;

    /// <summary>
    /// 플레이어 스탯 컴포넌트 참조
    /// </summary>
    [SerializeField] private PlayerStat _playerStat;

    [Header("애니메이션 설정")]
    /// <summary>
    /// UI가 나타나는 시간(초)
    /// </summary>
    [SerializeField] private float _fadeInDuration = 0.2f;

    /// <summary>
    /// UI가 사라지는 시간(초)
    /// </summary>
    [SerializeField] private float _fadeOutDuration = 0.3f;

    [Header("텍스트 설정")]
    /// <summary>
    /// 재장전 시간 표시 형식 (예: "{0:0.0}s")
    /// </summary>
    [SerializeField] private string _reloadTextFormat = "{0:0.0}s";

    /// <summary>
    /// 일반 상태 텍스트 색상
    /// </summary>
    [SerializeField] private Color _normalColor = Color.black;

    /// <summary>
    /// 재장전 완료 상태 텍스트 색상
    /// </summary>
    [SerializeField] private Color _completeColor = Color.green;

    /// <summary>
    /// 총 재장전 시간(초)
    /// </summary>
    private float _reloadDuration;

    /// <summary>
    /// UI 표시 상태
    /// </summary>
    private bool _isVisible;

    /// <summary>
    /// 현재 실행 중인 페이드 코루틴
    /// </summary>
    private Coroutine _fadeCoroutine;
    #endregion

    #region Unity 이벤트 함수
    /// <summary>
    /// 컴포넌트 초기화를 수행합니다.
    /// </summary>
    private void Awake()
    {
        // CanvasGroup 컴포넌트가 없으면 자동으로 추가
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // 초기 상태 설정 (숨김)
        _canvasGroup.alpha = 0f;
        _isVisible = false;
    }

    /// <summary>
    /// 시작 시 필요한 참조를 찾고 초기 설정을 수행합니다.
    /// </summary>
    private void Start()
    {
        // PlayerFire 컴포넌트 참조가 없으면 찾기
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
            _playerFire.TryGetComponent(out _playerStat);
        }

        // 재장전 시간 초기화
        UpdateReloadTime();
    }
    #endregion

    #region 이벤트 등록
    /// <summary>
    /// 이벤트 구독을 등록합니다.
    /// </summary>
    protected override void RegisterEvents()
    {
        if (_playerFire != null)
        {
            _playerFire.OnReloadStateChanged += HandleReloadStateChanged;
            _playerFire.OnReloadProgressChanged += HandleReloadProgressChanged;
            _playerFire.OnReloadCancelled += HandleReloadCancelled;
        }
    }

    /// <summary>
    /// 이벤트 구독을 해제합니다.
    /// </summary>
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
    /// <param name="playerStat">설정할 PlayerStat 컴포넌트</param>
    public void SetPlayerStat(PlayerStat playerStat)
    {
        _playerStat = playerStat;
        UpdateReloadTime();
    }

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
    /// <param name="isReloading">재장전 중인지 여부</param>
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
    /// <param name="progress">진행도 (0~1 사이의 값)</param>
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
    /// <param name="canvasGroup">페이드 효과를 적용할 캔버스 그룹</param>
    /// <param name="targetAlpha">목표 알파값</param>
    /// <param name="duration">페이드 지속 시간</param>
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