using System.Collections;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    #region Fields
    [Header("폭발 설정")]
    [SerializeField] private GameObject _explosionEffectPrefab;
    [SerializeField] private float _explosionDelay = 5f; // 자동 폭발 시간 (5초 후 자동 폭발)
    [SerializeField] private float _explosionEffectDuration = 2f; // 폭발 이펙트 지속 시간

    // 풀링 관련 필드
    private float _explosionTimer = 0f;
    private bool _hasExploded = false;
    private static bool _isExplosionEffectPoolInitialized = false;
    #endregion

    #region Unity Event Functions
    private void OnEnable()
    {
        // 오브젝트가 활성화될 때 초기화
        _explosionTimer = 0f;
        _hasExploded = false;

        // 폭발 이펙트 풀 초기화 (처음 사용 시)
        InitializeExplosionEffectPool();
    }

    private void Update()
    {
        // 자동 폭발 타이머
        if (!_hasExploded)
        {
            _explosionTimer += Time.deltaTime;
            if (_explosionTimer >= _explosionDelay)
            {
                Explode();
            }
        }
    }

    // 충돌했을 때
    private void OnCollisionEnter(Collision collision)
    {
        Explode();
    }
    #endregion

    #region Public Methods
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
            GameObject effectObject = ObjectPoolManager.Instance.GetFromPool(_explosionEffectPrefab.name);
            if (effectObject == null)
            {
                // 풀이 초기화되지 않았으면 일반 인스턴스 생성
                effectObject = Instantiate(_explosionEffectPrefab);
            }

            effectObject.transform.position = transform.position;
            effectObject.SetActive(true);

            // 자동 반환 설정 (코루틴 대신 DOTween 등으로 대체할 수 있음)
            StartCoroutine(ReturnEffectToPool(effectObject, _explosionEffectDuration));
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

    #region Private Methods
    /// <summary>
    /// 폭발 이펙트 오브젝트 풀 초기화
    /// </summary>
    private void InitializeExplosionEffectPool()
    {
        if (_explosionEffectPrefab != null && !_isExplosionEffectPoolInitialized)
        {
            ObjectPoolManager.Instance.InitializePool(_explosionEffectPrefab, 5);
            _isExplosionEffectPoolInitialized = true;
            Debug.Log("폭발 이펙트 오브젝트 풀 초기화 완료");
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
