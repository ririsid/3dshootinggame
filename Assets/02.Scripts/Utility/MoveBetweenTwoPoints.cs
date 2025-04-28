using UnityEngine;

/// <summary>
/// GameObject를 지정된 두 지점(_pointA와 _pointB) 사이에서 왕복 이동시킵니다.
/// </summary>
/// <remarks>
/// 이동 속도는 _speed 변수를 통해 조절할 수 있습니다.
/// Unity 에디터에서는 OnDrawGizmos를 통해 이동 경로가 시각적으로 표시됩니다.
/// </remarks>
public class MoveBetweenTwoPoints : MonoBehaviour
{
    /// <summary>
    /// 이동 시작 지점 Transform. Unity 에디터에서 설정합니다.
    /// </summary>
    [Header("이동 설정")]
    [SerializeField] private Transform _pointA; // 시작점

    /// <summary>
    /// 이동 끝 지점 Transform. Unity 에디터에서 설정합니다.
    /// </summary>
    [SerializeField] private Transform _pointB; // 끝점

    /// <summary>
    /// GameObject의 이동 속도. Unity 에디터에서 설정합니다.
    /// </summary>
    [SerializeField] private float _speed = 2.0f; // 이동 속도

    /// <summary>
    /// 현재 GameObject가 이동 목표로 삼는 지점(_pointA 또는 _pointB).
    /// </summary>
    private Transform _target;

    /// <summary>
    /// 컴포넌트 초기화 시 호출됩니다. 초기 목표 지점을 _pointB로 설정합니다.
    /// </summary>
    void Start()
    {
        // 초기 목표 지점 설정
        _target = _pointB;
    }

    /// <summary>
    /// 매 프레임 호출됩니다. GameObject를 _target 지점으로 이동시키고,
    /// 목표 지점에 도달하면 _target을 다른 지점으로 변경합니다.
    /// </summary>
    void Update()
    {
        // 목표 지점과 현재 위치 사이의 거리 계산
        float step = _speed * Time.deltaTime;
        // 목표 지점으로 이동
        transform.position = Vector3.MoveTowards(transform.position, _target.position, step);

        // 목표 지점에 도달했는지 확인
        if (Vector3.Distance(transform.position, _target.position) < 0.001f)
        {
            // 목표 지점 변경
            _target = _target == _pointB ? _pointA : _pointB;
        }
    }

    /// <summary>
    /// Unity 에디터에서 Gizmos를 그릴 때 호출됩니다.
    /// _pointA와 _pointB 사이에 선을 그리고 각 지점에 와이어 스피어를 그려 이동 경로를 시각화합니다.
    /// </summary>
    void OnDrawGizmos()
    {
        if (_pointA != null && _pointB != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(_pointA.position, _pointB.position);
            Gizmos.DrawWireSphere(_pointA.position, 0.3f);
            Gizmos.DrawWireSphere(_pointB.position, 0.3f);
        }
    }
}