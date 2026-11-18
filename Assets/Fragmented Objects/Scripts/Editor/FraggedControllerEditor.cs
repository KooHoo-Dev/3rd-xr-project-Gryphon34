using UnityEngine;
using System;
using UnityEditor;
/****************************************
	Copyright Unluck Software	
 	www.chemicalbliss.com																															
*****************************************/

namespace UnluckSoftware
{
    [CustomEditor(typeof(FraggedController)), CanEditMultipleObjects]

    [System.Serializable]
    public class FraggedControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            if (GUILayout.Button("Change Fragment Materials"))
            {
                foreach (FraggedController target_cs in targets)
                {
                    target_cs.fragments = target_cs.transform.Find("Fragments");
                    target_cs.ChangeMaterials();
                    target_cs.fragMaterials = null;
                    EditorUtility.SetDirty(target_cs);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}