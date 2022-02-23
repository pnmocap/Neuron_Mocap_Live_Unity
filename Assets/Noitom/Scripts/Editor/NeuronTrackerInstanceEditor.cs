using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(Neuron.NeuronTracker))]
public class NeuronTrackerInstanceEditor : Editor 
{
    SerializedProperty rigidbodyField;
    //void OnEnable()
    //{
    //    // Setup the SerializedProperties.
    //    transformsField = serializedObject.FindProperty("transforms");
    //}

    public override void OnInspectorGUI()
    {
        Neuron.NeuronTracker script = (Neuron.NeuronTracker)target;

        rigidbodyField = serializedObject.FindProperty("deviceName"); 

        EditorGUILayout.PropertyField(rigidbodyField);
        serializedObject.ApplyModifiedProperties();
        //DrawDefaultInspector();
        //serializedObject.ApplyModifiedProperties();

    }
}