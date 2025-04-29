using UnityEngine;
using DG.Tweening;

/// <summary>
/// DOTween에 대한 확장 메서드를 제공하는 클래스입니다.
/// </summary>
public static class DOTweenExtensions
{
    /// <summary>
    /// 트랜스폼의 localPosition을 흔들리게 합니다.
    /// </summary>
    /// <param name="target">대상 트랜스폼</param>
    /// <param name="duration">지속 시간</param>
    /// <param name="strength">흔들림 강도</param>
    /// <param name="vibrato">진동 횟수</param>
    /// <param name="randomness">무작위성 (0-90)</param>
    /// <param name="fadeOut">페이드 아웃 여부</param>
    /// <returns>트윈 객체</returns>
    public static Tween DOLocalShakePosition(this Transform target, float duration, float strength = 1f, int vibrato = 10, float randomness = 90f, bool fadeOut = true)
    {
        if (target == null) return null;
        return DOLocalShakePositionVector3(target, duration, new Vector3(strength, strength, strength), vibrato, randomness, fadeOut);
    }

    /// <summary>
    /// 트랜스폼의 localPosition을 축별 강도로 흔들리게 합니다.
    /// </summary>
    /// <param name="target">대상 트랜스폼</param>
    /// <param name="duration">지속 시간</param>
    /// <param name="strength">축별 흔들림 강도</param>
    /// <param name="vibrato">진동 횟수</param>
    /// <param name="randomness">무작위성 (0-90)</param>
    /// <param name="fadeOut">페이드 아웃 여부</param>
    /// <returns>트윈 객체</returns>
    public static Tween DOLocalShakePositionVector3(this Transform target, float duration, Vector3 strength, int vibrato = 10, float randomness = 90f, bool fadeOut = true)
    {
        if (target == null) return null;

        // 원래 위치 저장
        Vector3 originalPosition = target.localPosition;

        // 진동 시퀀스 생성
        Sequence sequence = DOTween.Sequence();

        // 진동 시간 간격
        float shakeDeltaTime = duration / vibrato;

        // 무작위성 계수 (0-90 범위를 0-1로 정규화)
        float randAmountFactor = randomness / 90f;

        // 각 진동마다 처리
        for (int i = 0; i < vibrato; i++)
        {
            // 페이드아웃 적용
            float fadeOutFactor = fadeOut ? 1f - (i / (float)vibrato) : 1f;

            // 랜덤한 위치 계산
            float randomX = Random.Range(-strength.x, strength.x) * fadeOutFactor * randAmountFactor;
            float randomY = Random.Range(-strength.y, strength.y) * fadeOutFactor * randAmountFactor;
            float randomZ = Random.Range(-strength.z, strength.z) * fadeOutFactor * randAmountFactor;

            Vector3 randomPosition = originalPosition + new Vector3(randomX, randomY, randomZ);

            // 시퀀스에 추가
            sequence.Append(target.DOLocalMove(randomPosition, shakeDeltaTime).SetEase(Ease.OutQuad));
        }

        // 원래 위치로 복귀
        sequence.Append(target.DOLocalMove(originalPosition, shakeDeltaTime).SetEase(Ease.OutQuad));

        // 시퀀스 반환
        return sequence.SetTarget(target).SetUpdate(true).SetId("DOLocalShakePosition").SetEase(Ease.Linear);
    }

    /// <summary>
    /// 트랜스폼의 localRotation을 흔들리게 합니다.
    /// </summary>
    /// <param name="target">대상 트랜스폼</param>
    /// <param name="duration">지속 시간</param>
    /// <param name="strength">흔들림 강도</param>
    /// <param name="vibrato">진동 횟수</param>
    /// <param name="randomness">무작위성 (0-90)</param>
    /// <param name="fadeOut">페이드 아웃 여부</param>
    /// <returns>트윈 객체</returns>
    public static Tween DOLocalShakeRotation(this Transform target, float duration, float strength = 90f, int vibrato = 10, float randomness = 90f, bool fadeOut = true)
    {
        if (target == null) return null;
        return DOLocalShakeRotationVector3(target, duration, new Vector3(strength, strength, strength), vibrato, randomness, fadeOut);
    }

    /// <summary>
    /// 트랜스폼의 localRotation을 축별 강도로 흔들리게 합니다.
    /// </summary>
    /// <param name="target">대상 트랜스폼</param>
    /// <param name="duration">지속 시간</param>
    /// <param name="strength">축별 흔들림 강도</param>
    /// <param name="vibrato">진동 횟수</param>
    /// <param name="randomness">무작위성 (0-90)</param>
    /// <param name="fadeOut">페이드 아웃 여부</param>
    /// <returns>트윈 객체</returns>
    public static Tween DOLocalShakeRotationVector3(this Transform target, float duration, Vector3 strength, int vibrato = 10, float randomness = 90f, bool fadeOut = true)
    {
        if (target == null) return null;

        // 원래 회전값 저장
        Vector3 originalEuler = target.localEulerAngles;

        // 진동 시퀀스 생성
        Sequence sequence = DOTween.Sequence();

        // 진동 시간 간격
        float shakeDeltaTime = duration / vibrato;

        // 무작위성 계수 (0-90 범위를 0-1로 정규화)
        float randAmountFactor = randomness / 90f;

        // 각 진동마다 처리
        for (int i = 0; i < vibrato; i++)
        {
            // 페이드아웃 적용
            float fadeOutFactor = fadeOut ? 1f - (i / (float)vibrato) : 1f;

            // 랜덤한 회전값 계산
            float randomX = Random.Range(-strength.x, strength.x) * fadeOutFactor * randAmountFactor;
            float randomY = Random.Range(-strength.y, strength.y) * fadeOutFactor * randAmountFactor;
            float randomZ = Random.Range(-strength.z, strength.z) * fadeOutFactor * randAmountFactor;

            Vector3 randomRotation = originalEuler + new Vector3(randomX, randomY, randomZ);

            // 시퀀스에 추가
            sequence.Append(target.DOLocalRotate(randomRotation, shakeDeltaTime).SetEase(Ease.OutQuad));
        }

        // 원래 회전값으로 복귀
        sequence.Append(target.DOLocalRotate(originalEuler, shakeDeltaTime).SetEase(Ease.OutQuad));

        // 시퀀스 반환
        return sequence.SetTarget(target).SetUpdate(true).SetId("DOLocalShakeRotation").SetEase(Ease.Linear);
    }
}