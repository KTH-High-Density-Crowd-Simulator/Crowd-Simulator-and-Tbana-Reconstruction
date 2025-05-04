using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Main)), CanEditMultipleObjects]
public class PropertyHolderEditor : Editor {

	public SerializedProperty
		agentMaxSpeed_Prop,
		agentMinSpeed_Prop,
		alpha_Prop,
		avoidanceRadius_Prop,
		customTimeStep_Prop,
		gridPrefab_Prop,
		handleCollison_Prop,
		lcpsolver_Prop,
		lcpsolverEpsilon_Prop,
		mapGen_Prop,
		neighbourBins_Prop,
		numberOfCells_Prop,
		planePrefab_Prop,
		planeSizeX_Prop,
		planeSizeZ_Prop,
		roadNode_Prop,
		skip_Prop,
		solverIterations_Prop,
		smoothTurns_Prop,
		timeStep_Prop,
		usePresetGroupDistances_Prop,
		visualizeDensity_Prop,
		visualizeVelocity_Prop,
		visibleMap_Prop,
		walkBack_Prop,
		maxNumberOfAgents_Prop;
		

		

	
	void OnEnable () {
		// Setup the SerializedProperties
		agentMaxSpeed_Prop = serializedObject.FindProperty ("agentMaxSpeed");
		agentMinSpeed_Prop = serializedObject.FindProperty ("agentMinSpeed");
		alpha_Prop = serializedObject.FindProperty ("alpha");
		avoidanceRadius_Prop = serializedObject.FindProperty ("agentAvoidanceRadius");
		customTimeStep_Prop = serializedObject.FindProperty ("customTimeStep");
		gridPrefab_Prop = serializedObject.FindProperty ("gridPrefab");
		handleCollison_Prop = serializedObject.FindProperty ("handleCollision");
		lcpsolver_Prop = serializedObject.FindProperty ("solver");
		lcpsolverEpsilon_Prop = serializedObject.FindProperty ("epsilon");
		mapGen_Prop = serializedObject.FindProperty ("mapGen");
		neighbourBins_Prop = serializedObject.FindProperty ("neighbourBins");
		numberOfCells_Prop = serializedObject.FindProperty ("cellsPerRow");
		planePrefab_Prop = serializedObject.FindProperty ("plane");
		planeSizeX_Prop = serializedObject.FindProperty ("planeSizeX");
		planeSizeZ_Prop = serializedObject.FindProperty ("planeSizeZ");
		roadNode_Prop = serializedObject.FindProperty ("roadNodeAmount");
		solverIterations_Prop = serializedObject.FindProperty ("solverMaxIterations");
		skip_Prop = serializedObject.FindProperty ("skipNodeIfSeeNext");
		smoothTurns_Prop = serializedObject.FindProperty ("smoothTurns");
		timeStep_Prop = serializedObject.FindProperty ("timeStep");
		usePresetGroupDistances_Prop = serializedObject.FindProperty ("usePresetGroupDistances");
		visualizeDensity_Prop = serializedObject.FindProperty ("showSplattedDensity");
		visualizeVelocity_Prop = serializedObject.FindProperty ("showSplattedVelocity");
		visibleMap_Prop = serializedObject.FindProperty ("visibleMap");
		walkBack_Prop = serializedObject.FindProperty ("walkBack");
		maxNumberOfAgents_Prop = serializedObject.FindProperty("maxNumberOfAgents");
	}
	
	public override void OnInspectorGUI() {
		serializedObject.Update ();
		EditorGUILayout.PropertyField(maxNumberOfAgents_Prop);
		EditorGUILayout.PropertyField(planeSizeX_Prop);
		EditorGUILayout.PropertyField(planeSizeZ_Prop);
		EditorGUILayout.PropertyField(roadNode_Prop);
		EditorGUILayout.PropertyField(numberOfCells_Prop);
		EditorGUILayout.PropertyField(neighbourBins_Prop);
		EditorGUILayout.PropertyField(agentMaxSpeed_Prop);
		EditorGUILayout.PropertyField(agentMinSpeed_Prop);
		EditorGUILayout.PropertyField (customTimeStep_Prop);
		if (customTimeStep_Prop.boolValue) {
			EditorGUILayout.Slider(timeStep_Prop, 0.01f, 0.05f);
		}
		EditorGUILayout.PropertyField(alpha_Prop);
		EditorGUILayout.Space();
		EditorGUILayout.PropertyField(lcpsolver_Prop);
		EditorGUILayout.PropertyField (solverIterations_Prop);
		EditorGUILayout.PropertyField (lcpsolverEpsilon_Prop);
		EditorGUILayout.Space();
		EditorGUILayout.PropertyField(visualizeDensity_Prop);
		EditorGUILayout.PropertyField(visualizeVelocity_Prop);
		EditorGUILayout.PropertyField(visibleMap_Prop);
		EditorGUILayout.PropertyField(walkBack_Prop);
		EditorGUILayout.PropertyField(skip_Prop);
		EditorGUILayout.PropertyField (smoothTurns_Prop);
		EditorGUILayout.PropertyField (handleCollison_Prop);
		EditorGUILayout.Space();
		EditorGUILayout.PropertyField(avoidanceRadius_Prop);
		EditorGUILayout.PropertyField(usePresetGroupDistances_Prop);
		EditorGUILayout.Space();
		EditorGUILayout.PropertyField(gridPrefab_Prop);
		EditorGUILayout.PropertyField(mapGen_Prop);
		EditorGUILayout.PropertyField(planePrefab_Prop);
		
		serializedObject.ApplyModifiedProperties ();
	}
}