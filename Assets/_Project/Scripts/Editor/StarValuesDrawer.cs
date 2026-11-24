using UnityEditor;
using UnityEngine;

namespace HordeLab.EditorTools
{
    /// <summary>Vẽ StarValues trên 1 dòng: nhãn + 4 ô (1★ 2★ 3★ 4★).</summary>
    [CustomPropertyDrawer(typeof(StarValues))]
    public class StarValuesDrawer : PropertyDrawer
    {
        static readonly string[] Names = { "star1", "star2", "star3", "star4" };
        static readonly GUIContent[] Labels = { new GUIContent("1★"), new GUIContent("2★"), new GUIContent("3★"), new GUIContent("4★") };

        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginProperty(pos, label, prop);
            pos = EditorGUI.PrefixLabel(pos, GUIUtility.GetControlID(FocusType.Passive), label);
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            float oldLabel = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 22f;
            float w = pos.width / 4f;
            for (int i = 0; i < 4; i++)
            {
                var r = new Rect(pos.x + i * w, pos.y, w - 4f, pos.height);
                EditorGUI.PropertyField(r, prop.FindPropertyRelative(Names[i]), Labels[i]);
            }
            EditorGUIUtility.labelWidth = oldLabel;
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}
