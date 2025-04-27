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
        int mask = layerMask.value; // LayerMask의 정수 값 가져오기

        // 마스크가 0이면 빈 배열 즉시 반환
        if (mask == 0)
        {
            return Array.Empty<int>();
        }

        // 포함된 비트 개수 미리 계산하여 리스트 초기 용량 지정
        int bitCount = CountBits(mask);
        var layers = new List<int>(bitCount);

        // 비트 연산으로 성능 최적화: 마스크가 0이 될 때까지 최하위 비트 추출
        while (mask != 0)
        {
            // 최하위 1 비트의 위치 찾기
            int lowestBitIndex = GetLowestSetBitIndex(mask);

            // 찾은 비트 인덱스 추가
            layers.Add(lowestBitIndex);

            // 해당 비트 마스크에서 제거
            mask &= ~(1 << lowestBitIndex);
        }

        return layers.ToArray();
    }

    /// <summary>
    /// 정수의 비트 중 1인 비트의 개수를 계산합니다.
    /// </summary>
    /// <param name="n">계산할 정수</param>
    /// <returns>1인 비트의 개수</returns>
    private static int CountBits(int n)
    {
        // Brian Kernighan의 알고리즘: 1인 비트의 개수를 효율적으로 계산
        int count = 0;
        while (n != 0)
        {
            n &= (n - 1); // 최하위 비트를 제거
            count++;
        }
        return count;
    }

    /// <summary>
    /// 정수의 가장 낮은 위치의 1 비트 인덱스를 찾습니다.
    /// </summary>
    /// <param name="n">확인할 정수</param>
    /// <returns>가장 낮은 위치의 1 비트 인덱스</returns>
    private static int GetLowestSetBitIndex(int n)
    {
        // 이 연산은 n과 (n의 2의 보수)의 AND 연산으로 가장 낮은 비트만 남김
        int isolatedLowestBit = n & -n;

        // 비트 위치 계산 (log2 구현)
        return (int)Math.Log(isolatedLowestBit, 2);
    }
}