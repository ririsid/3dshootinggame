using System.Collections;
using UnityEngine;

public class Barrel : MonoBehaviour, IDamageable
{
    [Header("스탯 설정")]
    [SerializeField] private int _maxHealth = 50;
    [SerializeField] private int _currentHealth;

    [Header("폭발 설정")]
    [SerializeField] private float _explosionRadius = 5f;
    [SerializeField] private int _explosionDamage = 30;
    [SerializeField] private float _explosionForce = 1000f;
    [SerializeField] private float _upwardModifier = 0.5f;
    [SerializeField] private LayerMask _damageableLayerMask;
    [SerializeField] private float _destroyDelay = 2f;

    [Header("이펙트")]
    [SerializeField] private GameObject _explosionEffectPrefab;

    private bool _isExploded = false;
    private Rigidbody _rigidbody;

    #region Unity 이벤트 함수
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        _currentHealth = _maxHealth;
    }
    #endregion

    /// <summary>
    /// 데미지를 받아 체력을 감소시키고, 체력이 0 이하가 되면 폭발합니다.
    /// </summary>
    /// <param name="damage">받은 데미지 정보</param>
    public void TakeDamage(Damage damage)
    {
        if (_isExploded) return;

        _currentHealth -= damage.Value;
        Debug.Log($"배럴이 {damage.From.name}에게 {damage.Value}의 데미지를 받았습니다. 현재 체력: {_currentHealth}");

        if (_currentHealth <= 0)
        {
            Explode();
        }
    }

    /// <summary>
    /// 배럴이 폭발하여 주변에 피해를 주고 물리 효과를 적용합니다.
    /// </summary>
    private void Explode()
    {
        if (_isExploded) return;
        _isExploded = true;

        // 폭발 이펙트 재생
        if (_explosionEffectPrefab != null)
        {
            Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // 주변 오브젝트에 데미지 적용
        Collider[] colliders = Physics.OverlapSphere(transform.position, _explosionRadius, _damageableLayerMask);
        foreach (Collider collider in colliders)
        {
            // 자기 자신에게는 데미지를 주지 않음
            if (collider.gameObject == gameObject) continue;

            // 데미지 적용
            if (collider.TryGetComponent<IDamageable>(out var damageable))
            {
                Damage explosionDamage = new()
                {
                    Value = _explosionDamage,
                    From = gameObject
                };
                damageable.TakeDamage(explosionDamage);
            }

            // 물리 효과 적용
            if (collider.TryGetComponent<Rigidbody>(out var rigidbody))
            {
                rigidbody.AddExplosionForce(_explosionForce, transform.position, _explosionRadius, _upwardModifier);
            }
        }

        // 배럴 자체에 물리 효과 적용하여 날아가도록 함
        if (_rigidbody != null)
        {
            // 랜덤 방향 요소 추가
            Vector3 randomDirection = new Vector3(
                Random.Range(-0.5f, 0.5f),
                Random.Range(0.05f, 0.15f), // 위쪽 방향은 약간 유지
                Random.Range(-0.5f, 0.5f)
            ).normalized;

            // 데미지를 입은 방향도 고려(마지막으로 데미지를 준 오브젝트 방향의 반대로 튕김)
            Vector3 explosionPosition = transform.position - randomDirection * 0.5f;

            // 폭발력 적용 (랜덤성과 약간의 회전도 추가)
            _rigidbody.AddExplosionForce(_explosionForce * Random.Range(0.8f, 1.2f),
                                        explosionPosition,
                                        _explosionRadius,
                                        _upwardModifier * Random.Range(0.8f, 1.2f));

            // 랜덤한 회전력 추가
            _rigidbody.AddTorque(new Vector3(
                Random.Range(-_explosionForce, _explosionForce),
                Random.Range(-_explosionForce, _explosionForce),
                Random.Range(-_explosionForce, _explosionForce)
            ) * 0.1f);

            // 배럴을 다른 오브젝트와 충돌하지 않도록 설정
            Physics.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Player"), true);
            Physics.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), true);
            // 지면과는 계속 충돌하도록 유지
        }

        // 일정 시간 후에 오브젝트 파괴
        StartCoroutine(DestroyAfterDelay());
    }

    /// <summary>
    /// 일정 시간 후에 오브젝트를 파괴합니다.
    /// </summary>
    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(_destroyDelay);
        Destroy(gameObject);
    }

    /// <summary>
    /// 디버그 모드에서 폭발 범위를 시각화합니다.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
    }
}
