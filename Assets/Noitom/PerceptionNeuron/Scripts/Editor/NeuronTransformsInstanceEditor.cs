using UnityEngine;
using System.Collections;
using UnityEditor;
using Neuron;

[CustomEditor(typeof(Neuron.NeuronTransformsInstance))]
public class NeuronTransformsInstanceEditor : Editor 
{

    // https://catlikecoding.com/unity/tutorials/editor/custom-list/
    SerializedProperty transformsField;
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
        Neuron.NeuronTransformsInstance script = (Neuron.NeuronTransformsInstance)target;

        int preUseNewRig = (int)script.skeletonType;
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
        DrawDefaultInspector();
        if(preUseNewRig != (int)script.skeletonType)
        {
            if (script.root == null)
                script.root = script.transform;

            for (int i = 0; i < script.transforms.Length; i++)
                script.transforms[i] = null;
            script.Bind(script.transform, script.prefix);

            EditorUtility.SetDirty(script);

            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        }

        if (script.motionUpdateMethod != Neuron.UpdateMethod.Normal)
        {
            EditorGUILayout.PropertyField(physicalReferenceOverrideField);
            serializedObject.ApplyModifiedProperties();
        }
        GUILayout.Space(10);
        if (GUILayout.Button("bind"))
        {
            Debug.Log("[NeuronTransformsInstanceVR] - LOAD all Transform references into the bones list!");

            script.Bind(script.transform, script.prefix);

            EditorUtility.SetDirty(script);

            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

        }

        // 这种做法适合不定长数组,对定长数组不起作用
        //EditorGUILayout.PropertyField(serializedObject.FindProperty("transforms"), new GUIContent("transforms"), true);
        serializedObject.Update();
        if(transformsField == null)
            transformsField = serializedObject.FindProperty("transforms");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("transforms"), new GUIContent("transforms"), true);

        EditorGUI.indentLevel += 1;
       // if (transformsField.isExpanded)
        {
            //EditorGUILayout.PropertyField(transformsField.FindPropertyRelative("Array.size"));
            for (int i = 0; i < transformsField.arraySize; i++)
            {
                if ((script.skeletonType == (int)(NeuronEnums.SkeletonType.PerceptionNeuronStudio)) && (i == (int)Neuron.NeuronBones.Spine3 || i == (int)Neuron.NeuronBones.Neck))
                    EditorGUILayout.PropertyField(transformsField.GetArrayElementAtIndex(i), new GUIContent(((Neuron.NeuronBonesV2)i).ToString()));
                else
                    EditorGUILayout.PropertyField(transformsField.GetArrayElementAtIndex(i), new GUIContent(((Neuron.NeuronBones)i).ToString()));
            }
        }
        EditorGUI.indentLevel -= 1;
        serializedObject.ApplyModifiedProperties();

    }
}