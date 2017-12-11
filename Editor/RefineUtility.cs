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
    public static List<Type> basicTypes;
    static RefineUtility()
    {
        basicTypes = new List<Type> {
            typeof(int),
            typeof(short),
            typeof(long),
            typeof(double),
            typeof(float),
            typeof(decimal),
            typeof(string),
            typeof(bool)
        };
    }
    /// <summary>
    /// 从预制体身上加载脚本
    /// </summary>
    /// <param name="trans"></param>
    /// <param name="types"></param>
    public static void LoadScriptsFromPrefab(Transform trans, List<MonoScript> behaivers)
    {
        var behaiver = trans.GetComponents<MonoBehaviour>();
        if (behaiver != null)
        {
            var monos = new MonoScript[behaiver.Length];
            for (int i = 0; i < monos.Length; i++)
            {
                monos[i] = MonoScript.FromMonoBehaviour(behaiver[i]);
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
        //sample.Imports.Add(new CodeNamespaceImport("UnityEngine"));

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
        if (type.BaseType != null)
        {
            wrapProxyClass.BaseTypes.Add(new CodeTypeReference(type.BaseType));// 如果需要的话 在这里声明继承关系 (基类 , 接口)
        }

        if (!type.IsSubclassOf(typeof(UnityEngine.Object)))
        {
            wrapProxyClass.CustomAttributes.Add(new CodeAttributeDeclaration("Serializable"));//添加一个Attribute到class上
        }

        foreach (var item in arguments)
        {
            System.CodeDom.CodeMemberField field = new CodeMemberField();
            field.Type = new CodeTypeReference(item.type);
            field.Name = item.name;
            field.Attributes = MemberAttributes.Public;
            if (!string.IsNullOrEmpty(item.defultValue))
            {
                var value = Convert.ChangeType(item.defultValue, Type.GetType(item.type));
                field.InitExpression = new CodePrimitiveExpression(value);
            }

            wrapProxyClass.Members.Add(field);
        }
        return wrapProxyClass;
    }

    private static CodeTypeDeclaration GenerateEnum(Type type, List<Argument> arguments)
    {
        CodeTypeDeclaration warpEnum = new CodeTypeDeclaration(type.Name);
        warpEnum.IsEnum = true;
        foreach (var item in arguments)
        {
            System.CodeDom.CodeMemberField field = new CodeMemberField();
            field.Type = new CodeTypeReference(item.type);
            field.Name = item.name;
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
        object instence = null;
        GameObject temp = null;
        if (type.IsSubclassOf(typeof(MonoBehaviour)) && !type.IsAbstract)
        {
            temp = new GameObject("temp");
            instence = temp.AddComponent(type);
        }
        else
        {
            try
            {
                if (Array.Find(type.GetConstructors(), x => x.IsPublic) != null)
                {
                    instence = System.Activator.CreateInstance(type);
                }
            }
            catch (System.Exception e)
            {

            }
        }

        FieldInfo[] fields = type.GetFields(BindingFlags.GetField | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public);
        foreach (var item in fields)
        {
            if (!IsFieldNeed(item)) continue;

            if (item.FieldType.IsValueType || item.FieldType.IsEnum || item.FieldType.IsClass)
            {
                var variable = CreateArgument(item, instence);
                arguments.Add(variable);
            }
        }


        if (temp != null)
        {
            UnityEngine.Object.DestroyImmediate(temp);
        }

    }
    private static bool IsFieldNeed(FieldInfo item)
    {
        if(item.FieldType.IsInterface)
        {
            return false;
        }

        if (item.FieldType.IsPublic && item.FieldType.IsClass)
        {
            var atts = item.FieldType.GetCustomAttributes(false);
            var seri = Array.Find(atts, x => x is System.SerializableAttribute);
            if (seri == null)
            {
                return false;
            }
        }

        if (item.FieldType.IsArray || item.FieldType.IsGenericType)
        {
            var type = item.FieldType;
            Type arrayType = null;
            if (type.IsGenericType)
            {
                arrayType = type.GetGenericArguments()[0];
            }
            else 
            {
                arrayType = type.GetElementType();
            }

            if(arrayType.IsInterface)
            {
                return false;
            }

            if (!basicTypes.Contains(arrayType) && !arrayType.ToString().StartsWith("UnityEngine"))
            {
                var atts = item.FieldType.GetCustomAttributes(false);
                var seri = Array.Find(atts, x => x is System.SerializableAttribute);
                if (seri == null)
                {
                    return false;
                }
            }
        }

        if(!item.FieldType.IsPublic)
        {
            var attrs = item.GetCustomAttributes(false);
            if (attrs == null && attrs.Length > 0 || Array.Find(attrs, x => x is SerializeField) == null)
            {
                return false;
            }
        }

        if (item.Name.Contains("k__BackingField"))
        {
            return false;
        }

        return true;
    }

    internal static void LoadScriptsFromFolder(string path, List<MonoScript> behaivers)
    {
        var files = System.IO.Directory.GetFiles(path, "*.cs", System.IO.SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var dirPath = file.Replace("\\", "/").Replace(Application.dataPath, "Assets");
            var mono = LoadScriptDriect(dirPath);
            if (mono != null)
            {
                behaivers.Add(mono);
            }
        }
    }

    internal static MonoScript LoadScriptDriect(string path)
    {
        var mono = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
        if (mono != null && mono.GetClass() != null)
        {
            if(mono.GetClass().IsSubclassOf(typeof(Editor)))
            {
                return null;
            }
            else if (mono.GetClass().IsSubclassOf(typeof(MonoBehaviour)) || mono.GetClass().IsSubclassOf(typeof(ScriptableObject)))
            {
                return mono;
            }
        }
        return null;
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
    private static Argument CreateArgument(FieldInfo item, object defult = null)
    {
        Argument arg = new Argument();
        arg.name = item.Name;
        var type = item.FieldType;
        arg.type = type.ToString();
        arg.typeAssemble = type.Assembly.ToString();

       

        if (defult != null && basicTypes.Contains(type))
        {
            arg.defultValue = item.GetValue(defult) == null ? null : item.GetValue(defult).ToString();
        }

        arg.subType = "";
        if (type.IsClass || type.IsEnum || type.IsArray || type.IsGenericType)
        {
            if (type.IsArray || (!basicTypes.Contains(type) && !arg.type.StartsWith("UnityEngine")))
            {
                if (type.IsGenericType)
                {
                    var arrayType = type.GetGenericArguments()[0];
                    if (!basicTypes.Contains(arrayType) && !arrayType.ToString().StartsWith("UnityEngine"))
                    {
                        arg.subType = arrayType.ToString();
                        arg.subAssemble = arrayType.Assembly.ToString();
                    }
                }
                else if (type.IsArray)
                {
                    Debug.Log("type.IsArray:" + type);
                    var arrayType = type.GetElementType();
                    if (!basicTypes.Contains(arrayType) && !arrayType.ToString().StartsWith("UnityEngine"))
                    {
                        arg.subType = arrayType.ToString();
                        arg.subAssemble = arrayType.Assembly.ToString();
                    }
                }
                else
                {
                    arg.subType = type.ToString();
                    arg.subAssemble = type.Assembly.ToString();
                }
            }
        }


        return arg;
    }


}
