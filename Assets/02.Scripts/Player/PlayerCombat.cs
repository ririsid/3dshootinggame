using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 전투 기능을 관리하는 클래스
/// </summary>
public class PlayerCombat : MonoBehaviour, IDamageable
{
    #region 필드
    [Header("컴포넌트 참조")]
    [SerializeField] private PlayerStat _playerStat;

    [Header("피격 효과")]
    [SerializeField] private float _damageFlashDuration = 0.1f; // 피격 시 깜빡임 효과 지속 시간
    [SerializeField] private Color _damageFlashColor = new Color(1f, 0f, 0f, 0.5f); // 피격 시 깜빡임 색상
    [SerializeField] private Material _playerMaterial; // 플레이어 머티리얼
    [SerializeField] private AudioClip _damageSound; // 피격 사운드
    [SerializeField] private AudioSource _audioSource; // 오디오 소스

    private Coroutine _invincibilityCoroutine;
    private Coroutine _damageFlashCoroutine;
    private Color _originalColor;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 현재 체력 - PlayerStat에 위임
    /// </summary>
    public int CurrentHealth => _playerStat != null ? _playerStat.CurrentHealth : 0;

    /// <summary>
    /// 최대 체력 - PlayerStat에 위임
    /// </summary>
    public int MaxHealth => _playerStat != null ? _playerStat.MaxHealth : 0;

    /// <summary>
    /// 플레이어가 사망 상태인지 여부 - PlayerStat에 위임
    /// </summary>
    public bool IsDead => _playerStat != null && _playerStat.IsDead;

    /// <summary>
    /// 플레이어가 무적 상태인지 여부 - PlayerStat에 위임
    /// </summary>
    public bool IsInvincible => _playerStat != null && _playerStat.IsInvincible;
    #endregion

    #region Unity 이벤트 함수
    private void Awake()
    {
        if (_playerStat == null)
        {
            _playerStat = GetComponent<PlayerStat>();
        }

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
        // 이벤트 구독 설정
        if (_playerStat != null)
        {
            _playerStat.OnPlayerHit += (damage) =>
            {
                ApplyDamageEffects();
            };
        }
    }

    private void OnDestroy()
    {
        // 코루틴 정리
        if (_invincibilityCoroutine != null)
            StopCoroutine(_invincibilityCoroutine);

        if (_damageFlashCoroutine != null)
            StopCoroutine(_damageFlashCoroutine);
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// IDamageable 인터페이스 구현 - 데미지를 처리합니다
    /// </summary>
    /// <param name="damage">적용할 피해의 정보</param>
    public void TakeDamage(Damage damage)
    {
        if (_playerStat == null) return;

        // PlayerStat으로 데미지 처리 위임
        _playerStat.TakeDamage(damage);

        // 무적 시간 적용 - 피격 후 무적 처리는 여전히 이 클래스에서 담당
        if (!_playerStat.IsDead)
        {
            StartInvincibility();
        }

        // EventManager를 통해 이벤트 발생 (화면에 혈흔 효과 등을 위해)
        EventManager.Instance.TriggerPlayerDamaged(damage);
    }
    #endregion

    #region 비공개 메서드
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
    }

    /// <summary>
    /// 무적 상태를 처리하는 코루틴
    /// </summary>
    private IEnumerator InvincibilityCoroutine()
    {
        if (_playerStat == null) yield break;

        _playerStat.SetInvincible(true);
        yield return new WaitForSeconds(_playerStat.InvincibilityDuration);
        _playerStat.SetInvincible(false);
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