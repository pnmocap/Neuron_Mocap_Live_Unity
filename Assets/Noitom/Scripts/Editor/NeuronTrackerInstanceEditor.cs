using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(Neuron.NeuronTracker))]
public class NeuronTrackerInstanceEditor : Editor 
{
    SerializedProperty addressField;
    SerializedProperty tcpPortField;
    SerializedProperty udpPortField;
    SerializedProperty udpServerPortField;
    SerializedProperty tcpOrUdpField;
    SerializedProperty rigidbodyField;
    //void OnEnable()
    //{
    //    // Setup the SerializedProperties.
    //    transformsField = serializedObject.FindProperty("transforms");
    //}

    public override void OnInspectorGUI()
    {
        Neuron.NeuronTracker script = (Neuron.NeuronTracker)target;

        if (addressField == null)
        {
            addressField = serializedObject.FindProperty("address");
            tcpPortField = serializedObject.FindProperty("portTcp");
            udpPortField = serializedObject.FindProperty("portUdp");
            udpServerPortField = serializedObject.FindProperty("portUdpServer");
            tcpOrUdpField = serializedObject.FindProperty("socketType");
            rigidbodyField = serializedObject.FindProperty("deviceName");
        }
        EditorGUILayout.PropertyField(addressField);
        if (script.socketType == Neuron.NeuronEnums.SocketType.TCP)
            EditorGUILayout.PropertyField(tcpPortField);
        else if (script.socketType == Neuron.NeuronEnums.SocketType.UDP)
        {
            EditorGUILayout.PropertyField(udpPortField);
            EditorGUILayout.PropertyField(udpServerPortField);
        }

        EditorGUILayout.PropertyField(tcpOrUdpField);
        EditorGUILayout.PropertyField(rigidbodyField);
        serializedObject.ApplyModifiedProperties();
        //DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

    }
}