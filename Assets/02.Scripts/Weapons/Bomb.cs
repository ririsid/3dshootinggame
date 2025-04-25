using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    #region 필드
    [Header("폭발 설정")]
    [SerializeField] private GameObject _explosionEffectPrefab;
    [SerializeField] private float _explosionEffectDuration = 2f; // 폭발 이펙트 지속 시간
    [SerializeField] private float _explosionRadius = 5f; // 폭발 반경
    [SerializeField] private float _explosionDamage = 50f; // 폭발 피해량
    [SerializeField] private LayerMask _damageableLayerMask; // 피해를 입힐 대상 레이어
    [SerializeField] private float _safetyDelay = 0.3f; // 초기 안전 딜레이

    // 풀링 관련 필드
    private bool _hasExploded = false;
    private static bool _isExplosionEffectPoolInitialized = false;

    // 충돌 관련 필드
    private float _activationTime; // 활성화 시간
    #endregion

    #region Unity 이벤트 함수
    private void Awake()
    {
        // Awake에서 풀 초기화 확인 (Start보다 일찍 실행)
        InitializeExplosionEffectPool();
    }

    private void OnEnable()
    {
        // 오브젝트가 활성화될 때 초기화
        _hasExploded = false;
        _activationTime = Time.time;
    }

    /// <summary>
    /// 충돌 감지 시 호출됩니다
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        // 안전 딜레이 확인 - 폭탄이 생성된 직후 충돌은 무시
        if (Time.time - _activationTime < _safetyDelay) return;

        // 충돌이 감지되면 폭발
        Explode();
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// 폭탄 폭발 처리
    /// </summary>
    public void Explode()
    {
        if (_hasExploded) return; // 이미 폭발했다면 무시
        _hasExploded = true;

        // 폭발 이펙트 생성 (오브젝트 풀링 적용)
        if (_explosionEffectPrefab != null)
        {
            // 효과 이름을 정확하게 가져옴
            string effectName = _explosionEffectPrefab.name;

            // 풀에서 오브젝트 가져오기
            GameObject effectObject = ObjectPoolManager.Instance.GetFromPool(effectName);
            if (effectObject == null)
            {
                // 풀이 아직 초기화되지 않았다면 재시도
                InitializeExplosionEffectPool();
                effectObject = ObjectPoolManager.Instance.GetFromPool(effectName);

                // 그래도 없으면 일반 인스턴스 생성
                if (effectObject == null)
                {
                    Debug.LogWarning($"{effectName} 풀 초기화 실패, 일반 인스턴스 생성");
                    effectObject = Instantiate(_explosionEffectPrefab);
                }
            }

            effectObject.transform.position = transform.position;
            effectObject.SetActive(true);

            // 자동 반환 설정 (코루틴 대신 DOTween 등으로 대체할 수 있음)
            StartCoroutine(ReturnEffectToPool(effectObject, _explosionEffectDuration));
        }

        // 주변 피해 처리
        Collider[] colliders = Physics.OverlapSphere(transform.position, _explosionRadius, _damageableLayerMask);
        foreach (Collider hitCollider in colliders)
        {
            // IDamageable 인터페이스를 가진 컴포넌트를 찾아서 피해를 줍니다.
            if (hitCollider.TryGetComponent<IDamageable>(out var damageable))
            {
                Damage damage = new()
                {
                    Value = (int)_explosionDamage,
                    From = gameObject
                };
                damageable.TakeDamage(damage);
            }
        }

        // 오브젝트 풀로 반환
        ReturnToPool();
    }

    /// <summary>
    /// 오브젝트 풀로 반환
    /// </summary>
    public void ReturnToPool()
    {
        // 객체는 풀로 반환
        ObjectPoolManager.Instance.ReturnToPool(gameObject);
    }
    #endregion

    #region 비공개 메서드
    /// <summary>
    /// 폭발 이펙트 오브젝트 풀 초기화
    /// </summary>
    private void InitializeExplosionEffectPool()
    {
        if (_explosionEffectPrefab != null && !_isExplosionEffectPoolInitialized)
        {
            // 오브젝트 풀 초기화 (이펙트는 여러 개 필요할 수 있으므로 여유있게 설정)
            ObjectPoolManager.Instance.InitializePool(_explosionEffectPrefab, 10);
            _isExplosionEffectPoolInitialized = true;
            Debug.Log($"{_explosionEffectPrefab.name} 폭발 이펙트 오브젝트 풀 초기화 완료");
        }
    }

    /// <summary>
    /// 지정된 시간 후 이펙트를 풀에 반환
    /// </summary>
    private IEnumerator ReturnEffectToPool(GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (effect != null && effect.activeSelf)
        {
            ObjectPoolManager.Instance.ReturnToPool(effect);
        }
    }
    #endregion
}
