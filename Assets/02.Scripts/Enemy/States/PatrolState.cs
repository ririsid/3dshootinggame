using UnityEngine;

/// <summary>
/// 적의 순찰 상태를 구현하는 클래스입니다.
/// </summary>
public class PatrolState : IEnemyState
{
    private Enemy _enemy;

    public PatrolState(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {
        // 시작 위치 설정
        Vector3? startPosOverride = null;
        if (!_enemy.EnemyPatrol.HasWaypoints)
        {
            startPosOverride = _enemy.StartPosition;
        }

        // NavMeshAgent 유효성 검사
        if (!_enemy.IsAgentValid())
        {
            // 유효하지 않은 경우 경고 로그 출력
            Debug.LogWarning("PatrolState: NavMeshAgent가 유효하지 않습니다!");
        }

        _enemy.EnemyPatrol.StartPatrol(_enemy.Agent, _enemy.ReturnSpeed, startPosOverride);
    }

    public void Exit()
    {
        _enemy.EnemyPatrol.StopPatrol();
    }

    public void Update()
    {
        // 순찰은 EnemyPatrol 컴포넌트에서 처리

        // 이동 방향으로 회전하는 로직 추가
        _enemy.RotateToMoveDirection();
    }

    public void CheckTransitions()
    {
        // NavMeshUtility를 사용하여 NavMesh 경로 거리 계산
        float distanceToPlayer = NavMeshUtility.GetDistanceTo(
            _enemy.Agent,
            _enemy.Player.transform.position,
            true // NavMesh 경로 사용
        );

        // NavMeshUtility를 사용하여 시야 내 플레이어 확인
        bool isPlayerInSight = NavMeshUtility.IsInSight(
            _enemy.transform,
            _enemy.Player.transform.position,
            _enemy.ViewAngle,
            _enemy.FindDistance // 최대 감지 거리
        );

        // 거리와 시야각을 모두 고려하여 상태 전환
        if (distanceToPlayer < _enemy.FindDistance && isPlayerInSight)
        {
            _enemy.SetState(EnemyState.Trace);
            return;
        }

        // NavMeshUtility를 사용하여 에이전트 유효성 검사
        bool isAgentValid = NavMeshUtility.IsAgentValid(_enemy.Agent);
        bool hasPath = _enemy.Agent != null && _enemy.Agent.hasPath;
        bool isStopped = _enemy.Agent != null && _enemy.Agent.isStopped;

        // 에이전트가 정지 상태이거나 경로가 설정되지 않은 경우만 Idle로 전환
        if (!isAgentValid || (hasPath == false && isStopped))
        {
            _enemy.SetState(EnemyState.Idle);
        }
    }
}