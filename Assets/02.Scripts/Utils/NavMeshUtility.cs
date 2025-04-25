using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NavMesh 작업에 관련된 유틸리티 기능들을 제공하는 클래스입니다.
/// </summary>
public static class NavMeshUtility
{
    /// <summary>
    /// NavMeshAgent 경로 설정의 성공 여부를 확인합니다.
    /// </summary>
    /// <param name="agent">확인할 NavMeshAgent</param>
    /// <param name="destination">설정할 목적지</param>
    /// <returns>경로 설정 성공 여부</returns>
    public static bool TrySetDestination(NavMeshAgent agent, Vector3 destination)
    {
        if (agent == null || !agent.isOnNavMesh || !agent.enabled)
            return false;

        return agent.SetDestination(destination);
    }

    /// <summary>
    /// 현재 위치에서 목적지까지의 거리를 계산합니다.
    /// </summary>
    /// <param name="agent">NavMeshAgent</param>
    /// <param name="destination">목적지 위치</param>
    /// <param name="useNavMeshPath">NavMesh 경로 거리 사용 여부</param>
    /// <returns>거리</returns>
    public static float GetDistanceTo(NavMeshAgent agent, Vector3 destination, bool useNavMeshPath = false)
    {
        if (useNavMeshPath && agent != null && agent.isOnNavMesh && agent.enabled)
        {
            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(destination, path))
            {
                // NavMesh 경로를 통한 거리 계산
                float distance = 0;
                Vector3[] corners = path.corners;
                for (int i = 1; i < corners.Length; i++)
                {
                    distance += Vector3.Distance(corners[i - 1], corners[i]);
                }
                return distance;
            }
        }

        // 직선 거리 반환
        return Vector3.Distance(agent.transform.position, destination);
    }

    /// <summary>
    /// 목적지에 도달했는지 확인합니다.
    /// </summary>
    /// <param name="agent">NavMeshAgent</param>
    /// <param name="destination">목적지 위치</param>
    /// <param name="stoppingDistance">정지 거리</param>
    /// <returns>도달 여부</returns>
    public static bool HasReachedDestination(NavMeshAgent agent, Vector3 destination, float stoppingDistance = 0.1f)
    {
        if (agent == null || !agent.isOnNavMesh || !agent.enabled)
            return false;

        if (agent.pathPending)
            return false;

        if (agent.remainingDistance > stoppingDistance)
            return false;

        if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.1f)
            return Vector3.Distance(agent.transform.position, destination) <= stoppingDistance + agent.radius;

        return false;
    }

    /// <summary>
    /// NavMeshAgent가 유효한 상태인지 확인합니다.
    /// </summary>
    /// <param name="agent">확인할 NavMeshAgent</param>
    /// <returns>유효 여부</returns>
    public static bool IsAgentValid(NavMeshAgent agent)
    {
        return agent != null && agent.isOnNavMesh && agent.enabled;
    }

    /// <summary>
    /// 특정 위치를 기준으로 시야 내에 있는지 확인합니다.
    /// </summary>
    /// <param name="observer">관찰자 위치</param>
    /// <param name="target">확인할 대상 위치</param>
    /// <param name="viewAngle">시야각 (도)</param>
    /// <param name="maxDistance">최대 거리</param>
    /// <param name="layerMask">레이캐스트 레이어 마스크</param>
    /// <returns>시야 내 여부</returns>
    public static bool IsInSight(Transform observer, Vector3 target, float viewAngle = 120f, float maxDistance = 10f, LayerMask? layerMask = null)
    {
        Vector3 directionToTarget = (target - observer.position).normalized;
        float distanceToTarget = Vector3.Distance(observer.position, target);

        if (distanceToTarget > maxDistance)
            return false;

        float angle = Vector3.Angle(observer.forward, directionToTarget);
        if (angle > viewAngle * 0.5f)
            return false;

        // 장애물 체크
        if (layerMask.HasValue)
        {
            if (Physics.Raycast(observer.position, directionToTarget, out RaycastHit hit, maxDistance, layerMask.Value))
            {
                if (Vector3.Distance(hit.point, target) > 0.5f) // 타겟과 충돌 지점이 충분히 가까운지 확인
                    return false;
            }
        }
        else
        {
            if (Physics.Raycast(observer.position, directionToTarget, out RaycastHit hit, maxDistance))
            {
                if (Vector3.Distance(hit.point, target) > 0.5f)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 월드 좌표를 NavMesh 표면 좌표로 변환합니다.
    /// </summary>
    /// <param name="worldPosition">월드 좌표</param>
    /// <param name="maxDistance">검사 최대 거리</param>
    /// <returns>NavMesh 표면 좌표 (찾지 못한 경우 원래 좌표 반환)</returns>
    public static Vector3 GetNavMeshPosition(Vector3 worldPosition, float maxDistance = 5f)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(worldPosition, out hit, maxDistance, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return worldPosition;
    }

    /// <summary>
    /// 특정 위치에서 NavMesh 경로 탐색이 가능한지 확인합니다.
    /// </summary>
    /// <param name="start">시작 위치</param>
    /// <param name="end">목표 위치</param>
    /// <returns>경로 탐색 가능 여부</returns>
    public static bool CanReachDestination(Vector3 start, Vector3 end)
    {
        NavMeshPath path = new NavMeshPath();
        return NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
    }
}