using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomEditor(typeof(AddNetworkPlayerDebugScriptMonoBehaviour))]
public class AddNetworkPlayerDebugScriptEditor : Editor
{
    //https://learn.unity.com/tutorial/editor-scripting
    public override void OnInspectorGUI()
    {
        AddNetworkPlayerDebugScriptMonoBehaviour myScript = (AddNetworkPlayerDebugScriptMonoBehaviour)target;
        if (GUILayout.Button("Spawn new client player"))
        {
            GameObject.Instantiate(myScript.clientPrefab);
        }

        base.OnInspectorGUI();
    }
}
#endif