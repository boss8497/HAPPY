#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using CharacterInfo = Script.GameInfo.Character.CharacterInfo;

namespace Script.GameInfo.Table.Editor {
    public sealed class CharacterTableEditorWindow : TableEditorWindow<CharacterTable, CharacterInfo> {
        protected override string WindowTitle         => "Character Table Editor";
        protected override string DefaultSearchFilter => "t:CharacterTable";

        [MenuItem("Tools/GameInfo/캐릭터 정보")]
        private static void OpenWindow() {
            var window = GetWindow<CharacterTableEditorWindow>();
            window.titleContent = new GUIContent("Character Table");
            window.Show();
        }

        protected override string GetInfosPropertyName() {
            return nameof(CharacterTable.CharacterInfos);
        }

        protected override string GetDisplayName(CharacterInfo info) {
            if (info == null)
                return "<null>";

            // 네 CharacterInfo 구조에 맞게 수정
            // 예시:
            return string.IsNullOrEmpty(info.ID) ? "CharacterInfo" : info.ID;
        }

        protected override void DrawCreateDraftGUI() {
            var draft = CreateDraft;
            if (draft == null)
                return;

            SirenixEditorGUI.Title("Draft", "새 데이터를 입력한 뒤 Create / Upsert를 누르세요.", TextAlignment.Left, true);
            GUILayout.Space(4);

            // 아래는 예시야.
            // 네 CharacterInfo 필드에 맞게 바꿔줘.

            draft.UID = EditorGUILayout.IntField("UID", draft.UID);
            draft.ID  = EditorGUILayout.TextField("Name", draft.ID);

            // 예:
            // draft.level = EditorGUILayout.IntField("Level", draft.level);
            // draft.hp    = EditorGUILayout.FloatField("HP", draft.hp);
            // draft.atk   = EditorGUILayout.FloatField("Attack", draft.atk);
        }

        protected override bool BeforeUpsert(CharacterInfo draft) {
            if (draft == null)
                return false;

            if (string.IsNullOrWhiteSpace(draft.ID)) {
                EditorUtility.DisplayDialog("Invalid Data", "Name을 입력해 주세요.", "OK");
                return false;
            }

            return true;
        }
    }
}
#endif