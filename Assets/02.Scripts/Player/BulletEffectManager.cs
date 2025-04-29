using System.Collections;
using UnityEngine;

/// <summary>
/// 총알 관련 이펙트(피격, 궤적 등)를 관리하는 컴포넌트
/// </summary>
public class BulletEffectManager : MonoBehaviour
{
    #region 필드
    [Header("피격 이펙트 설정")]
    [SerializeField] private ParticleSystem _bulletImpactEffectPrefab;
    [SerializeField] private float _bulletImpactEffectDuration = 1.5f;
    [SerializeField] private int _bulletImpactEffectPoolSize = 10;

    [Header("총알 궤적 설정")]
    [SerializeField] private LineRenderer _tracerBulletPrefab;
    [SerializeField] private float _tracerBulletDuration = 0.1f;
    [SerializeField] private int _tracerBulletPoolSize = 15;
    [SerializeField] private float _tracerBulletWidth = 0.05f;
    [SerializeField] private Material _tracerBulletMaterial;
    [SerializeField] private Color _tracerBulletColor = Color.yellow;
    [SerializeField] private AnimationCurve _tracerAlphaOverLifetime;

    private bool _isBulletImpactEffectPoolInitialized = false;
    private bool _isTracerBulletPoolInitialized = false;
    #endregion

    #region Unity 이벤트 함수
    private void Awake()
    {
        InitializeEffectPools();
    }
    #endregion

    #region 비공개 메서드
    /// <summary>
    /// 이펙트 오브젝트 풀 초기화
    /// </summary>
    private void InitializeEffectPools()
    {
        InitializeBulletImpactEffectPool();
        InitializeTracerBulletPool();
    }

    /// <summary>
    /// 피격 이펙트 오브젝트 풀 초기화
    /// </summary>
    private void InitializeBulletImpactEffectPool()
    {
        if (_bulletImpactEffectPrefab != null && !_isBulletImpactEffectPoolInitialized)
        {
            GameObject bulletEffectObj = _bulletImpactEffectPrefab.gameObject;
            ObjectPoolManager.Instance.InitializePool(bulletEffectObj, _bulletImpactEffectPoolSize);
            _isBulletImpactEffectPoolInitialized = true;
            Debug.Log($"피격 이펙트 오브젝트 풀 초기화 완료 (크기: {_bulletImpactEffectPoolSize})");
        }
    }

    /// <summary>
    /// 총알 궤적 오브젝트 풀 초기화
    /// </summary>
    private void InitializeTracerBulletPool()
    {
        if (_tracerBulletPrefab != null && !_isTracerBulletPoolInitialized)
        {
            GameObject tracerObj = _tracerBulletPrefab.gameObject;
            ObjectPoolManager.Instance.InitializePool(tracerObj, _tracerBulletPoolSize);
            _isTracerBulletPoolInitialized = true;
            Debug.Log($"총알 궤적 오브젝트 풀 초기화 완료 (크기: {_tracerBulletPoolSize})");
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

    /// <summary>
    /// 총알 궤적 페이드아웃 처리
    /// </summary>
    private IEnumerator FadeOutTracer(LineRenderer lineRenderer, float duration)
    {
        float startTime = Time.time;
        float endTime = startTime + duration;

        // 초기 색상 저장
        Color originalColor = lineRenderer.material.GetColor("_TintColor");

        // 시간에 따라 알파값 감소
        while (Time.time < endTime && lineRenderer != null)
        {
            float normalizedTime = (Time.time - startTime) / duration;
            float alpha = _tracerAlphaOverLifetime != null
                ? _tracerAlphaOverLifetime.Evaluate(normalizedTime)
                : 1.0f - normalizedTime;

            Color newColor = originalColor;
            newColor.a = alpha;
            lineRenderer.material.SetColor("_TintColor", newColor);

            yield return null;
        }
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// 피격 이펙트 생성
    /// </summary>
    public void CreateBulletImpactEffect(Vector3 position, Vector3 normal)
    {
        if (_bulletImpactEffectPrefab == null) return;

        // 오브젝트 풀에서 이펙트 가져오기
        GameObject effectObj = ObjectPoolManager.Instance.GetFromPool(_bulletImpactEffectPrefab.name);

        if (effectObj == null)
        {
            // 풀에서 가져오기 실패 시 직접 생성
            ParticleSystem hitEffect = Instantiate(_bulletImpactEffectPrefab);
            hitEffect.transform.position = position;
            hitEffect.transform.forward = normal;
            hitEffect.Play();
            return;
        }

        // 풀에서 가져온 이펙트 설정
        effectObj.transform.position = position;
        effectObj.transform.forward = normal;
        effectObj.SetActive(true);

        // 파티클 시스템 재생
        if (effectObj.TryGetComponent<ParticleSystem>(out var particleSystem))
        {
            particleSystem.Play();
        }

        // 일정 시간 후 풀로 반환
        StartCoroutine(ReturnEffectToPool(effectObj, _bulletImpactEffectDuration));
    }

    /// <summary>
    /// 총알 궤적 이펙트 생성
    /// </summary>
    public void CreateTracerBulletEffect(Vector3 startPosition, Vector3 endPosition)
    {
        if (_tracerBulletPrefab == null) return;

        // 오브젝트 풀에서 궤적 이펙트 가져오기
        GameObject tracerObj = ObjectPoolManager.Instance.GetFromPool(_tracerBulletPrefab.name);

        if (tracerObj == null)
        {
            Debug.LogWarning("총알 궤적 오브젝트 풀에서 가져오기 실패");
            return;
        }

        // 활성화 및 위치 설정
        tracerObj.SetActive(true);

        // LineRenderer 컴포넌트 설정
        if (tracerObj.TryGetComponent<LineRenderer>(out var lineRenderer))
        {
            // 머티리얼이 할당된 경우 설정
            if (_tracerBulletMaterial != null)
            {
                lineRenderer.material = _tracerBulletMaterial;
            }

            // 색상 설정
            lineRenderer.startColor = _tracerBulletColor;
            lineRenderer.endColor = _tracerBulletColor;

            // 선 두께 설정
            lineRenderer.startWidth = _tracerBulletWidth;
            lineRenderer.endWidth = _tracerBulletWidth * 0.5f; // 끝으로 갈수록 얇아지도록

            // 선 위치 설정
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startPosition);
            lineRenderer.SetPosition(1, endPosition);

            // 페이드아웃 효과 시작
            StartCoroutine(FadeOutTracer(lineRenderer, _tracerBulletDuration));
        }

        // 일정 시간 후 풀로 반환
        StartCoroutine(ReturnEffectToPool(tracerObj, _tracerBulletDuration));
    }
    #endregion
}