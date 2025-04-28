using UnityEditor;
using UnityEngine;

public static class EditorTools
{
    [MenuItem("Tools/Reload Domain")]
    public static void ReloadDomain()
    {
        EditorUtility.RequestScriptReload();
    }
}