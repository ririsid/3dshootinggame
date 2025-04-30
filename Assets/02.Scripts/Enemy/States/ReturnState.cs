using System.Collections;
using UnityEngine;

/// <summary>
/// 적의 복귀 상태를 구현하는 클래스입니다.
/// </summary>
public class ReturnState : IEnemyState
{
    #region 필드
    /// <summary>
    /// Enemy 컴포넌트 참조
    /// </summary>
    private Enemy _enemy;

    /// <summary>
    /// 복귀 코루틴 참조
    /// </summary>
    private Coroutine _returnCoroutine;

    /// <summary>
    /// 복귀 타이머
    /// </summary>
    private float _returnTimer = 0f;
    #endregion

    #region 생성자
    /// <summary>
    /// ReturnState 생성자
    /// </summary>
    /// <param name="enemy">Enemy 컴포넌트 참조</param>
    public ReturnState(Enemy enemy)
    {
        _enemy = enemy;
    }
    #endregion

    #region IEnemyState 구현
    /// <summary>
    /// 복귀 상태에 진입할 때 호출됩니다.
    /// </summary>
    public void Enter()
    {
        _returnTimer = 0f;

        // NavMeshAgent 속도 설정 (NavMeshUtility 유효성 검사)
        if (_enemy.IsAgentValid())
        {
            _enemy.Agent.speed = _enemy.ReturnSpeed;
        }

        _returnCoroutine = _enemy.StartCoroutine(ReturnCoroutine());
    }

    /// <summary>
    /// 복귀 상태에서 나갈 때 호출됩니다.
    /// </summary>
    public void Exit()
    {
        if (_returnCoroutine != null)
        {
            _enemy.StopCoroutine(_returnCoroutine);
            _returnCoroutine = null;
        }
    }

    /// <summary>
    /// 복귀 상태의 매 프레임 호출되는 업데이트 함수입니다.
    /// </summary>
    public void Update()
    {
        // 코루틴에서 처리
        CheckTransitions();
    }

    /// <summary>
    /// 다른 상태로의 전환 조건을 확인합니다.
    /// </summary>
    public void CheckTransitions()
    {
        if (_enemy.Player == null) return;

        float distanceToPlayer = _enemy.GetDistanceToDestination(_enemy.Player.transform.position, true);

        // 원래 위치에 도달했으면 전략 패턴의 초기 상태로 전환
        if (_enemy.HasReachedDestination(_enemy.StartPosition) && _enemy.CharacterController.isGrounded)
        {
            _enemy.SetState(_enemy.BehaviorStrategy.GetInitialState());
        }
        else if (distanceToPlayer < _enemy.FindDistance)
        {
            // 시야 내에 플레이어가 있는지 확인
            if (_enemy.IsTargetInSight(_enemy.Player.transform.position, 120f, _enemy.FindDistance))
            {
                // 플레이어 감지 처리를 전략 패턴에 위임
                EnemyState nextState = _enemy.BehaviorStrategy.OnPlayerDetected(
                    _enemy,
                    _enemy.Player.transform.position,
                    distanceToPlayer
                );

                if (nextState != _enemy.GetComponent<EnemyStateMachine>().CurrentStateType)
                {
                    _enemy.SetState(nextState);
                }
            }
        }
    }
    #endregion

    #region 비공개 메서드
    /// <summary>
    /// 시작 위치로 복귀하는 코루틴입니다.
    /// </summary>
    private IEnumerator ReturnCoroutine()
    {
        // NavMeshUtility를 통해 목적지 설정
        if (!_enemy.SetDestination(_enemy.StartPosition))
        {
            // 시작 위치로 경로를 설정할 수 없는 경우 안전한 위치로 텔레포트 시도
            Vector3 safePosition = NavMeshUtility.GetNavMeshPosition(_enemy.StartPosition);
            _enemy.transform.position = safePosition;
            _enemy.transform.rotation = _enemy.StartRotation;

            // 전략 패턴의 초기 상태로 전환
            _enemy.SetState(_enemy.BehaviorStrategy.GetInitialState());
            yield break;
        }

        while (true)
        {
            _returnTimer += Time.deltaTime;

            // 최대 복귀 시간 초과 시 안전 장치 발동
            if (_returnTimer > _enemy.MaxReturnDuration)
            {
                // NavMeshAgent 작업을 NavMeshUtility를 통해 처리
                if (_enemy.IsAgentValid())
                {
                    _enemy.Agent.isStopped = true;
                    _enemy.Agent.ResetPath();
                }

                // NavMesh 상의 안전한 위치 찾기
                Vector3 safePosition = NavMeshUtility.GetNavMeshPosition(_enemy.StartPosition);
                _enemy.transform.position = safePosition;
                _enemy.transform.rotation = _enemy.StartRotation;

                // 전략 패턴의 초기 상태로 전환
                _enemy.SetState(_enemy.BehaviorStrategy.GetInitialState());
                yield break;
            }

            // 경로 디버깅 (Development Build에서만 활성화)
            if (Debug.isDebugBuild)
            {
                NavMeshUtility.DebugDrawAgentPath(_enemy.Agent, Color.blue, 0.1f);
            }

            // 이동 방향으로 회전
            _enemy.RotateToMoveDirection();

            // 유틸리티 메서드를 사용하여 목적지 도달 여부 확인
            if (_enemy.HasReachedDestination(_enemy.StartPosition, _enemy.Agent.stoppingDistance))
            {
                // 전략 패턴의 초기 상태로 전환
                _enemy.SetState(_enemy.BehaviorStrategy.GetInitialState());
                yield break;
            }

            // 주기적으로 경로 재계산 (5초마다)
            if (_returnTimer % 5f < Time.deltaTime)
            {
                if (_enemy.IsAgentValid())
                {
                    _enemy.SetDestination(_enemy.StartPosition);
                }
            }

            yield return null;
        }
    }
    #endregion
}