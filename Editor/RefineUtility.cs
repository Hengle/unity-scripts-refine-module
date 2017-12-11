using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.IO;
using System.Text;

public static class RefineUtility
{

    /// <summary>
    /// 从预制体身上加载脚本
    /// </summary>
    /// <param name="trans"></param>
    /// <param name="types"></param>
    public static void LoadScriptsFromPrefab(Transform trans, List<MonoBehaviour> behaivers)
    {
        var behaiver = trans.GetComponents<MonoBehaviour>();
        if (behaiver != null)
        {
            behaivers.AddRange(behaiver);
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
                LoadScriptsFromPrefab(childTrans, behaivers);
            }
        }

    }
    /// <summary>
    /// 生成新的脚本
    /// </summary>
    /// <param name="type"></param>
    /// <param name="arguments"></param>
    /// <returns></returns>
    internal static string GenerateNewScirpt(Type type, List<Argument> arguments)
    {
        //声明代码的部分
        CodeCompileUnit compunit = new CodeCompileUnit();
        CodeNamespace sample = new CodeNamespace(type.Namespace);
        compunit.Namespaces.Add(sample);

        //引用命名空间
        sample.Imports.Add(new CodeNamespaceImport("System"));
        sample.Imports.Add(new CodeNamespaceImport("UnityEngine"));

        if (type.IsClass)
        {
            sample.Types.Add(GenerateClass(type, arguments));//把这个类添加到命名空间 ,待会儿才会编译这个类
        }
        else if (type.IsEnum)
        {
            sample.Types.Add(GenerateEnum(type, arguments));
        }

        CSharpCodeProvider cprovider = new CSharpCodeProvider();
        StringBuilder fileContent = new StringBuilder();
        using (StringWriter sw = new StringWriter(fileContent))
        {
            cprovider.GenerateCodeFromCompileUnit(compunit, sw, new CodeGeneratorOptions());//想把生成的代码保存为cs文件
        }

        return fileContent.ToString();
    }

    private static CodeTypeDeclaration GenerateClass(Type type, List<Argument> arguments)
    {
        //在命名空间下添加一个类
        CodeTypeDeclaration wrapProxyClass = new CodeTypeDeclaration(type.Name);
        wrapProxyClass.BaseTypes.Add(new CodeTypeReference(type.BaseType));// 如果需要的话 在这里声明继承关系 (基类 , 接口)
        //wrapProxyClass.CustomAttributes.Add(new CodeAttributeDeclaration("Serializable"));//添加一个Attribute到class上

        foreach (var item in arguments)
        {
            System.CodeDom.CodeMemberField field = new CodeMemberField();
            field.Type = new CodeTypeReference(item.type);
            field.Name = item.name;
            field.Attributes = MemberAttributes.Public;
            wrapProxyClass.Members.Add(field);
        }
        return wrapProxyClass;
    }

    private static CodeTypeDeclaration GenerateEnum(Type type, List<Argument> arguments)
    {
        //在命名空间下添加一个类
        CodeTypeDeclaration warpEnum = new CodeTypeDeclaration(type.Name);
        //warpEnum.BaseTypes.Add(new CodeTypeReference(type.BaseType));// 如果需要的话 在这里声明继承关系 (基类 , 接口)
        //wrapProxyClass.CustomAttributes.Add(new CodeAttributeDeclaration("Serializable"));//添加一个Attribute到class上
        warpEnum.IsEnum = true;
        foreach (var item in arguments)
        {
            System.CodeDom.CodeMemberField field = new CodeMemberField();
            field.Type = new CodeTypeReference(item.type);
            field.Name = item.name;
            //field.Attributes = MemberAttributes.Public;
            warpEnum.Members.Add(field);
        }
        return warpEnum;
    }

    /// <summary>
    /// 将脚本上的变量记录
    /// </summary>
    /// <param name="behaiver"></param>
    /// <param name="arguments"></param>
    public static void AnalysisArguments(Type type, List<Argument> arguments)
    {
        if (type.IsClass)
        {
            AnalysisClassArguments(type, arguments);
        }
        else if (type.IsEnum)
        {
            AnalysisEnumArguments(type, arguments);
        }
    }

    private static void AnalysisClassArguments(Type type, List<Argument> arguments)
    {
        FieldInfo[] publicFields = type.GetFields(BindingFlags.GetField | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);
        foreach (var item in publicFields)
        {
            var variable = CreateArgument(item);
            arguments.Add(variable);
        }
        FieldInfo[] privateFields = type.GetFields(BindingFlags.GetField | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic);
        foreach (var item in privateFields)
        {
            var attrs = item.GetCustomAttributes(false);
            if (attrs != null && attrs.Length > 0 && Array.Find(attrs, x => x is SerializeField) != null)
            {
                var variable = CreateArgument(item);
                arguments.Add(variable);
            }
        }
    }

    private static void AnalysisEnumArguments(Type type, List<Argument> arguments)
    {
        FieldInfo[] fieldInfo = type.GetFields();
        foreach (var item in fieldInfo)
        {
            if (item.Name != "value__")
            {
                var old = arguments.Find(x => x.name == item.Name);
                var arg = new Argument();
                arg.name = item.Name;
                arguments.Add(arg);
            }
        }
    }

    internal static void ExportScripts(string path, List<RefineItem> refineList)
    {
        for (int i = 0; i < refineList.Count; i++)
        {
            var item = refineList[i];
            var scriptPath = path + "\\" + item.name + ".cs";
            var metaPath = scriptPath + ".meta";
            var newScript = RefineUtility.GenerateNewScirpt(Assembly.Load(item.assemble).GetType(item.type), item.arguments);
            System.IO.File.WriteAllText(scriptPath, newScript);
            if (!string.IsNullOrEmpty(item.metaFilePath))
            {
                var metaFile = System.IO.File.ReadAllText(item.metaFilePath);
                System.IO.File.WriteAllText(metaPath, metaFile);
            }

        }
    }

    /// <summary>
    /// 反射生成Argument
    /// </summary>
    /// <param name="item"></param>
    /// <param name="behaiver"></param>
    /// <returns></returns>
    private static Argument CreateArgument(FieldInfo item)
    {
        Argument arg = new Argument();
        arg.name = item.Name;
        var type = item.FieldType;
        arg.type = type.ToString();
        var supportTypes = new List<Type> {
            typeof(int),
            typeof(short),
            typeof(long),
            typeof(double),
            typeof(float),
            typeof(decimal),
            typeof(string),
            typeof(bool)
        };
        arg.subType = "";

        if (type.IsArray || (!supportTypes.Contains(type) && !arg.type.StartsWith("UnityEngine")))
        {
            if (type.IsGenericType)
            {
                var arrayType = type.GetGenericArguments()[0];
                if (!supportTypes.Contains(arrayType) && !arrayType.ToString().StartsWith("UnityEngine"))
                {
                    arg.subType = arrayType.ToString();
                    arg.assemble = arrayType.Assembly.ToString();
                }
            }
            else
            {
                arg.subType = type.ToString();
                arg.assemble = type.Assembly.ToString();
            }
        }
        return arg;
    }


}
