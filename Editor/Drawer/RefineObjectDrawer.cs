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
        EditorGUILayout.ObjectField(refineObj, typeof(RefineObj), false);
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
                    RefineUtility.CopyPropertyValue(worpRefineListProp.GetArrayElementAtIndex(0), prop);
                }
            }
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
                TryLoadFromSelection();
                refineObj.refineList.Sort();
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

    private void TryLoadFromSelection()
    {
        if (Selection.objects.Length > 0)
        {
            var list = new List<MonoScript>();
            foreach (var item in Selection.objects)
            {
                if (item is ScriptableObject)
                {
                    TryLoadFromScriptObject(item as ScriptableObject, list);
                }
                else if (item is MonoScript)
                {
                    TryLoadSingleScript(item as MonoScript, list);
                }
                else if (item is GameObject)
                {
                    TryLoadScriptsFromPrefab(item as GameObject, list);
                }
                else if (ProjectWindowUtil.IsFolder(item.GetInstanceID()))
                {
                    var path = AssetDatabase.GetAssetPath(Selection.activeObject);
                    TryLoadFromFolder(path, list);
                }
            }
            OnLoadMonoScript(list.ToArray());
            EditorUtility.SetDirty(refineObj);
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
    private void TryLoadFromScriptObject(ScriptableObject scriptObj, List<MonoScript> scriptList)
    {
        var mono = MonoScript.FromScriptableObject(scriptObj);
        if (RefineUtility.IsMonoBehaiverOrScriptObjectRuntime(mono))
        {
            scriptList.Add(mono);
        }
    }


    /// <summary>
    /// 直接读取一个脚本
    /// </summary>
    private void TryLoadSingleScript(MonoScript script, List<MonoScript> scriptList)
    {
        if (RefineUtility.IsMonoBehaiverOrScriptObjectRuntime(script))
        {
            scriptList.Add(script);
        }
    }

    /// <summary>
    /// 从文件夹读取
    /// </summary>
    private void TryLoadFromFolder(string path, List<MonoScript> scriptList)
    {
        var files = System.IO.Directory.GetFiles(path, "*.*", System.IO.SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var dirPath = file.Replace("\\", "/").Replace(Application.dataPath, "Assets");
            if (file.EndsWith(".cs"))
            {
                var mono = AssetDatabase.LoadAssetAtPath<MonoScript>(dirPath);
                if (mono != null)
                {
                    TryLoadSingleScript(mono, scriptList);
                }
            }
            else if (file.EndsWith(".asset"))
            {
                var scriptObj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(dirPath);
                if (scriptObj != null)
                {
                    TryLoadFromScriptObject(scriptObj, scriptList);
                }
            }
            else if (file.EndsWith(".prefab"))
            {
                var gameObj = AssetDatabase.LoadAssetAtPath<GameObject>(dirPath);
                if (gameObj != null)
                {
                    TryLoadScriptsFromPrefab(gameObj, scriptList);
                }
            }

        }
    }


    /// <summary>
    /// 从预制体身上加载脚本
    /// </summary>
    /// <param name="trans"></param>
    /// <param name="types"></param>
    private void TryLoadScriptsFromPrefab(GameObject go, List<MonoScript> behaivers)
    {
        if (go == null) return;
        var trans = go.transform;
        var behaiver = trans.GetComponents<MonoBehaviour>();
        if (behaiver != null)
        {
            //var monos = new MonoScript[behaiver.Length];
            var monos = new List<MonoScript>();
            for (int i = 0; i < behaiver.Length; i++)
            {
                if (behaiver[i] == null)
                {
                    Debug.Log(trans.name + ":scriptMissing", go);
                }
                else
                {
                    var mono = MonoScript.FromMonoBehaviour(behaiver[i]);
                    if (RefineUtility.IsMonoBehaiverOrScriptObjectRuntime(mono))
                    {
                        monos.Add(mono);
                    }
                }
            }
            behaivers.AddRange(monos);
        }
        if (trans.childCount == 0)
        {
            return;
        }
        else
        {
            for (int i = 0; i < trans.childCount; i++)
            {
                var childTrans = trans.GetChild(i);
                TryLoadScriptsFromPrefab(childTrans.gameObject, behaivers);
            }
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
            if (old == null)
            {
                var refineItem = new RefineItem(item);
                refineObj.refineList.Add(refineItem);
                LoopInsertItem(refineItem, refineObj.refineList);
            }
            else
            {
                old.Update(item);
            }
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

                if (type == null) continue;

                if (type.IsGenericType)
                {
                    type = type.GetGenericArguments()[0];
                }
                else if (type.IsArray)
                {
                    type = type.GetElementType();
                }

                if (RefineUtility.IsInternalScript(type)) continue;

                var old = refineObj.refineList.Find(x => x.type == type.ToString());
                if (old == null)
                {
                    var refineItem = new RefineItem(type);
                    refineObj.refineList.Add(refineItem);
                    LoopInsertItem(refineItem, refineList);
                }
                else
                {
                    old.Update(type);
                }
            }
        }

        var currentType = Assembly.Load(item.assemble).GetType(item.type);
        if(currentType == null)
        {
            currentType = Type.GetType(item.type);
        }
        if (currentType == null)
        {
            Debug.Log(item.type + ": load empty");
            return;
        }

        //遍历泛型类
        if (currentType.IsGenericType)
        {
            var gtypes = currentType.GetGenericArguments();
            foreach (var gtype in gtypes)
            {
                if (RefineUtility.IsInternalScript(gtype)) continue;

                var old = refineObj.refineList.Find(x => x.type == gtype.ToString());
                if (old == null)
                {
                    var refineItem = new RefineItem(gtype);
                    refineObj.refineList.Add(refineItem);
                    LoopInsertItem(refineItem, refineObj.refineList);
                }
                else
                {
                    old.Update(gtype);
                }
            }
        }

        //遍历类父级
        while (currentType != null && currentType.BaseType != null && !RefineUtility.IsInternalScript(currentType.BaseType))
        {
            currentType = currentType.BaseType;
            var old = refineObj.refineList.Find(x => x.type == currentType.ToString());
            if (old == null)
            {
                var refineItem = new RefineItem(currentType);
                refineObj.refineList.Add(refineItem);
                LoopInsertItem(refineItem, refineObj.refineList);
            }
            else
            {
                old.Update(currentType);
            }
        }
    }
}
