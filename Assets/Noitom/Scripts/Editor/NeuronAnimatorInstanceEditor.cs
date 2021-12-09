using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(NeuronAnimatorInstance))]
public class NeuronAnimatorInstanceEditor : Editor 
{
    SerializedProperty addressField;
    SerializedProperty tcpPortField;
    SerializedProperty udpPortField;
    SerializedProperty physicalReferenceOverrideField;
    SerializedProperty disableBoneMovementField;
    //void OnEnable()
    //{
    //    // Setup the SerializedProperties.
    //    transformsField = serializedObject.FindProperty("transforms");
    //}

    public override void OnInspectorGUI()
    {
        NeuronAnimatorInstance script = (NeuronAnimatorInstance)target;

        if (addressField == null)
        {
            addressField = serializedObject.FindProperty("address");
            tcpPortField = serializedObject.FindProperty("portTcp");
            udpPortField = serializedObject.FindProperty("portUdp");
            physicalReferenceOverrideField = serializedObject.FindProperty("physicalReferenceOverride");
        }

        if (script.socketType == Neuron.NeuronEnums.SocketType.TCP)
        {
            EditorGUILayout.PropertyField(addressField);
            EditorGUILayout.PropertyField(tcpPortField);
        }
        else if (script.socketType == Neuron.NeuronEnums.SocketType.UDP)
            EditorGUILayout.PropertyField(udpPortField);
        serializedObject.ApplyModifiedProperties();

        if (disableBoneMovementField == null)
            disableBoneMovementField = serializedObject.FindProperty("disableBoneMovement");

        DrawDefaultInspector();
        GUILayout.Space(10);
        GUILayout.Label("disable bone movement: ");
        for (int i = 0; i < disableBoneMovementField.arraySize; i++)
        {
            EditorGUILayout.PropertyField(disableBoneMovementField.GetArrayElementAtIndex(i), new GUIContent(((HumanBodyBones)i).ToString()));
        }
        serializedObject.ApplyModifiedProperties();

        serializedObject.Update();

        if (script.motionUpdateMethod != Neuron.UpdateMethod.Normal)
        {
            EditorGUILayout.PropertyField(physicalReferenceOverrideField);
            serializedObject.ApplyModifiedProperties();
        }

        
        serializedObject.ApplyModifiedProperties();

    }
}