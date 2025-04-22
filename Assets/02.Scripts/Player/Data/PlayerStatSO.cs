using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStatSO", menuName = "Scriptable Objects/PlayerStatSO")]
public class PlayerStatSO : ScriptableObject
{
    [Header("스태미너 설정")]
    [Tooltip("최대 스태미너")]
    public float maxStamina = 100f;

    [Tooltip("스태미너 회복 속도(초당)")]
    public float staminaRecoveryRate = 15f;

    [Tooltip("달릴 때 스태미너 소모 속도(초당)")]
    public float staminaUseRate = 5f;

    [Tooltip("구르기에 사용되는 스태미너 양")]
    public float rollStaminaCost = 20f;

    [Header("이동 설정")]
    [Tooltip("걷기 속도")]
    public float walkSpeed = 7f;

    [Tooltip("달리기 속도")]
    public float runSpeed = 12f;

    [Tooltip("이동 감지 임계값")]
    [Range(0f, 1f)]
    public float moveInputThreshold = 0.1f;

    [Tooltip("대각선 이동 정규화 임계값")]
    [Range(0f, 2f)]
    public float diagonalMovementNormalizeThreshold = 1.0f;

    [Header("점프 설정")]
    [Tooltip("점프 파워")]
    public float jumpPower = 5f;

    [Tooltip("최대 점프 횟수 (2 = 2단 점프)")]
    [Range(1, 5)]
    public int maxJumpCount = 2;

    [Header("구르기 설정")]
    [Tooltip("구르기 속도")]
    public float rollSpeed = 15f;

    [Tooltip("구르기 지속 시간")]
    public float rollDuration = 0.5f;

    [Tooltip("구르기 쿨다운 시간")]
    public float rollCooldown = 1f;

    [Tooltip("구르기 회전 속도 (도/초)")]
    public float rollRotationSpeed = 720f;

    [Tooltip("Y축 방향 초기화 값")]
    public float rollYAxisClearValue = 0f;

    [Tooltip("구르기 방향 결정 임계값")]
    [Range(-1f, 1f)]
    public float rollDirectionDotThreshold = 0f;

    [Header("벽 오르기 스태미너 설정")]
    [Tooltip("벽 오르기 중 스태미너 소모 속도(초당)")]
    public float wallClimbStaminaUseRate = 8f;

    [Tooltip("하강 시 스태미너 소모율 계수")]
    [Range(0f, 1f)]
    public float wallDescendStaminaFactor = 0.5f;

    [Tooltip("좌우 이동 시 스태미너 소모율 계수")]
    [Range(0f, 1f)]
    public float wallStrafeStaminaFactor = 0.6f;

    [Tooltip("입력 없을 때 스태미너 소모율 계수")]
    [Range(0f, 1f)]
    public float wallIdleStaminaFactor = 0.3f;

    [Header("벽 오르기 설정")]
    [Tooltip("벽 오르기 속도")]
    public float wallClimbSpeed = 5f;

    [Tooltip("벽 내려가기 속도")]
    public float wallDescendSpeed = 3f;

    [Tooltip("벽 좌우 이동 속도")]
    public float wallStrafeSpeed = 4f;

    [Tooltip("벽으로 인식할 최소 수직 각도 (0.7 = 약 45도)")]
    [Range(0f, 1f)]
    public float minWallNormalY = 0.7f;

    [Tooltip("벽 오르기 중 입력 감지 기준값")]
    [Range(0f, 1f)]
    public float wallInputThreshold = 0.1f;

    [Tooltip("벽에서 떨어질 거리 기준값")]
    public float wallMaxDistance = 1.5f;

    [Header("폭탄 설정")]
    [Tooltip("최대 폭탄 개수")]
    [Range(0, 10)]
    public int maxBombCount = 3;

    [Tooltip("폭탄 던지기 파워")]
    public float bombThrowPower = 15f;
}
