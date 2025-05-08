/**
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NewSpawner)), CanEditMultipleObjects]
public class NewSpawnerEditor : Editor {

    private Main mainScript;
    private SerializedObject mainSerializedObject;

    public SerializedProperty
        useSimpleAgents_Prop,
		agentEditorContainer_Prop,
		customGoal_Prop,
		spawnRate_Prop,
		waitingAgents_Prop,
		subwayAgents_Prop,
		usePoisson_Prop,
        agentPrefab_Prop;
	
	private SerializedProperty
		avoidanceRadius_Prop,
		planeSize_Prop;




    void OnEnable () {

        mainScript = FindObjectOfType<Main>();
        if (mainScript != null)
        {
            mainSerializedObject = new SerializedObject(mainScript);
			avoidanceRadius_Prop = mainSerializedObject.FindProperty ("agentAvoidanceRadius");
			planeSize_Prop = mainSerializedObject.FindProperty ("planeSize"); 
        } else { Debug.Log("Main script not found"); }

        useSimpleAgents_Prop = serializedObject.FindProperty ("useSimpleAgents");
		agentEditorContainer_Prop = serializedObject.FindProperty ("agentEditorContainer");
		customGoal_Prop = serializedObject.FindProperty ("customGoal");
		spawnRate_Prop = serializedObject.FindProperty ("spawnRate");
		waitingAgents_Prop = serializedObject.FindProperty ("waitingAgents");
		subwayAgents_Prop = serializedObject.FindProperty ("subwayAgents");
		usePoisson_Prop = serializedObject.FindProperty ("usePoisson");
        agentPrefab_Prop = serializedObject.FindProperty ("agentPrefab");
    }

    public override void OnInspectorGUI() {
        serializedObject.Update ();
        mainSerializedObject.Update();

		EditorGUILayout.PropertyField(agentEditorContainer_Prop);
		EditorGUILayout.PropertyField(customGoal_Prop);
		EditorGUILayout.Space();
 
        EditorGUILayout.PropertyField(spawnRate_Prop);
        EditorGUILayout.PropertyField(usePoisson_Prop);
        EditorGUILayout.PropertyField(waitingAgents_Prop);
        EditorGUILayout.PropertyField(subwayAgents_Prop);
        EditorGUILayout.PropertyField(useSimpleAgents_Prop);
        EditorGUILayout.PropertyField(agentPrefab_Prop);


        serializedObject.ApplyModifiedProperties ();
        mainSerializedObject.ApplyModifiedProperties();
    }

}
*/