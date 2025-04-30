using UnityEngine;

/// <summary>
/// MonoBehaviour 기반 싱글톤 패턴을 제공하는 제네릭 클래스
/// </summary>
/// <typeparam name="T">싱글톤으로 구현할 MonoBehaviour 타입</typeparam>
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static object _lock = new object();
    private static bool _isApplicationQuitting = false;

    /// <summary>
    /// 싱글톤 인스턴스에 접근하기 위한 프로퍼티
    /// </summary>
    public static T Instance
    {
        get
        {
            // 애플리케이션이 종료 중이면 null 반환
            if (_isApplicationQuitting)
            {
                Debug.LogWarning($"[Singleton] {typeof(T)} 인스턴스가 이미 파괴되었거나 애플리케이션이 종료 중입니다.");
                return null;
            }

            // 스레드 안전한 인스턴스 생성
            lock (_lock)
            {
                if (_instance == null)
                {
                    // 씬에서 기존 인스턴스 검색
                    _instance = (T)FindFirstObjectByType(typeof(T));

                    // 씬에 없으면 새로 생성
                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject();
                        _instance = singletonObject.AddComponent<T>();
                        singletonObject.name = $"[Singleton] {typeof(T).Name}";

                        // 씬 전환 시에도 유지
                        DontDestroyOnLoad(singletonObject);

                        Debug.Log($"[Singleton] {typeof(T).Name} 인스턴스 생성됨");
                    }
                }

                return _instance;
            }
        }
    }

    // 인스턴스 존재 여부를 확인하는 정적 속성 추가
    public static bool HasInstance => _instance != null && !_isApplicationQuitting;

    protected virtual void Awake()
    {
        // 중복 인스턴스 검사 및 제거
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"[Singleton] {typeof(T).Name}의 다른 인스턴스가 이미 존재합니다. 중복 인스턴스를 제거합니다.");
            Destroy(gameObject);
            return;
        }

        _instance = this as T;
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnApplicationQuit()
    {
        _isApplicationQuitting = true;
    }

    // 게임 시작 시 정적 변수 초기화
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticValues()
    {
        _isApplicationQuitting = false;
        _instance = null;
    }
}