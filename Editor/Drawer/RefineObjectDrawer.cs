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
    private void OnEnable()
    {
        scriptProp = serializedObject.FindProperty("m_Script");
        exportPathProp = serializedObject.FindProperty("exportPath");
        refineListProp = serializedObject.FindProperty("refineList");
        refineObj = target as RefineObj;
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(scriptProp);
        DrawHeadButtons();
        DrawScripts();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScripts()
    {
        for (int i = 0; i < refineListProp.arraySize; i++)
        {
            var prop = refineListProp.GetArrayElementAtIndex(i);
            EditorGUILayout.PropertyField(prop);
        }
    }

    private void DrawHeadButtons()
    {
        var btnStyles = EditorStyles.toolbarButton;
        using (var hor = new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("读取脚本", btnStyles))
            {
                LoadPrefabScripts();
            }
            if (GUILayout.Button("批量导出", btnStyles))
            {
                ExportWorpScripts();
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
    /// 将预制体身上的脚本读取到列表中
    /// </summary>
    private void LoadPrefabScripts()
    {
        if (Selection.activeTransform)
        {
            var behaivers = new List<MonoBehaviour>();
            RefineUtility.LoadScriptsFromPrefab(Selection.activeTransform, behaivers);
            foreach (var item in behaivers)
            {
                var old = refineObj.refineList.Find(x => x.type == item.GetType().ToString());
                if (old != null){
                    refineObj.refineList.Remove(old);
                }
                var refineItem = new RefineItem(item);
                refineObj.refineList.Add(refineItem);
                LoopInsertItem(refineItem, refineObj.refineList);

                var currentType = item.GetType();
                while (currentType.BaseType != typeof(MonoBehaviour)){
                    old = refineObj.refineList.Find(x => x.type == currentType.BaseType.ToString());
                    if (old != null){
                        refineObj.refineList.Remove(old);
                    }
                    refineItem = new RefineItem(currentType.BaseType);
                    refineObj.refineList.Add(refineItem);
                    currentType = currentType.BaseType;
                    LoopInsertItem(refineItem, refineObj.refineList);
                }

            }
            EditorUtility.SetDirty(refineObj);
        }
    }

    private void LoopInsertItem(RefineItem item,List<RefineItem> refineList)
    {
        foreach (var arg in item.arguments)
        {
            if (!string.IsNullOrEmpty(arg.subType))
            {
                var type = Assembly.Load(arg.assemble).GetType(arg.subType);
                if (type.IsGenericType)
                {
                    type = type.GetGenericArguments()[0];
                }

                Debug.Log("SubType:" + arg.subType);

                var old = refineObj.refineList.Find(x => x.type == type.ToString());
                if (old != null){
                    refineObj.refineList.Remove(old);
                }
                var refineItem = new RefineItem(type);
                refineObj.refineList.Add(refineItem);

                LoopInsertItem(refineItem, refineList);
            }
        }
    }
}
