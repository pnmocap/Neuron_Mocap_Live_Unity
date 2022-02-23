using UnityEngine;
using System.Collections;
using UnityEditor;
using Neuron;

[CustomEditor(typeof(Neuron.NeuronSourceManager))]
public class NeuronSourceManagerEditor : Editor 
{
    SerializedProperty addressField;
    SerializedProperty tcpPortField;
    SerializedProperty udpPortField;
    SerializedProperty udpServerPortField;
    SerializedProperty tcpOrUdpField;

    public override void OnInspectorGUI()
    {
        NeuronSourceManager script = (NeuronSourceManager)target;

        if (addressField == null)
        {
            addressField = serializedObject.FindProperty("address");
            tcpPortField = serializedObject.FindProperty("portTcp");
            udpServerPortField = serializedObject.FindProperty("portUdpServer");
            udpPortField = serializedObject.FindProperty("portUdp");
            tcpOrUdpField = serializedObject.FindProperty("socketType");
        }
        EditorGUILayout.PropertyField(addressField);
        if (script.socketType == Neuron.NeuronEnums.SocketType.TCP)
            EditorGUILayout.PropertyField(tcpPortField);
        else if (script.socketType == Neuron.NeuronEnums.SocketType.UDP)
        {
            EditorGUILayout.PropertyField(udpServerPortField);
            EditorGUILayout.PropertyField(udpPortField);
        }

        EditorGUILayout.PropertyField(tcpOrUdpField);
        serializedObject.ApplyModifiedProperties();

    }
}