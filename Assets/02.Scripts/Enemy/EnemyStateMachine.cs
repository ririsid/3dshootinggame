using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적의 상태 머신을 관리하는 클래스입니다.
/// </summary>
public class EnemyStateMachine : MonoBehaviour
{
    #region 필드
    /// <summary>
    /// 초기 상태
    /// </summary>
    [SerializeField] private EnemyState _initialState = EnemyState.Idle;

    /// <summary>
    /// 가능한 모든 상태 목록
    /// </summary>
    private Dictionary<EnemyState, IEnemyState> _states = new Dictionary<EnemyState, IEnemyState>();

    /// <summary>
    /// 현재 활성화된 상태
    /// </summary>
    private IEnemyState _currentState;

    /// <summary>
    /// 현재 상태의 타입
    /// </summary>
    private EnemyState _currentStateType;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 현재 상태의 타입을 반환합니다.
    /// </summary>
    public EnemyState CurrentStateType => _currentStateType;
    #endregion

    #region 공개 메서드
    /// <summary>
    /// 상태 머신을 초기화합니다.
    /// </summary>
    /// <param name="states">상태 타입과 구현 객체의 사전</param>
    public void Initialize(Dictionary<EnemyState, IEnemyState> states)
    {
        _states = states;
        SetState(_initialState);
    }

    /// <summary>
    /// 상태를 업데이트합니다.
    /// </summary>
    public void UpdateState()
    {
        if (_currentState == null) return;

        _currentState.Update();
        _currentState.CheckTransitions();
    }

    /// <summary>
    /// 상태를 변경합니다.
    /// </summary>
    /// <param name="newStateType">새로운 상태 타입</param>
    public void SetState(EnemyState newStateType)
    {
        // 상태가 같고 이미 초기화된 상태인 경우에만 변경하지 않음
        if (_currentState != null && _currentStateType == newStateType)
            return;

        // 상태가 존재하지 않는 경우 변경하지 않음
        if (!_states.ContainsKey(newStateType))
        {
            if (Debug.isDebugBuild)
            {
                Debug.LogError($"존재하지 않는 상태({newStateType})로 변경할 수 없습니다.");
            }
            return;
        }

        // 개발 빌드에서만 상태 변경 로그 출력
        if (Debug.isDebugBuild)
        {
            Debug.Log($"적 상태 변경: {_currentStateType} -> {newStateType}");
        }

        // 현재 상태가 있으면 종료
        if (_currentState != null)
            _currentState.Exit();

        _currentStateType = newStateType;
        _currentState = _states[_currentStateType];
        _currentState.Enter();
    }
    #endregion
}