using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System;
using System.Reflection;


[CustomEditor(typeof(RefineObj))]
public class RefineObjectDrawer : Editor
{
    SerializedProperty scriptProp;
    SerializedProperty refineListProp;
    SerializedProperty exportPathProp;
    RefineObj refineObj;
    SerializedProperty worpRefineListProp;
    string match;
    private void OnEnable()
    {
        scriptProp = serializedObject.FindProperty("m_Script");
        exportPathProp = serializedObject.FindProperty("exportPath");
        refineListProp = serializedObject.FindProperty("refineList");
        refineObj = target as RefineObj;
        var worpObj = ScriptableObject.CreateInstance<RefineObj>();
        worpRefineListProp = new SerializedObject(worpObj).FindProperty("refineList");
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(scriptProp);
        DrawHeadButtons();
        DrawMatchField();
        DrawScripts();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawMatchField()
    {
        EditorGUI.BeginChangeCheck();
        match = EditorGUILayout.TextField(match, EditorStyles.textField);
        if (EditorGUI.EndChangeCheck() && !string.IsNullOrEmpty(match))
        {
            worpRefineListProp.ClearArray();
            for (int i = 0; i < refineListProp.arraySize; i++)
            {
                var prop = refineListProp.GetArrayElementAtIndex(i);
                if (prop.FindPropertyRelative("type").stringValue.ToLower().Contains(match.ToLower()))
                {
                    worpRefineListProp.InsertArrayElementAtIndex(0);
                    CopyPropertyValue(worpRefineListProp.GetArrayElementAtIndex(0), prop);
                }
            }
        }
    }
    /// <summary>
    /// Copies value of <paramref name="sourceProperty"/> into <pararef name="destProperty"/>.
    /// </summary>
    /// <param name="destProperty">Destination property.</param>
    /// <param name="sourceProperty">Source property.</param>
    public static void CopyPropertyValue(SerializedProperty destProperty, SerializedProperty sourceProperty)
    {
        if (destProperty == null)
            throw new ArgumentNullException("destProperty");
        if (sourceProperty == null)
            throw new ArgumentNullException("sourceProperty");

        sourceProperty = sourceProperty.Copy();
        destProperty = destProperty.Copy();

        CopyPropertyValueSingular(destProperty, sourceProperty);

        if (sourceProperty.hasChildren)
        {
            int elementPropertyDepth = sourceProperty.depth;
            while (sourceProperty.Next(true) && destProperty.Next(true) && sourceProperty.depth > elementPropertyDepth)
                CopyPropertyValueSingular(destProperty, sourceProperty);
        }
    }
    private static void CopyPropertyValueSingular(SerializedProperty destProperty, SerializedProperty sourceProperty)
    {
        switch (destProperty.propertyType)
        {
            case SerializedPropertyType.Integer:
                destProperty.intValue = sourceProperty.intValue;
                break;
            case SerializedPropertyType.Boolean:
                destProperty.boolValue = sourceProperty.boolValue;
                break;
            case SerializedPropertyType.Float:
                destProperty.floatValue = sourceProperty.floatValue;
                break;
            case SerializedPropertyType.String:
                destProperty.stringValue = sourceProperty.stringValue;
                break;
            case SerializedPropertyType.Color:
                destProperty.colorValue = sourceProperty.colorValue;
                break;
            case SerializedPropertyType.ObjectReference:
                destProperty.objectReferenceValue = sourceProperty.objectReferenceValue;
                break;
            case SerializedPropertyType.LayerMask:
                destProperty.intValue = sourceProperty.intValue;
                break;
            case SerializedPropertyType.Enum:
                destProperty.enumValueIndex = sourceProperty.enumValueIndex;
                break;
            case SerializedPropertyType.Vector2:
                destProperty.vector2Value = sourceProperty.vector2Value;
                break;
            case SerializedPropertyType.Vector3:
                destProperty.vector3Value = sourceProperty.vector3Value;
                break;
            case SerializedPropertyType.Vector4:
                destProperty.vector4Value = sourceProperty.vector4Value;
                break;
            case SerializedPropertyType.Rect:
                destProperty.rectValue = sourceProperty.rectValue;
                break;
            case SerializedPropertyType.ArraySize:
                destProperty.intValue = sourceProperty.intValue;
                break;
            case SerializedPropertyType.Character:
                destProperty.intValue = sourceProperty.intValue;
                break;
            case SerializedPropertyType.AnimationCurve:
                destProperty.animationCurveValue = sourceProperty.animationCurveValue;
                break;
            case SerializedPropertyType.Bounds:
                destProperty.boundsValue = sourceProperty.boundsValue;
                break;
            case SerializedPropertyType.Gradient:
                //!TODO: Amend when Unity add a public API for setting the gradient.
                break;
        }
    }
    private void DrawScripts()
    {
        if (string.IsNullOrEmpty(match))
        {
            for (int i = 0; i < refineListProp.arraySize; i++)
            {
                var prop = refineListProp.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(prop);
            }
        }
        else
        {
            for (int i = 0; i < worpRefineListProp.arraySize; i++)
            {
                var prop = worpRefineListProp.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(prop);
            }
        }
    }

    private void DrawHeadButtons()
    {
        var btnStyles = EditorStyles.toolbarButton;
        using (var hor = new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("读取脚本", btnStyles))
            {
                TryLoadFromScriptObject();
                TryLoadFromTransformScripts();
                TryLoadFromFolderScripts();
                TryLoadSingleScript();
            }
            if (GUILayout.Button("批量导出", btnStyles))
            {
                ExportWorpScripts();
            }
            if (GUILayout.Button("清空", btnStyles))
            {
                refineListProp.ClearArray();
            }
        }
    }

    /// <summary>
    /// 生成worp脚本
    /// </summary>
    private void ExportWorpScripts()
    {
        var oldPath = Application.streamingAssetsPath;
        if (!string.IsNullOrEmpty(exportPathProp.stringValue))
        {
            oldPath = exportPathProp.stringValue;
        }
        var folder = EditorUtility.SaveFolderPanel("选择导出路径", oldPath, "");
        if (!string.IsNullOrEmpty(folder))
        {
            exportPathProp.stringValue = folder;
            RefineUtility.ExportScripts(folder, refineObj.refineList);
        }
    }

    /// <summary>
    /// 将scriptObject用到的脚本读取到列表中
    /// </summary>
    private void TryLoadFromScriptObject()
    {
        if (Selection.activeObject && Selection.activeObject is ScriptableObject)
        {
            var mono = MonoScript.FromScriptableObject(Selection.activeObject as ScriptableObject);
            OnLoadMonoScript(mono);
            EditorUtility.SetDirty(refineObj);
        }
    }
    /// <summary>
    /// 将预制体身上的脚本读取到列表中
    /// </summary>
    private void TryLoadFromTransformScripts()
    {
        if (Selection.activeTransform)
        {
            var behaivers = new List<MonoScript>();
            RefineUtility.LoadScriptsFromPrefab(Selection.activeTransform, behaivers);
            OnLoadMonoScript(behaivers.ToArray());
            EditorUtility.SetDirty(refineObj);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="monos"></param>
    private void OnLoadMonoScript(params MonoScript[] monos)
    {
        foreach (var item in monos)
        {
            if (item == null || item.GetClass() == null) continue;

            var old = refineObj.refineList.Find(x => x.type == item.GetClass().ToString());
            if (old != null)
            {
                refineObj.refineList.Remove(old);
            }

            var refineItem = new RefineItem(item);
            refineObj.refineList.Add(refineItem);
            LoopInsertItem(refineItem, refineObj.refineList);
        }
    }
    /// <summary>
    /// 从文件夹读取
    /// </summary>
    private void TryLoadFromFolderScripts()
    {
        if (Selection.activeObject && ProjectWindowUtil.IsFolder(Selection.activeObject.GetInstanceID()))
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            var behaivers = new List<MonoScript>();
            RefineUtility.LoadScriptsFromFolder(path, behaivers);
            OnLoadMonoScript(behaivers.ToArray());
            EditorUtility.SetDirty(refineObj);
        }
    }


    /// <summary>
    /// 直接读取一个脚本
    /// </summary>
    private void TryLoadSingleScript()
    {
        if (Selection.activeObject && Selection.activeObject is MonoScript)
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            var mono = RefineUtility.LoadScriptDriect(path);
            if (mono != null)
            {
                OnLoadMonoScript(mono);
            }
            EditorUtility.SetDirty(refineObj);
        }
    }

    private void LoopInsertItem(RefineItem item, List<RefineItem> refineList)
    {
        //遍历参数
        foreach (var arg in item.arguments)
        {
            if (!string.IsNullOrEmpty(arg.subType))
            {
                var type = Assembly.Load(arg.subAssemble).GetType(arg.subType);
                if (type.IsGenericType)
                {
                    type = type.GetGenericArguments()[0];
                }

                //Debug.Log("SubType:" + arg.subType);

                var old = refineObj.refineList.Find(x => x.type == type.ToString());
                if (old != null)
                {
                    refineObj.refineList.Remove(old);
                }
                var refineItem = new RefineItem(type);
                refineObj.refineList.Add(refineItem);

                LoopInsertItem(refineItem, refineList);
            }
        }

        var currentType = Assembly.Load(item.assemble).GetType(item.type);
        //遍历类父级
        while (currentType.BaseType != null &&
            currentType.BaseType != typeof(MonoBehaviour) &&
            currentType.BaseType != typeof(ScriptableObject) &&
            currentType.BaseType != typeof(object) &&
            currentType.BaseType != typeof(Enum))
        {
            var old = refineObj.refineList.Find(x => x.type == currentType.BaseType.ToString());
            if (old != null)
            {
                refineObj.refineList.Remove(old);
            }
            var refineItem = new RefineItem(currentType.BaseType);
            refineObj.refineList.Add(refineItem);
            currentType = currentType.BaseType;
            LoopInsertItem(refineItem, refineObj.refineList);
        }
    }
}
