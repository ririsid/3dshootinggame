using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 오브젝트 풀링을 관리하는 매니저 클래스입니다.
/// </summary>
/// <remarks>
/// 게임 내에서 자주 생성 및 파괴되는 오브젝트를 효율적으로 관리하여 성능을 최적화합니다.
/// 싱글톤 패턴으로 구현되어 전역에서 접근 가능합니다.
/// </remarks>
public class ObjectPoolManager : Singleton<ObjectPoolManager>
{
    #region 필드
    // 풀링할 오브젝트 종류별로 딕셔너리 관리
    private Dictionary<string, Queue<GameObject>> _poolDictionary = new Dictionary<string, Queue<GameObject>>(); // 비활성화된 오브젝트들을 담는 큐 모음
    private Dictionary<string, GameObject> _prefabDictionary = new Dictionary<string, GameObject>(); // 프리팹 원본 참조 모음
    private Dictionary<string, Transform> _poolParentDictionary = new Dictionary<string, Transform>(); // 풀 부모 트랜스폼 모음

    [Header("기본 풀 크기")]
    [SerializeField] private int _defaultPoolSize = 10; // 풀 초기화 시 기본 생성 개수

    // 모든 풀의 부모 객체
    private Transform _poolRoot; // 모든 풀의 최상위 부모 트랜스폼
    #endregion

    #region Unity 이벤트 함수
    /// <summary>
    /// 컴포넌트 초기화 시 호출됩니다.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();

        // 풀의 루트 객체 생성
        _poolRoot = new GameObject("PoolRoot").transform;
        _poolRoot.parent = transform;
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// 오브젝트 풀을 초기화합니다.
    /// </summary>
    /// <param name="prefab">풀링할 프리팹</param>
    /// <param name="size">풀 초기 크기 (0 이하일 경우 기본값 사용)</param>
    /// <remarks>
    /// 프리팹의 이름을 키로 사용하여 풀을 생성하고 초기 오브젝트를 미리 생성합니다.
    /// 동일한 프리팹에 대해 여러 번 호출해도 한 번만 초기화됩니다.
    /// </remarks>
    public void InitializePool(GameObject prefab, int size = 0)
    {
        string prefabName = prefab.name;

        if (_poolDictionary.ContainsKey(prefabName))
        {
            // 이미 초기화된 풀은 무시
            return;
        }

        if (size <= 0) size = _defaultPoolSize;

        // 풀 부모 객체 생성
        Transform poolParent = new GameObject($"Pool_{prefabName}").transform;
        poolParent.parent = _poolRoot;
        _poolParentDictionary[prefabName] = poolParent;

        _poolDictionary[prefabName] = new Queue<GameObject>();
        _prefabDictionary[prefabName] = prefab;

        // 초기 오브젝트 생성
        for (int i = 0; i < size; i++)
        {
            CreateNewObject(prefabName);
        }
    }

    /// <summary>
    /// 풀에서 활성화된 오브젝트를 가져옵니다.
    /// </summary>
    /// <param name="prefabName">가져올 오브젝트의 프리팹 이름</param>
    /// <returns>풀에서 가져온 활성화된 오브젝트 (풀이 초기화되지 않은 경우 null)</returns>
    /// <remarks>
    /// 풀에 사용 가능한 오브젝트가 없으면 새로 생성합니다.
    /// 풀이 초기화되지 않은 경우 로그 오류를 출력하고 null을 반환합니다.
    /// </remarks>
    public GameObject GetFromPool(string prefabName)
    {
        // 풀이 초기화되지 않았다면 null 반환
        if (!_poolDictionary.ContainsKey(prefabName))
        {
            Debug.LogError($"풀링 오류: {prefabName} 풀이 초기화되지 않았습니다.");
            return null;
        }

        // 풀에 오브젝트가 없으면 새로 생성
        if (_poolDictionary[prefabName].Count == 0)
        {
            CreateNewObject(prefabName);
        }

        // 풀에서 오브젝트 꺼내기
        GameObject obj = _poolDictionary[prefabName].Dequeue();
        obj.SetActive(true);

        return obj;
    }

    /// <summary>
    /// 사용이 끝난 오브젝트를 풀로 반환합니다.
    /// </summary>
    /// <param name="gameObject">풀로 반환할 게임 오브젝트</param>
    /// <remarks>
    /// 오브젝트를 비활성화하고 기본 상태로 초기화한 후 풀에 다시 추가합니다.
    /// 해당 오브젝트의 풀이 존재하지 않으면 오브젝트를 파괴합니다.
    /// </remarks>
    public void ReturnToPool(GameObject gameObject)
    {
        string prefabName = gameObject.name.Replace("(Clone)", "");

        // 풀이 초기화되지 않았다면 파괴
        if (!_poolDictionary.ContainsKey(prefabName))
        {
            Destroy(gameObject);
            return;
        }

        // 오브젝트 비활성화 및 풀의 부모 아래로 이동
        gameObject.SetActive(false);
        gameObject.transform.parent = _poolParentDictionary[prefabName];

        // 기본 상태로 초기화 (위치, 회전, 속도 등)
        gameObject.transform.localPosition = Vector3.zero;
        gameObject.transform.localRotation = Quaternion.identity;
        gameObject.transform.localScale = Vector3.one;

        if (gameObject.TryGetComponent<Rigidbody>(out Rigidbody rigidbody))
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }

        // 풀에 반환
        _poolDictionary[prefabName].Enqueue(gameObject);
    }
    #endregion

    #region 비공개 메서드
    /// <summary>
    /// 풀에 새 오브젝트를 생성합니다.
    /// </summary>
    /// <param name="prefabName">생성할 프리팹의 이름</param>
    /// <returns>생성된 비활성화 상태의 오브젝트</returns>
    /// <remarks>
    /// 생성된 오브젝트는 비활성화 상태로 풀에 추가됩니다.
    /// </remarks>
    private GameObject CreateNewObject(string prefabName)
    {
        GameObject prefab = _prefabDictionary[prefabName];
        Transform parent = _poolParentDictionary[prefabName];

        GameObject newObj = Instantiate(prefab, parent);
        newObj.name = prefab.name; // (Clone) 접미사 제거
        newObj.SetActive(false);

        _poolDictionary[prefabName].Enqueue(newObj);
        return newObj;
    }
    #endregion
}