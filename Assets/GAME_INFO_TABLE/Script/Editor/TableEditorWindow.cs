#if UNITY_EDITOR
using System;
using System.Linq;
using Script.GameInfo.Base;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Script.GameInfo.Table.Editor {
    public abstract class TableEditorWindow<TTable, TInfo> : OdinEditorWindow
        where TTable : TableBase
        where TInfo : InfoBase, new() {

        private TTable  _table;
        private int     _selectedIndex = -1;
        private TInfo   _selectedInfo;
        private TInfo   _createDraft = new();

        private Vector2 _leftScroll;
        private Vector2 _rightScroll;

        protected TTable Table => _table;
        protected TInfo SelectedInfo => _selectedInfo;
        protected TInfo CreateDraft => _createDraft;

        protected int Count => _table?.Infos?.Length ?? 0;
        protected int SelectedUid => _selectedInfo?.UID ?? -1;

        protected abstract string WindowTitle { get; }
        protected virtual string DefaultSearchFilter => $"t:{typeof(TTable).Name}";
        protected virtual Vector2 DefaultWindowSize => new(1200, 700);
        protected virtual float LeftPanelWidth => 360f;

        protected override void OnEnable() {
            base.OnEnable();
            titleContent = new GUIContent(WindowTitle);
            minSize = DefaultWindowSize;
            TryLoadTable();
        }

        protected override void OnImGUI() {
            base.OnImGUI();
            DrawToolbar();
            //SirenixEditorGUI.HorizontalLineSeparator(Color.blanchedAlmond, 2);

            if (_table == null) {
                DrawMissingTableGUI();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar() {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(60))) {
                TryLoadTable(true);
            }

            GUI.enabled = _table != null;

            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60))) {
                SaveTable();
            }

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70))) {
                RefreshTable();
            }

            if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(60))) {
                EditorGUIUtility.PingObject(_table);
                Selection.activeObject = _table;
            }

            GUI.enabled = true;

            GUILayout.Space(12);
            GUILayout.Label($"Count: {Count}", EditorStyles.miniLabel, GUILayout.Width(90));
            GUILayout.Label($"Selected UID: {SelectedUid}", EditorStyles.miniLabel, GUILayout.Width(120));

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawMissingTableGUI() {
            GUILayout.Space(10);
            SirenixEditorGUI.MessageBox("테이블 에셋을 찾지 못했습니다.", MessageType.Warning);

            GUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Find Table Asset", GUILayout.Height(30))) {
                TryLoadTable(true);
            }

            if (GUILayout.Button("Create Table Asset", GUILayout.Height(30))) {
                CreateTableAsset();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLeftPanel() {
            EditorGUILayout.BeginVertical(GUILayout.Width(LeftPanelWidth));

            SirenixEditorGUI.BeginBox("Data List");
            DrawListToolbar();
            GUILayout.Space(4);

            var infos = _table.Infos ?? Array.Empty<InfoBase>();

            using (var scroll = new EditorGUILayout.ScrollViewScope(_leftScroll)) {
                _leftScroll = scroll.scrollPosition;

                for (var i = 0; i < infos.Length; i++) {
                    if (infos[i] is not TInfo info)
                        continue;

                    DrawListItem(i, info);
                    GUILayout.Space(2);
                }
            }

            SirenixEditorGUI.EndBox();
            EditorGUILayout.EndVertical();
        }

        private void DrawRightPanel() {
            EditorGUILayout.BeginVertical();

            using (var scroll = new EditorGUILayout.ScrollViewScope(_rightScroll)) {
                _rightScroll = scroll.scrollPosition;

                DrawDetailsSection();
                GUILayout.Space(8);
                DrawCreateSection();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDetailsSection() {
            SirenixEditorGUI.BeginBox("Details");

            if (_selectedInfo == null) {
                GUILayout.Space(20);
                GUILayout.Label("좌측에서 항목을 선택해 주세요.", SirenixGUIStyles.CenteredGreyMiniLabel);
                GUILayout.Space(20);
                SirenixEditorGUI.EndBox();
                return;
            }

            GUILayout.Space(4);

            var serializedObject = new SerializedObject(_table);
            serializedObject.Update();

            var infosProp = serializedObject.FindProperty(GetInfosPropertyName());
            if (infosProp != null && _selectedIndex >= 0 && _selectedIndex < infosProp.arraySize) {
                var elementProp = infosProp.GetArrayElementAtIndex(_selectedIndex);
                if (elementProp != null) {
                    DrawSelectedHeader(_selectedInfo);
                    GUILayout.Space(6);
                    EditorGUILayout.PropertyField(elementProp, true);
                }
                else {
                    SirenixEditorGUI.MessageBox("선택된 데이터를 찾을 수 없습니다.", MessageType.Warning);
                }
            }
            else {
                SirenixEditorGUI.MessageBox(
                    $"선택된 데이터를 그릴 수 없습니다.\nProperty Name: {GetInfosPropertyName()}",
                    MessageType.Warning);
            }

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Apply", GUILayout.Height(28))) {
                serializedObject.ApplyModifiedProperties();
                MarkDirtyAndSave(false);
                RefreshSelectedReference();
            }

            if (GUILayout.Button("Delete", GUILayout.Height(28))) {
                DeleteSelected();
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
            SirenixEditorGUI.EndBox();
        }

        private void DrawCreateSection() {
            SirenixEditorGUI.BeginBox("Create New");

            if (_createDraft == null)
                _createDraft = CreateNewDraft();

            DrawCreateDraftGUI();

            GUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Reset Draft", GUILayout.Height(28))) {
                _createDraft = CreateNewDraft();
                GUI.FocusControl(null);
            }

            if (GUILayout.Button("Create / Upsert", GUILayout.Height(28))) {
                CreateOrUpsertDraft();
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
            SirenixEditorGUI.EndBox();
        }

        private void DrawListToolbar() {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Add New", GUILayout.Height(26))) {
                _createDraft = CreateNewDraft();
                GUI.FocusControl(null);
            }

            GUI.enabled = _selectedInfo != null;
            if (GUILayout.Button("Duplicate Selected", GUILayout.Height(26))) {
                DuplicateSelected();
                GUI.FocusControl(null);
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawListItem(int index, TInfo info) {
            var isSelected = index == _selectedIndex;

            var oldColor = GUI.backgroundColor;
            if (isSelected)
                GUI.backgroundColor = new Color(0.35f, 0.65f, 1f, 0.35f);

            SirenixEditorGUI.BeginBox();

            var buttonStyle = new GUIStyle(SirenixGUIStyles.Button) {
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = 36
            };

            if (GUILayout.Button(GetListItemLabel(index, info), buttonStyle)) {
                _selectedIndex = index;
                RefreshSelectedReference();
                GUI.FocusControl(null);
            }

            SirenixEditorGUI.EndBox();

            GUI.backgroundColor = oldColor;

            var clickableRect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.ContextClick && clickableRect.Contains(Event.current.mousePosition)) {
                ShowContextMenu(index, info);
                Event.current.Use();
            }
        }

        private void CreateOrUpsertDraft() {
            if (_table == null) {
                Debug.LogError("Table is null.");
                return;
            }

            if (_createDraft == null) {
                Debug.LogError("Create draft is null.");
                return;
            }

            if (!BeforeUpsert(_createDraft))
                return;

            Undo.RecordObject(_table, $"Upsert {typeof(TInfo).Name}");

            var previousUid = _createDraft.UID;
            _table.Upsert(_createDraft);

            MarkDirtyAndSave();
            RefreshTable(false);

            var infos = _table.Infos ?? Array.Empty<InfoBase>();

            var selectedUid = _createDraft.UID != 0 ? _createDraft.UID : previousUid;
            _selectedIndex = Array.FindIndex(infos, x => x != null && x.UID == selectedUid);
            RefreshSelectedReference();

            AfterUpsert(_createDraft);

            _createDraft = CreateNewDraft();
            GUI.FocusControl(null);
        }

        private void DeleteSelected() {
            if (_table == null || _selectedInfo == null)
                return;

            if (!EditorUtility.DisplayDialog(
                    "Delete Info",
                    $"UID {_selectedInfo.UID} 데이터를 삭제할까요?",
                    "Delete",
                    "Cancel")) {
                return;
            }

            Undo.RecordObject(_table, $"Delete {typeof(TInfo).Name}");
            _table.Remove(_selectedInfo.UID);

            MarkDirtyAndSave();
            RefreshTable(false);

            _selectedIndex = -1;
            _selectedInfo = null;
            GUI.FocusControl(null);
        }

        private void DuplicateSelected() {
            if (_selectedInfo == null)
                return;

            _createDraft = CloneInfo(_selectedInfo);
            _createDraft.UID = 0;
        }

        private void ShowContextMenu(int index, TInfo info) {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Select"), false, () => {
                _selectedIndex = index;
                RefreshSelectedReference();
                Repaint();
            });

            menu.AddItem(new GUIContent("Duplicate"), false, () => {
                _selectedIndex = index;
                RefreshSelectedReference();
                DuplicateSelected();
                Repaint();
            });

            menu.AddItem(new GUIContent("Delete"), false, () => {
                _selectedIndex = index;
                RefreshSelectedReference();
                DeleteSelected();
                Repaint();
            });

            menu.ShowAsContext();
        }

        private void RefreshSelectedReference() {
            if (_table == null) {
                _selectedInfo = null;
                return;
            }

            var infos = _table.Infos;
            if (infos == null || _selectedIndex < 0 || _selectedIndex >= infos.Length) {
                _selectedInfo = null;
                return;
            }

            _selectedInfo = infos[_selectedIndex] as TInfo;
        }

        protected virtual void DrawSelectedHeader(TInfo info) {
            var title = $"UID : {info?.UID ?? -1}";
            var subtitle = GetDisplayName(info);

            SirenixEditorGUI.Title(title, subtitle, TextAlignment.Left, true);
        }

        protected virtual string GetListItemLabel(int index, TInfo info) {
            if (info == null)
                return $"[{index}] <null>";

            return $"[{index}] UID: {info.UID}  |  {GetDisplayName(info)}";
        }

        protected virtual string GetDisplayName(TInfo info) {
            return info == null ? "<null>" : info.GetType().Name;
        }

        protected virtual TInfo CreateNewDraft() {
            return new TInfo();
        }

        protected virtual TInfo CloneInfo(TInfo source) {
            if (source == null)
                return new TInfo();

            var json = JsonUtility.ToJson(source);
            return JsonUtility.FromJson<TInfo>(json);
        }

        protected virtual bool BeforeUpsert(TInfo draft) {
            return true;
        }

        protected virtual void AfterUpsert(TInfo draft) { }

        protected abstract void DrawCreateDraftGUI();

        protected virtual string GetInfosPropertyName() {
            return "Infos";
        }

        protected virtual void TryLoadTable(bool showDialogWhenMissing = false) {
            _table = FindTableAsset();

            if (_table == null && showDialogWhenMissing) {
                EditorUtility.DisplayDialog("Table Not Found", $"{typeof(TTable).Name} 에셋을 찾지 못했습니다.", "OK");
            }

            RefreshTable(false);
        }

        protected virtual TTable FindTableAsset() {
            var guids = AssetDatabase.FindAssets(DefaultSearchFilter);
            if (guids == null || guids.Length == 0)
                return null;

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<TTable>(path);
        }

        protected virtual void CreateTableAsset() {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Table Asset",
                typeof(TTable).Name,
                "asset",
                $"{typeof(TTable).Name} 에셋 저장 위치를 선택하세요.");

            if (string.IsNullOrEmpty(path))
                return;

            var asset = CreateInstance<TTable>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _table = asset;
            RefreshTable(false);
            EditorGUIUtility.PingObject(asset);
        }

        protected virtual void SaveTable() {
            if (_table == null)
                return;

            MarkDirtyAndSave();
        }

        protected virtual void RefreshTable(bool reloadAsset = true) {
            if (_table == null)
                return;

            if (reloadAsset) {
                var path = AssetDatabase.GetAssetPath(_table);
                if (!string.IsNullOrEmpty(path)) {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    _table = AssetDatabase.LoadAssetAtPath<TTable>(path);
                }
            }

            var infos = _table.Infos ?? Array.Empty<InfoBase>();

            if (infos.Length == 0) {
                _selectedIndex = -1;
                _selectedInfo = null;
            }
            else if (_selectedIndex < 0 || _selectedIndex >= infos.Length) {
                _selectedIndex = 0;
                RefreshSelectedReference();
            }
            else {
                RefreshSelectedReference();
            }

            Repaint();
        }

        protected virtual void MarkDirtyAndSave(bool refreshAsset = true) {
            if (_table == null)
                return;

            EditorUtility.SetDirty(_table);
            AssetDatabase.SaveAssets();

            if (refreshAsset)
                AssetDatabase.Refresh();
        }
    }
}
#endif