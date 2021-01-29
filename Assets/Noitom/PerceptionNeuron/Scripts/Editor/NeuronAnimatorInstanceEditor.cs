﻿using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(NeuronAnimatorInstance))]
public class NeuronAnimatorInstanceEditor : Editor 
{
    SerializedProperty addressField;
    SerializedProperty tcpPortField;
    SerializedProperty udpPortField;
    SerializedProperty physicalReferenceOverrideField;
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
        EditorGUILayout.PropertyField(addressField);
        if (script.socketType == Neuron.NeuronConnection.SocketType.TCP)
            EditorGUILayout.PropertyField(tcpPortField);
        else if (script.socketType == Neuron.NeuronConnection.SocketType.UDP)
            EditorGUILayout.PropertyField(udpPortField);
        serializedObject.ApplyModifiedProperties();

        DrawDefaultInspector();
        serializedObject.Update();

        if (script.motionUpdateMethod != Neuron.UpdateMethod.Normal)
        {
            EditorGUILayout.PropertyField(physicalReferenceOverrideField);
            serializedObject.ApplyModifiedProperties();
        }

        
        serializedObject.ApplyModifiedProperties();

    }
}