using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System;

[System.Serializable]
public class RefineItem
{
    public string name;
    public string assemble;
    public string baseType;
    public string type;
    public List<Argument> arguments;
    public string metaFilePath;

    public RefineItem(MonoBehaviour behaiver)
    {
        this.name = MonoScript.FromMonoBehaviour(behaiver).name;
        this.type = behaiver.GetType().ToString();
        this.assemble = behaiver.GetType().Assembly.ToString();
        baseType = behaiver.GetType().BaseType.ToString();
        this.metaFilePath = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(behaiver)) + ".meta";
        arguments = new List<Argument>();
        RefineUtility.AnalysisArguments(behaiver.GetType(), arguments);
    }

    public RefineItem(Type type)
    {
        this.name = type.Name;
        this.type = type.ToString();
        this.assemble = type.Assembly.ToString();
        this.baseType = type.BaseType.ToString();
        arguments = new List<Argument>();
        RefineUtility.AnalysisArguments(type, arguments);
    }


}

