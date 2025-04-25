using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NavMesh 작업에 관련된 유틸리티 기능들을 제공하는 클래스입니다.
/// </summary>
public static class NavMeshUtility
{
    #region 에이전트 관련 기능

    /// <summary>
    /// NavMeshAgent 경로 설정의 성공 여부를 확인합니다.
    /// </summary>
    /// <param name="agent">확인할 NavMeshAgent</param>
    /// <param name="destination">설정할 목적지</param>
    /// <returns>경로 설정 성공 여부</returns>
    public static bool TrySetDestination(NavMeshAgent agent, Vector3 destination)
    {
        if (!IsAgentValid(agent))
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
        if (!IsAgentValid(agent))
            return 0f;

        // NavMesh 표면 좌표로 변환
        Vector3 agentPosition = GetNavMeshPosition(agent.transform.position);
        Vector3 navMeshDestination = GetNavMeshPosition(destination);

        if (useNavMeshPath)
        {
            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(navMeshDestination, path))
            {
                return CalculatePathLength(path);
            }
        }

        // 직선 거리 반환
        return Vector3.Distance(agentPosition, navMeshDestination);
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
        if (!IsAgentValid(agent))
            return false;

        if (agent.pathPending)
            return false;

        if (agent.remainingDistance > stoppingDistance)
            return false;

        if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.1f)
        {
            return GetDistanceTo(agent, destination) <= stoppingDistance + agent.radius;
        }

        return false;
    }

    /// <summary>
    /// NavMeshAgent가 유효한 상태인지 확인합니다.
    /// </summary>
    /// <param name="agent">확인할 NavMeshAgent</param>
    /// <returns>유효 여부</returns>
    public static bool IsAgentValid(NavMeshAgent agent)
    {
        return agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;
    }

    #endregion

    #region 인지 및 시야 기능

    /// <summary>
    /// 특정 위치를 기준으로 시야 내에 있는지 확인합니다.
    /// </summary>
    /// <param name="observer">관찰자 위치</param>
    /// <param name="target">확인할 대상 위치</param>
    /// <param name="viewAngle">시야각 (도)</param>
    /// <param name="maxDistance">최대 거리</param>
    /// <param name="layerMask">레이캐스트 레이어 마스크</param>
    /// <param name="targetThreshold">타겟과 충돌 지점 간 허용 오차</param>
    /// <returns>시야 내 여부</returns>
    public static bool IsInSight(Transform observer, Vector3 target, float viewAngle = 120f,
                                float maxDistance = 10f, LayerMask? layerMask = null, float targetThreshold = 0.5f)
    {
        if (observer == null) return false;

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
                if (Vector3.Distance(hit.point, target) > targetThreshold) // 타겟과 충돌 지점이 충분히 가까운지 확인
                    return false;
            }
        }
        else
        {
            if (Physics.Raycast(observer.position, directionToTarget, out RaycastHit hit, maxDistance))
            {
                if (Vector3.Distance(hit.point, target) > targetThreshold)
                    return false;
            }
        }

        return true;
    }

    #endregion

    #region 위치 변환 및 계산

    /// <summary>
    /// 월드 좌표를 NavMesh 표면 좌표로 변환합니다.
    /// </summary>
    /// <param name="worldPosition">월드 좌표</param>
    /// <param name="maxDistance">검사 최대 거리</param>
    /// <param name="areaMask">NavMesh 영역 마스크</param>
    /// <returns>NavMesh 표면 좌표 (찾지 못한 경우 원래 좌표 반환)</returns>
    public static Vector3 GetNavMeshPosition(Vector3 worldPosition, float maxDistance = 5f, int areaMask = NavMesh.AllAreas)
    {
        if (NavMesh.SamplePosition(worldPosition, out NavMeshHit hit, maxDistance, areaMask))
        {
            return hit.position;
        }
        return worldPosition;
    }

    /// <summary>
    /// 주어진 방향으로 NavMesh 경계까지의 거리를 계산합니다.
    /// </summary>
    /// <param name="origin">시작 위치</param>
    /// <param name="direction">방향 벡터 (정규화됨)</param>
    /// <param name="maxDistance">최대 검사 거리</param>
    /// <returns>NavMesh 경계까지의 거리 (경계가 없으면 maxDistance 반환)</returns>
    public static float GetNavMeshEdgeDistance(Vector3 origin, Vector3 direction, float maxDistance = 100f)
    {
        direction.Normalize();

        // NavMesh 표면 좌표로 변환
        Vector3 navMeshOrigin = GetNavMeshPosition(origin);

        NavMeshHit hit;
        // 경계 찾기
        if (NavMesh.FindClosestEdge(navMeshOrigin, out hit, NavMesh.AllAreas))
        {
            // 이미 경계에 있는 경우
            if (hit.distance < 0.1f)
            {
                return 0f;
            }
        }

        // 방향을 따라 NavMesh 위에 있는지 단계적으로 체크
        float distance = 0f;
        float step = 1.0f; // 1미터 단위로 체크

        while (distance < maxDistance)
        {
            distance += step;
            Vector3 testPoint = navMeshOrigin + direction * distance;

            // GetNavMeshPosition을 사용하여 NavMesh 표면 위치를 확인
            Vector3 navMeshPoint = GetNavMeshPosition(testPoint, 0.5f);

            // 원래 위치와 같다면 NavMesh 위에 없다는 의미
            if (navMeshPoint == testPoint)
            {
                // NavMesh 위에 없는 지점을 찾았으므로 이전 유효 거리 반환
                return distance - step;
            }
        }

        // 최대 거리 내에서 경계를 찾지 못한 경우
        return maxDistance;
    }

    #endregion

    #region 경로 탐색 및 검증

    /// <summary>
    /// 특정 위치에서 NavMesh 경로 탐색이 가능한지 확인합니다.
    /// </summary>
    /// <param name="start">시작 위치</param>
    /// <param name="end">목표 위치</param>
    /// <param name="pathLength">계산된 경로 길이 (탐색 불가능시 -1)</param>
    /// <param name="areaMask">NavMesh 영역 마스크</param>
    /// <returns>경로 탐색 가능 여부</returns>
    public static bool CanReachDestination(Vector3 start, Vector3 end, out float pathLength, int areaMask = NavMesh.AllAreas)
    {
        // NavMesh 표면 좌표로 변환
        Vector3 navMeshStart = GetNavMeshPosition(start, 5f, areaMask);
        Vector3 navMeshEnd = GetNavMeshPosition(end, 5f, areaMask);

        NavMeshPath path = new NavMeshPath();
        bool result = NavMesh.CalculatePath(navMeshStart, navMeshEnd, areaMask, path) && path.status == NavMeshPathStatus.PathComplete;

        pathLength = -1f;
        if (result)
        {
            pathLength = CalculatePathLength(path);
        }

        return result;
    }

    /// <summary>
    /// 특정 위치에서 NavMesh 경로 탐색이 가능한지 확인합니다.
    /// </summary>
    /// <param name="start">시작 위치</param>
    /// <param name="end">목표 위치</param>
    /// <param name="areaMask">NavMesh 영역 마스크</param>
    /// <returns>경로 탐색 가능 여부</returns>
    public static bool CanReachDestination(Vector3 start, Vector3 end, int areaMask = NavMesh.AllAreas)
    {
        // 경로 길이는 필요 없지만 오버로드된 메서드를 활용하여 코드 중복 제거
        return CanReachDestination(start, end, out _, areaMask);
    }

    #endregion

    #region 무작위 위치 생성

    /// <summary>
    /// 현재 위치에서 특정 각도와 거리 내의 무작위 위치를 NavMesh 상에서 찾습니다.
    /// </summary>
    /// <param name="currentPosition">현재 위치</param>
    /// <param name="rotationCenter">회전 중심 (일반적으로 적 객체의 위치)</param>
    /// <param name="angleRange">각도 범위 (-angleRange ~ +angleRange)</param>
    /// <param name="minDistance">최소 거리</param>
    /// <param name="maxDistance">최대 거리</param>
    /// <param name="layerMask">충돌 검사에 사용할 레이어 마스크</param>
    /// <returns>유효한 NavMesh 위치, 찾지 못한 경우 현재 위치 반환</returns>
    public static Vector3 GetRandomLookAtPosition(Vector3 currentPosition, Vector3 rotationCenter,
                                                 float angleRange = 120f, float minDistance = 3f,
                                                 float maxDistance = 15f, LayerMask? layerMask = null)
    {
        // 현재 forward 방향 기준으로 무작위 각도 생성
        float randomAngle = Random.Range(-angleRange, angleRange);

        // 현재 위치 기준으로 회전 방향 계산
        Vector3 currentForward = Quaternion.LookRotation(
            new Vector3(rotationCenter.x, 0, rotationCenter.z) -
            new Vector3(currentPosition.x, 0, currentPosition.z)
        ).eulerAngles;

        // 최종 회전 방향 계산
        Vector3 direction = Quaternion.Euler(0, currentForward.y + randomAngle, 0) * Vector3.forward;

        // 무작위 거리 계산
        float randomDistance = Random.Range(minDistance, maxDistance);

        // 목표 위치 계산
        Vector3 targetPosition = rotationCenter + direction * randomDistance;

        // NavMesh 표면 좌표로 변환
        Vector3 navMeshPosition = GetNavMeshPosition(targetPosition, maxDistance);

        // 장애물 체크
        if (layerMask.HasValue)
        {
            if (Physics.Linecast(rotationCenter, navMeshPosition, out RaycastHit rayHit, layerMask.Value))
            {
                // 장애물이 있는 경우, 장애물 위치 근처의 다른 NavMesh 위치 시도
                Vector3 obstacleAvoidPosition = rayHit.point - (navMeshPosition - rotationCenter).normalized * 0.5f;
                return GetNavMeshPosition(obstacleAvoidPosition, maxDistance / 2f);
            }
        }

        return navMeshPosition != targetPosition ? navMeshPosition : currentPosition;
    }

    /// <summary>
    /// 지정된 영역 내에서 NavMesh 상의 무작위 위치를 찾아 반환합니다.
    /// </summary>
    /// <param name="center">영역의 중심점</param>
    /// <param name="radius">중심점으로부터의 반경</param>
    /// <param name="areaMask">NavMesh 영역 마스크</param>
    /// <returns>NavMesh 상의 무작위 유효 위치, 실패 시 입력 중심점 반환</returns>
    public static Vector3 GetRandomPointInNavMesh(Vector3 center, float radius, int areaMask = NavMesh.AllAreas)
    {
        // 최대 30회 시도
        for (int i = 0; i < 30; i++)
        {
            // 원형 영역 내 무작위 위치 계산
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 randomPoint = center + new Vector3(randomCircle.x, 0f, randomCircle.y);

            // GetNavMeshPosition 메서드를 사용하여 NavMesh 표면 좌표로 변환
            Vector3 navMeshPoint = GetNavMeshPosition(randomPoint, radius, areaMask);

            // 원래 위치와 다른 경우(즉, NavMesh 상의 유효한 위치를 찾은 경우) 반환
            if (navMeshPoint != randomPoint)
            {
                return navMeshPoint;
            }
        }

        // 유효한 위치를 찾지 못한 경우 중심점의 NavMesh 위치 반환
        return GetNavMeshPosition(center, radius, areaMask);
    }

    #endregion

    #region 디버깅 도구

    /// <summary>
    /// NavMesh 경로를 시각적으로 디버깅합니다.
    /// </summary>
    /// <param name="start">시작 위치</param>
    /// <param name="end">목표 위치</param>
    /// <param name="color">경로 색상</param>
    /// <param name="duration">표시 지속 시간</param>
    /// <param name="areaMask">NavMesh 영역 마스크</param>
    public static void DebugDrawPath(Vector3 start, Vector3 end, Color color, float duration = 1.0f, int areaMask = NavMesh.AllAreas)
    {
        // NavMesh 표면 좌표로 변환
        Vector3 navMeshStart = GetNavMeshPosition(start, 5f, areaMask);
        Vector3 navMeshEnd = GetNavMeshPosition(end, 5f, areaMask);

        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(navMeshStart, navMeshEnd, areaMask, path))
        {
            Vector3[] corners = path.corners;
            for (int i = 1; i < corners.Length; i++)
            {
                Debug.DrawLine(corners[i - 1], corners[i], color, duration);
            }
        }
    }

    /// <summary>
    /// NavMeshAgent의 현재 경로를 시각적으로 디버깅합니다.
    /// </summary>
    /// <param name="agent">NavMeshAgent</param>
    /// <param name="color">경로 색상</param>
    /// <param name="duration">표시 지속 시간</param>
    public static void DebugDrawAgentPath(NavMeshAgent agent, Color color, float duration = 1.0f)
    {
        if (!IsAgentValid(agent)) return;

        NavMeshPath path = agent.path;
        Vector3[] corners = path.corners;
        for (int i = 1; i < corners.Length; i++)
        {
            Debug.DrawLine(corners[i - 1], corners[i], color, duration);
        }
    }

    #endregion

    #region 경로 계산 유틸리티

    /// <summary>
    /// NavMesh 경로의 총 길이를 계산합니다.
    /// </summary>
    /// <param name="path">길이를 계산할 NavMesh 경로</param>
    /// <returns>경로의 총 길이</returns>
    private static float CalculatePathLength(NavMeshPath path)
    {
        float pathLength = 0f;
        Vector3[] corners = path.corners;

        for (int i = 1; i < corners.Length; i++)
        {
            pathLength += Vector3.Distance(corners[i - 1], corners[i]);
        }

        return pathLength;
    }

    #endregion
}