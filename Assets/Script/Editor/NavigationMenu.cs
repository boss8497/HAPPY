using System;
using Expression;
using Script.GameInfo.Table;
using UnityEditor;
using UnityEngine;

public static class NavigationMenu
{
    [MenuItem("Tools/데이터 리로드")]
    public static void ReLoadDatabase() {
        GameInfoManager.Instance.Release();
        GameInfoManager.Instance.Load();
    }
    
}
