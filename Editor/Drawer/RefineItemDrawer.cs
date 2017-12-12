using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomPropertyDrawer(typeof(RefineItem))]
public class RefineItemDrawer : PropertyDrawer
{
    SerializedProperty typeProp;
    SerializedProperty argumentsProp;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        typeProp = property.FindPropertyRelative("type");
        argumentsProp = property.FindPropertyRelative("arguments");
        if (!property.isExpanded)
        {
            return EditorGUIUtility.singleLineHeight;
        }
        return (argumentsProp.arraySize + 1) * EditorGUIUtility.singleLineHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        if (GUI.Button(rect, typeProp.stringValue, EditorStyles.toolbarButton))
        {
            property.isExpanded = !property.isExpanded;
        }
        if (property.isExpanded)
        {
            EditorGUI.BeginDisabledGroup(true);
            for (int i = 0; i < argumentsProp.arraySize; i++)
            {
                rect.y += EditorGUIUtility.singleLineHeight;
                var prop = argumentsProp.GetArrayElementAtIndex(i);
                var pname = prop.FindPropertyRelative("name");
                EditorGUI.PropertyField(rect,pname);
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
