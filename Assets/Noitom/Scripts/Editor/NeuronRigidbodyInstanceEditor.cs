using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(Neuron.NeuronRigidbody))]
public class NeuronRigidbodyInstanceEditor : Editor 
{
    SerializedProperty rigidbodyField;
    //void OnEnable()
    //{
    //    // Setup the SerializedProperties.
    //    transformsField = serializedObject.FindProperty("transforms");
    //}

    public override void OnInspectorGUI()
    {
        Neuron.NeuronRigidbody script = (Neuron.NeuronRigidbody)target;
        rigidbodyField = serializedObject.FindProperty("rigidbodyId");
        EditorGUILayout.PropertyField(rigidbodyField);
        serializedObject.ApplyModifiedProperties();
        //DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

    }
}