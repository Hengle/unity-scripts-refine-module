using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class AttributeInfo  {
    public string attribute;
    public SupportAttributes attType = SupportAttributes.NoArgument;
    public string[] keys;
    public string[] values;

    public enum SupportAttributes
    {
        NoArgument,
        RequireComponent,
        CreateAssetMenu,
    }
}
