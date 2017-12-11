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

    public RefineItem(MonoScript mono)
    {
        this.name = mono.name;
        this.type = mono.GetClass().ToString();
        this.assemble = mono.GetClass().Assembly.ToString();
        if(mono.GetClass().BaseType != null) baseType = mono.GetClass().BaseType.ToString();
        this.metaFilePath = AssetDatabase.GetAssetPath(mono) + ".meta";
        arguments = new List<Argument>();
        RefineUtility.AnalysisArguments(mono.GetClass(), arguments);
    }

    public RefineItem(Type type)
    {
        this.name = type.Name;
        this.type = type.ToString();
        this.assemble = type.Assembly.ToString();
        if(type.BaseType != null){
            this.baseType = type.BaseType.ToString();
        }
        arguments = new List<Argument>();
        RefineUtility.AnalysisArguments(type, arguments);
    }


}

