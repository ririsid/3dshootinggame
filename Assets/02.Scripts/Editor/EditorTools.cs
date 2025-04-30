using UnityEditor;
using UnityEngine;

/// <summary>
/// 유니티 에디터에서 사용할 수 있는 도구 모음 클래스입니다.
/// </summary>
public static class EditorTools
{
    #region 에디터 도구
    /// <summary>
    /// 스크립트 도메인을 다시 로드합니다.
    /// 스크립트 변경 후 수동으로 다시 로드해야 할 때 사용합니다.
    /// </summary>
    [MenuItem("Tools/Reload Domain")]
    public static void ReloadDomain()
    {
        EditorUtility.RequestScriptReload();
        Debug.Log("스크립트 도메인을 다시 로드했습니다.");
    }
    #endregion

    #region 프리팹 관리
    /// <summary>
    /// 선택한 프리팹의 모든 참조를 찾아 로그로 출력합니다.
    /// </summary>
    [MenuItem("Tools/Find Prefab References")]
    public static void FindPrefabReferences()
    {
        GameObject selectedObject = Selection.activeGameObject;
        if (selectedObject == null)
        {
            Debug.LogWarning("선택된 게임 오브젝트가 없습니다.");
            return;
        }

        Debug.Log($"'{selectedObject.name}' 프리팹의 참조를 검색 중...");
        // 추후 구현할 참조 검색 로직
    }
    #endregion
}