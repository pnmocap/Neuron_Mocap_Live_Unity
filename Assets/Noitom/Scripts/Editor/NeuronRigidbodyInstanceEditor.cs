using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(Neuron.NeuronRigidbody))]
public class NeuronRigidbodyInstanceEditor : Editor 
{
    SerializedProperty addressField;
    SerializedProperty tcpPortField;
    SerializedProperty udpPortField;
    SerializedProperty tcpOrUdpField;
    SerializedProperty rigidbodyField;
    //void OnEnable()
    //{
    //    // Setup the SerializedProperties.
    //    transformsField = serializedObject.FindProperty("transforms");
    //}

    public override void OnInspectorGUI()
    {
        Neuron.NeuronRigidbody script = (Neuron.NeuronRigidbody)target;

        if (addressField == null)
        {
            addressField = serializedObject.FindProperty("address");
            tcpPortField = serializedObject.FindProperty("portTcp");
            udpPortField = serializedObject.FindProperty("portUdp");
            tcpOrUdpField = serializedObject.FindProperty("socketType");
            rigidbodyField = serializedObject.FindProperty("rigidbodyId");
        }
        EditorGUILayout.PropertyField(addressField);
        if (script.socketType == Neuron.NeuronEnums.SocketType.TCP)
            EditorGUILayout.PropertyField(tcpPortField);
        else if (script.socketType == Neuron.NeuronEnums.SocketType.UDP)
            EditorGUILayout.PropertyField(udpPortField);

        EditorGUILayout.PropertyField(tcpOrUdpField);
        EditorGUILayout.PropertyField(rigidbodyField);
        serializedObject.ApplyModifiedProperties();
        //DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

    }
}