using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(NeuronTransformsInstanceVR))]
public class NeuronTransformsInstanceVREditor : Editor 
{
    SerializedProperty addressField;
    SerializedProperty tcpPortField;
    SerializedProperty udpPortField;
    public override void OnInspectorGUI()
	{
		NeuronTransformsInstanceVR vrScript = (NeuronTransformsInstanceVR)target;

        if (addressField == null)
        {
            addressField = serializedObject.FindProperty("address");
            tcpPortField = serializedObject.FindProperty("portTcp");
            udpPortField = serializedObject.FindProperty("portUdp");
        }
        EditorGUILayout.PropertyField(addressField);
        if (vrScript.socketType == Neuron.NeuronConnection.SocketType.TCP)
            EditorGUILayout.PropertyField(tcpPortField);
        else if (vrScript.socketType == Neuron.NeuronConnection.SocketType.UDP)
            EditorGUILayout.PropertyField(udpPortField);
        serializedObject.ApplyModifiedProperties();
        DrawDefaultInspector ();


		if(GUILayout.Button("Load bone references"))
		{
			Debug.Log ("[NeuronTransformsInstanceVR] - LOAD all Transform references into the bones list!");

			vrScript.Bind( vrScript.root, vrScript.prefix );
			vrScript.editorScriptHasLoadedBones = true;

			EditorUtility.SetDirty (vrScript);
		}

		if(GUILayout.Button("Clear bone references"))
		{
			Debug.Log ("[NeuronTransformsInstanceVR] - CLEAR all Transform references in the bones list!");

			for (int i=0; i < vrScript.bones.Length; i++){
				vrScript.bones[i] = null;
			}

			vrScript.editorScriptHasLoadedBones = false;

			EditorUtility.SetDirty (vrScript);
		}



	}
}