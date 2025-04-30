using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 전투 기능을 관리하는 클래스
/// </summary>
public class PlayerCombat : MonoBehaviour, IDamageable
{
    #region 필드
    [Header("전투 설정")]
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private float _invincibilityDuration = 1f; // 피격 후 무적 시간
    [SerializeField] private float _damageFlashDuration = 0.1f; // 피격 시 깜빡임 효과 지속 시간
    [SerializeField] private Color _damageFlashColor = new Color(1f, 0f, 0f, 0.5f); // 피격 시 깜빡임 색상

    [Header("피격 효과")]
    [SerializeField] private Material _playerMaterial; // 플레이어 머티리얼
    [SerializeField] private AudioClip _damageSound; // 피격 사운드
    [SerializeField] private AudioSource _audioSource; // 오디오 소스

    [Header("체력 회복 설정")]
    [SerializeField] private float _healthRecoveryRate = 5f; // 초당 회복량
    [SerializeField] private float _healthRecoveryDelay = 5f; // 마지막 피격 후 회복 시작까지 대기 시간

    private int _currentHealth;
    private bool _isDead = false;
    private bool _isInvincible = false;
    private float _lastDamageTime = -999f;
    private Coroutine _invincibilityCoroutine;
    private Coroutine _damageFlashCoroutine;
    private Color _originalColor;

    // 컴포넌트 참조
    private PlayerStat _playerStat;
    private PlayerMove _playerMove;
    private PlayerFire _playerFire;
    #endregion

    #region 이벤트
    /// <summary>
    /// 체력 변경 시 발생하는 이벤트입니다. (현재 체력, 최대 체력)
    /// </summary>
    public event Action<int, int> OnHealthChanged;

    /// <summary>
    /// 플레이어 사망 시 발생하는 이벤트입니다.
    /// </summary>
    public event Action OnPlayerDied;

    /// <summary>
    /// 플레이어 피격 시 발생하는 이벤트입니다. 데미지 정보를 전달합니다.
    /// </summary>
    public event Action<Damage> OnPlayerHit;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 현재 체력
    /// </summary>
    public int CurrentHealth
    {
        get => _currentHealth;
        private set
        {
            int oldHealth = _currentHealth;
            _currentHealth = Mathf.Clamp(value, 0, _maxHealth);

            if (oldHealth != _currentHealth)
            {
                OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            }
        }
    }

    /// <summary>
    /// 최대 체력
    /// </summary>
    public int MaxHealth => _maxHealth;

    /// <summary>
    /// 플레이어가 사망 상태인지 여부
    /// </summary>
    public bool IsDead => _isDead;

    /// <summary>
    /// 플레이어가 무적 상태인지 여부
    /// </summary>
    public bool IsInvincible => _isInvincible;
    #endregion

    #region Unity 이벤트 함수
    private void Awake()
    {
        // 컴포넌트 참조 초기화
        _playerStat = GetComponent<PlayerStat>();
        _playerMove = GetComponent<PlayerMove>();
        _playerFire = GetComponent<PlayerFire>();

        if (_playerMaterial != null)
        {
            _originalColor = _playerMaterial.color;
        }

        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }
    }

    private void Start()
    {
        // 초기 체력 설정
        _currentHealth = _maxHealth;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    private void Update()
    {
        // 자동 체력 회복 처리
        HandleHealthRecovery();
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// IDamageable 인터페이스 구현 - 데미지를 처리합니다
    /// </summary>
    /// <param name="damage">적용할 피해의 정보</param>
    public void TakeDamage(Damage damage)
    {
        // 사망 상태거나 무적 상태면 데미지를 받지 않음
        if (_isDead || _isInvincible) return;

        // 피격 시간 기록
        _lastDamageTime = Time.time;

        // 데미지 적용
        CurrentHealth -= damage.Amount;

        // 피격 이벤트 발생
        OnPlayerHit?.Invoke(damage);

        // EventManager를 통해 이벤트 발생 (화면에 혈흔 효과 등을 위해)
        EventManager.Instance.TriggerPlayerDamaged(damage);

        // 피격 효과 적용
        ApplyDamageEffects();

        // 체력이 0 이하면 사망 처리
        if (CurrentHealth <= 0)
        {
            Die();
        }
        else
        {
            // 무적 시간 적용
            StartInvincibility();
        }
    }

    /// <summary>
    /// 체력을 회복합니다
    /// </summary>
    /// <param name="amount">회복할 체력량</param>
    public void HealHealth(int amount)
    {
        if (_isDead) return;

        CurrentHealth += amount;
    }
    #endregion

    #region 비공개 메서드
    /// <summary>
    /// 플레이어가 사망할 때 호출되는 메서드
    /// </summary>
    private void Die()
    {
        if (_isDead) return;

        _isDead = true;

        // 사망 효과 처리
        StopAllCoroutines();

        // 사망 이벤트 발생
        OnPlayerDied?.Invoke();

        // TODO: 사망 처리 추가 (애니메이션, 게임오버 처리 등)
    }

    /// <summary>
    /// 무적 상태를 시작합니다
    /// </summary>
    private void StartInvincibility()
    {
        if (_invincibilityCoroutine != null)
        {
            StopCoroutine(_invincibilityCoroutine);
        }
        _invincibilityCoroutine = StartCoroutine(InvincibilityCoroutine());
    }

    /// <summary>
    /// 피격 효과를 적용합니다
    /// </summary>
    private void ApplyDamageEffects()
    {
        // 피격 사운드 재생
        if (_audioSource != null && _damageSound != null)
        {
            _audioSource.PlayOneShot(_damageSound);
        }

        // 피격 시 깜빡임 효과
        if (_playerMaterial != null)
        {
            if (_damageFlashCoroutine != null)
            {
                StopCoroutine(_damageFlashCoroutine);
            }
            _damageFlashCoroutine = StartCoroutine(DamageFlashCoroutine());
        }

        // 필요하다면 여기에 카메라 흔들림 등 추가 효과 구현
    }

    /// <summary>
    /// 자동 체력 회복을 처리합니다
    /// </summary>
    private void HandleHealthRecovery()
    {
        // 최대 체력이면 회복하지 않음
        if (_currentHealth >= _maxHealth || _isDead) return;

        // 마지막 피격 후 일정 시간이 지났을 때만 체력 회복
        if (Time.time - _lastDamageTime < _healthRecoveryDelay) return;

        // 체력 회복
        float recoveryAmount = _healthRecoveryRate * Time.deltaTime;
        CurrentHealth += Mathf.CeilToInt(recoveryAmount);
    }

    /// <summary>
    /// 무적 상태를 처리하는 코루틴
    /// </summary>
    private IEnumerator InvincibilityCoroutine()
    {
        _isInvincible = true;
        yield return new WaitForSeconds(_invincibilityDuration);
        _isInvincible = false;
        _invincibilityCoroutine = null;
    }

    /// <summary>
    /// 피격 시 깜빡임 효과를 처리하는 코루틴
    /// </summary>
    private IEnumerator DamageFlashCoroutine()
    {
        _playerMaterial.color = _damageFlashColor;
        yield return new WaitForSeconds(_damageFlashDuration);
        _playerMaterial.color = _originalColor;
        _damageFlashCoroutine = null;
    }
    #endregion
}