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
    public List<AttributeInfo> attributes;
    public List<Argument> arguments;
    public string metaFilePath;

    public RefineItem(MonoScript mono):this(mono.GetClass())
    {
        this.metaFilePath = AssetDatabase.GetAssetPath(mono) + ".meta";
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
        attributes = new List<AttributeInfo>();
        RefineUtility.AnalysisAttributes(type, attributes);
        
    }


}

