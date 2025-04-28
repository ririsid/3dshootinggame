using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 오브젝트 풀링을 관리하는 매니저 클래스
/// </summary>
public class ObjectPoolManager : Singleton<ObjectPoolManager>
{
    #region 필드
    // 풀링할 오브젝트 종류별로 딕셔너리 관리
    private Dictionary<string, Queue<GameObject>> _poolDictionary = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, GameObject> _prefabDictionary = new Dictionary<string, GameObject>();
    private Dictionary<string, Transform> _poolParentDictionary = new Dictionary<string, Transform>();

    [Header("기본 풀 크기")]
    [SerializeField] private int _defaultPoolSize = 10;

    // 모든 풀의 부모 객체
    private Transform _poolRoot;
    #endregion

    #region Unity 이벤트 함수
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
    /// 오브젝트 풀 초기화
    /// </summary>
    /// <param name="prefab">풀링할 프리팹</param>
    /// <param name="size">풀 초기 크기</param>
    public void InitializePool(GameObject prefab, int size = 0)
    {
        string prefabName = prefab.name;

        if (_poolDictionary.ContainsKey(prefabName))
        {
            Debug.LogWarning($"이미 {prefabName} 풀이 초기화되어 있습니다.");
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
    /// 풀에서 오브젝트 가져오기
    /// </summary>
    /// <param name="prefabName">프리팹 이름</param>
    /// <returns>활성화된 오브젝트</returns>
    public GameObject GetFromPool(string prefabName)
    {
        // 풀이 초기화되지 않았다면 null 반환
        if (!_poolDictionary.ContainsKey(prefabName))
        {
            Debug.LogError($"{prefabName} 풀이 초기화되지 않았습니다.");
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
    /// 오브젝트를 풀로 반환
    /// </summary>
    /// <param name="gameObject">반환할 오브젝트</param>
    public void ReturnToPool(GameObject gameObject)
    {
        string prefabName = gameObject.name.Replace("(Clone)", "");

        // 풀이 초기화되지 않았다면 파괴
        if (!_poolDictionary.ContainsKey(prefabName))
        {
            Debug.LogWarning($"{prefabName} 풀이 존재하지 않습니다. 오브젝트를 파괴합니다.");
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
    /// 새 오브젝트 생성
    /// </summary>
    /// <param name="prefabName">프리팹 이름</param>
    /// <returns>생성된 비활성화 오브젝트</returns>
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