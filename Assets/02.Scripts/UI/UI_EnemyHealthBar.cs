using UnityEngine;

/// <summary>
/// 적 머리 위에 체력 바를 표시하는 클래스입니다.
/// </summary>
public class UI_EnemyHealthBar : MonoBehaviour
{
    [Header("참조")]
    /// <summary>
    /// 체력바가 따라갈 대상 트랜스폼
    /// </summary>
    [SerializeField] private Transform _targetTransform;

    /// <summary>
    /// 체력바 위치 오프셋
    /// </summary>
    [SerializeField] private Vector3 _offset = new Vector3(0, 1.5f, 0);

    /// <summary>
    /// 지연 체력바 컴포넌트
    /// </summary>
    [SerializeField] private DelayedHealthBar _healthBar;

    /// <summary>
    /// 메인 카메라 참조
    /// </summary>
    [SerializeField] private Camera _mainCamera;

    /// <summary>
    /// 적 전투 컴포넌트 참조
    /// </summary>
    [SerializeField] private EnemyCombat _enemyCombat;

    [Header("빌보드 설정")]
    /// <summary>
    /// 카메라를 향해 회전하는 빌보드 효과 사용 여부
    /// </summary>
    [Tooltip("카메라를 향해 회전하는 빌보드 효과 사용")]
    [SerializeField] private bool _useBillboard = true;

    /// <summary>
    /// Y축 회전만 적용 여부 (수직 방향은 고정)
    /// </summary>
    [Tooltip("Y축 회전만 적용 (수직 방향은 고정)")]
    [SerializeField] private bool _useYAxisOnly = true;

    #region Unity 이벤트 함수
    /// <summary>
    /// 초기화 작업을 수행합니다.
    /// </summary>
    private void Awake()
    {
        InitializeReferences();
    }

    /// <summary>
    /// 시작 시 체력 설정 및 이벤트 구독
    /// </summary>
    private void Start()
    {
        if (_healthBar != null && _enemyCombat != null)
        {
            // 초기 체력 설정
            _healthBar.MaxHealth = _enemyCombat.MaxHealth;
            _healthBar.CurrentHealth = _enemyCombat.CurrentHealth;

            // 체력 변경 이벤트 구독
            _enemyCombat.OnHealthChanged += UpdateHealthBar;
        }
        else
        {
            if (Debug.isDebugBuild)
            {
                Debug.LogWarning($"UI_EnemyHealthBar: 체력바 또는 적 전투 컴포넌트가 할당되지 않았습니다.");
            }
        }
    }

    /// <summary>
    /// 매 프레임 마지막에 체력바 위치와 회전 업데이트
    /// </summary>
    private void LateUpdate()
    {
        UpdatePosition();
        UpdateRotation();
    }

    /// <summary>
    /// 오브젝트 제거 시 이벤트 구독 해제
    /// </summary>
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (_enemyCombat != null)
        {
            _enemyCombat.OnHealthChanged -= UpdateHealthBar;
        }
    }
    #endregion

    #region 비공개 메서드
    /// <summary>
    /// 필요한 참조 초기화
    /// </summary>
    private void InitializeReferences()
    {
        // 메인 카메라가 할당되지 않았을 경우 자동으로 찾기
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        // 체력바 컴포넌트 찾기
        if (_healthBar == null)
        {
            _healthBar = GetComponentInChildren<DelayedHealthBar>();
            if (_healthBar == null && Debug.isDebugBuild)
            {
                Debug.LogWarning("UI_EnemyHealthBar: DelayedHealthBar 컴포넌트를 찾을 수 없습니다.");
            }
        }

        // 부모 오브젝트 확인 및 참조 설정
        Transform parentTransform = transform.parent;
        if (parentTransform == null)
        {
            if (Debug.isDebugBuild)
            {
                Debug.LogWarning("UI_EnemyHealthBar: 부모 오브젝트가 없습니다.");
            }
            return;
        }

        // 타겟 트랜스폼이 설정되지 않았다면 부모 트랜스폼으로 설정
        if (_targetTransform == null)
        {
            _targetTransform = parentTransform;
        }

        // 부모가 Enemy 컴포넌트를 가지고 있는지 확인
        Enemy parentEnemy = parentTransform.GetComponent<Enemy>();
        if (parentEnemy != null)
        {
            // EnemyCombat이 설정되지 않았다면 부모에서 찾기
            if (_enemyCombat == null)
            {
                _enemyCombat = parentTransform.GetComponent<EnemyCombat>();
                if (_enemyCombat == null && Debug.isDebugBuild)
                {
                    Debug.LogWarning("UI_EnemyHealthBar: 부모에서 EnemyCombat 컴포넌트를 찾을 수 없습니다.");
                }
            }
        }
        else if (_enemyCombat == null && Debug.isDebugBuild)
        {
            // Enemy가 아닌 다른 부모 오브젝트인 경우 경고 메시지 출력
            Debug.LogWarning("UI_EnemyHealthBar: 부모가 Enemy가 아니며 EnemyCombat이 설정되지 않았습니다.");
        }
    }

    /// <summary>
    /// 체력바 위치 업데이트
    /// </summary>
    private void UpdatePosition()
    {
        if (_targetTransform != null)
        {
            transform.position = _targetTransform.position + _offset;
        }
    }

    /// <summary>
    /// 체력바 회전 업데이트 (빌보드 효과)
    /// </summary>
    private void UpdateRotation()
    {
        if (!_useBillboard || _mainCamera == null) return;

        if (_useYAxisOnly)
        {
            // Y축 회전만 적용 (수직은 고정)
            Vector3 direction = _mainCamera.transform.position - transform.position;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
        else
        {
            // 완전한 빌보드 효과 (카메라를 정면으로)
            transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward,
                            _mainCamera.transform.rotation * Vector3.up);
        }
    }

    /// <summary>
    /// 체력바 업데이트
    /// </summary>
    /// <param name="currentHealth">현재 체력</param>
    /// <param name="maxHealth">최대 체력</param>
    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        Debug.Log($"UI_EnemyHealthBar: 체력 업데이트 - 현재 체력: {currentHealth}, 최대 체력: {maxHealth}");
        if (_healthBar != null)
        {
            _healthBar.CurrentHealth = currentHealth;
        }
    }
    #endregion
}