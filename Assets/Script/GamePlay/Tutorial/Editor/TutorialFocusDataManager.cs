using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Script.GameInfo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Script.Tutorial.Editor {
    public class TutorialFocusDataManager {
        // 캐시(원하면 제거)
        private static List<TutorialFocusData> _prefabFocusCache;

        public static string sceneFloderPath = "Assets/Scenes";
        public static string extension       = ".asset";


        public static Color BackGroundBlue = new Color(0.2f, 0.4f, 0.8f, 1f);
        public static Color BackGroundRed  = new Color(0.8f, 0.2f, 0.3f, 1f);

        public static float ElementMaginTop   = 8f;
        public static float TitleLabelPersent = 15;

        public static List<FocusComponent> _focusObject;


        public static List<string> ignorePath = new() {
            "Assets/GAME_ASSETS/Unit",
        };

        [SettingsProvider]
        public static SettingsProvider CreateCustomSettingsProvider() {
            // SettingsProvider 생성 및 설정
            var provider = new SettingsProvider("Project/FocusSettings", SettingsScope.Project) {
                label = "Focus Settings",
                activateHandler = (searchContext, rootElement) => {
                    // VisualElement로 UI 구성하기
                    var container = new VisualElement();

                    // 경로 입력용 TextField 추가
                    var pathField = new TextField("Scene Folder Path") { value = EditorPrefs.GetString(sceneFloderPath, "Assets/Scenes") };
                    pathField.RegisterValueChangedCallback(evt => {
                        // EditorPrefs에 경로 저장
                        EditorPrefs.SetString(sceneFloderPath, evt.newValue);
                    });

                    // 파일 경로 선택 버튼 추가
                    var selectPathButton = new Button(() => {
                        var selectedPath = EditorUtility.OpenFolderPanel("Select Folder", pathField.value, "");
                        selectedPath = Path.GetRelativePath(Environment.CurrentDirectory, selectedPath);
                        if (!string.IsNullOrEmpty(selectedPath)) {
                            pathField.value = selectedPath;
                            EditorPrefs.SetString(sceneFloderPath, selectedPath);
                        }
                    }) { text = "Select Folder" };

                    container.Add(pathField);
                    container.Add(selectPathButton);

                    // Project Settings의 rootElement에 추가
                    rootElement.Add(container);
                },
                keywords = new[] { "Focus", "Path", "Settings" }
            };

            return provider;
        }


        public static List<TutorialFocusData> GetFocusDataFromAllPrefabs(bool forceRefresh = false) {
            if (!forceRefresh && _prefabFocusCache is { Count: > 0 })
                return _prefabFocusCache;

            var result = new Dictionary<string, TutorialFocusData>(); // key = focusData.guid

            // 모든 prefab GUID 수집
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

            try {
                for (int i = 0; i < prefabGuids.Length; i++) {
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "Scanning Prefabs",
                            $"Reading prefabs... ({i + 1}/{prefabGuids.Length})",
                            (float)(i + 1) / prefabGuids.Length)) {
                        break; // 사용자가 취소
                    }

                    var path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                    if (string.IsNullOrEmpty(path))
                        continue;

                    if (ignorePath.Any(a => path.Contains(a))) continue;

                    // 프리팹 열기(임시 씬)
                    GameObject root = null;
                    try {
                        root = PrefabUtility.LoadPrefabContents(path);
                        if (root == null) continue;

                        // 프리팹 내부 전체에서 FocusObject 검색(비활성 포함)
                        var focuses = root.GetComponentsInChildren<FocusComponent>(true);
                        foreach (var f in focuses) {
                            var data = f?.FocusData;
                            if (data == null) continue;
                            var guid = data.guid.ToString();
                            if (string.IsNullOrEmpty(guid)) continue;

                            // guid 유니크 보장
                            if (!result.ContainsKey(guid))
                                result.Add(guid, data);
                        }
                    }
                    catch (Exception ex) {
                        Debug.LogWarning($"[Focus Scan] Failed to read prefab: {path}\n{ex}");
                    }
                    finally {
                        if (root != null)
                            PrefabUtility.UnloadPrefabContents(root);
                    }
                }
            }
            finally {
                EditorUtility.ClearProgressBar();
            }

            _prefabFocusCache = result.Values.ToList();
            return _prefabFocusCache;
        }

        public static List<TutorialFocusData> GetFocusData(bool forceRefresh = false) {
            return GetFocusDataFromAllPrefabs(forceRefresh);
        }

        public static List<FocusComponent> GetFocusObjects(bool forceRefresh = false) {
            if (Application.isPlaying) return _focusObject;

            if ((_focusObject?.Count ?? 0) <= 0 || forceRefresh) {
                if (_focusObject != null) _focusObject.Clear();

                _focusObject = FindScenesWithScript<FocusComponent>(sceneFloderPath)
                               .Where(r => r.FocusData != null)
                               .GroupBy(r => r.FocusData.guid)
                               .Select(s => s.FirstOrDefault())
                               .ToList();
            }

            return _focusObject;
        }


        public static List<T> FindScenesWithScript<T>(string sceneFloderPath) where T : MonoBehaviour {
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { sceneFloderPath });
            var scripts    = new List<T>();

            foreach (string sceneGuid in sceneGuids) {
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                var scene     = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);


                foreach (var rootObject in scene.GetRootGameObjects()) {
                    scripts.AddRange(FindScriptInGameObject<T>(rootObject));
                }

                //EditorSceneManager.CloseScene(scene, false);
                EditorSceneManager.UnloadSceneAsync(scene);
            }

            return scripts;
        }


        private static List<T> FindScriptInGameObject<T>(GameObject gameObject) where T : MonoBehaviour {
            var scripts    = new List<T>();
            var scriptType = typeof(T);
            var components = gameObject.GetComponentsInChildren<Component>(true);
            foreach (var component in components) {
                if (component != null && component.GetType() == scriptType) {
                    scripts.Add(component as T);
                }
            }

            return scripts;
        }
    }
}