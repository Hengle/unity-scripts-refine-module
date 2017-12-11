using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CreateAssetMenu(menuName = "生成/脚本提炼")]
public class RefineObj : ScriptableObject
{
    public string exportPath;
    public List<RefineItem> refineList = new List<RefineItem>();
}
