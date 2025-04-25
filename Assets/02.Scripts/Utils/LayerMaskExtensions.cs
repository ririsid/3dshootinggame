using System;
using System.Collections.Generic;
using UnityEngine;

public static class LayerMaskExtensions
{
    /// <summary>
    /// LayerMask에 포함된 모든 레이어의 인덱스를 배열로 반환합니다.
    /// </summary>
    /// <param name="layerMask">레이어 마스크</param>
    /// <returns>포함된 레이어 인덱스 배열</returns>
    public static int[] GetIncludedLayerIndices(this LayerMask layerMask)
    {
        List<int> layers = new List<int>(8); // 초기 용량 지정으로 메모리 재할당 최소화
        int mask = layerMask.value; // LayerMask의 정수 값 가져오기

        // 마스크가 0이면 빈 배열 즉시 반환
        if (mask == 0)
        {
            return Array.Empty<int>();
        }

        // 0부터 31까지 모든 레이어 인덱스를 확인
        for (int i = 0; i < 32; i++)
        {
            // i번째 비트가 1인지 확인 (해당 레이어가 마스크에 포함되었는지)
            if ((mask & (1 << i)) != 0)
            {
                layers.Add(i); // 포함되었다면 리스트에 추가
            }
        }
        return layers.ToArray(); // 리스트를 배열로 변환하여 반환
    }
}