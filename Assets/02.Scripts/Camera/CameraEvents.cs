using System;
using UnityEngine;

/// <summary>
/// 카메라 관련 이벤트를 정의하는 정적 클래스입니다.
/// </summary>
public static class CameraEvents
{
    #region 이벤트 정의
    /// <summary>
    /// 카메라 모드가 변경되었을 때 발생하는 이벤트입니다.
    /// </summary>
    public static event Action<CameraMode> OnCameraModeChanged;

    /// <summary>
    /// 플레이어 회전이 변경되었을 때 발생하는 이벤트입니다.
    /// </summary>
    public static event Action<Vector2> OnPlayerRotationInput;
    #endregion

    #region 열거형
    /// <summary>
    /// 카메라 모드를 정의하는 열거형입니다.
    /// </summary>
    public enum CameraMode
    {
        /// <summary>
        /// 1인칭 시점
        /// </summary>
        FPS,

        /// <summary>
        /// 3인칭 시점
        /// </summary>
        TPS,

        /// <summary>
        /// 쿼터뷰 시점
        /// </summary>
        Quarter
    }
    #endregion

    #region 이벤트 메서드
    /// <summary>
    /// 카메라 모드 변경 이벤트를 발생시킵니다.
    /// </summary>
    /// <param name="mode">변경된 카메라 모드</param>
    public static void RaiseCameraModeChanged(CameraMode mode)
    {
        OnCameraModeChanged?.Invoke(mode);
    }

    /// <summary>
    /// 플레이어 회전 입력 이벤트를 발생시킵니다.
    /// </summary>
    /// <param name="rotationInput">회전 입력 값(X: 좌우, Y: 상하)</param>
    public static void RaisePlayerRotationInput(Vector2 rotationInput)
    {
        OnPlayerRotationInput?.Invoke(rotationInput);
    }
    #endregion
}