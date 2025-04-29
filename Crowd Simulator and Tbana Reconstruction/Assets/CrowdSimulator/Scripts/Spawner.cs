using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class Spawner : MonoBehaviour {

	public enum Method{
		uniformSpawn,
		circleSpawn,
		discSpawn,
		continuousSpawn,
		areaSpawn
	}

	private int node;	//The node for this spawner
	private Main mainScript;

	public Method spawnMethod;
	public int numberOfAgents;

	// Continuous spawn
	public bool useGroupedAgents;
	public float individualAgents;
	public float percentOfTwoInGroup;
	public float percentOfThreeInGroup;
	public float percentOfFourInGroup;
	public bool useSimpleAgents;

	// Circle and disc spawn
	public float circleRadius;
	public int numberOfDiscRows;

	// Area spawn
	public int rows;
	public int rowLength;

	// Waiting agents
	public bool waitingAgents;
	private WaitingAreaController waitingAreaController;
	public bool subwayAgents;

	//Common items for this spawner
	internal GameObject agentModels;
	internal GameObject subgroupModels;
	internal List<int> subgroupModelsParentIndex;

	internal Agent manShirtColor;
	internal List<Agent> agentList; //Reference to global agentlist
	internal MapGen.map map; //map of available spawns / goals
	Vector2 X, Z; //Information about plane sizes
	internal float agentAvoidanceRadius;

	internal int subgroupTag = 0; //Tag counter for subgroups
	internal Material materialColor; //Material with a color
	internal Material groupAgentMaterial; //Material with a color

	public Color agentSpawnColor = Color.black; //Default color (Changed by user in editor)
	public Color groupAgentSpawnColor = Color.black; //Default color (Changed by user in editor)

	public GameObject agentEditorContainer = null;
	public CustomNode customGoal = null;
	private int goal;

	public float spawnRate;
	public bool usePoisson = false;

	internal Dictionary<string, int> skins;

	// Set the node index for this spawner's node
	public void SetNode(int node)
	{
		this.node = node;
	}

	/**
	* Set the goal for the agents of this spawner.
	* If there is no custom goal set in the editor, the goal will be the goal node with index 0.
	*/
	private void SetGoal()
	{
		goal = map.goals[0];
		if (customGoal != null) {
			//OPT: Use dictionary in mapgen to get constant time access!
			for(int i = 0; i < map.allNodes.Count; ++i) {
				if (map.allNodes [i].transform.position == customGoal.transform.position) {
					goal = i;
					break;
				}
			}
		}
	}

	public void InitializeSpawner(ref GameObject agentModels, ref GameObject subgroupModels, ref Agent manShirtColor, 
					 ref MapGen.map map,  ref List<Agent> agentList, Vector2 X, Vector2 Z, float agentAvoidanceRadius) {
		this.agentModels = agentModels;
		this.subgroupModels = subgroupModels;
		this.manShirtColor = manShirtColor;
		this.map = map;
		this.X = X; this.Z = Z;
		this.agentAvoidanceRadius = agentAvoidanceRadius;
		this.agentList = agentList;

		this.materialColor = Materials.MakeMaterial (agentSpawnColor);
		this.groupAgentMaterial = Materials.MakeMaterial (groupAgentSpawnColor);
		skins = new Dictionary<string, int>();
		Object[] allSkins = Resources.LoadAll ("");
		for (int i = 0; i < allSkins.GetLength (0); ++i) {
			string tag = allSkins [i].name.Split ('-') [0];
			if (skins.ContainsKey (tag)) {
				skins [tag] += 1;
			} else {
				skins.Add (tag, 1);
			}
		}
		subgroupModelsParentIndex = new List<int> ();
		for(int i = 0; i < subgroupModels.transform.childCount; ++i) {
			if (subgroupModels.transform.GetChild (i).tag == "female" || subgroupModels.transform.GetChild (i).tag == "male") {
				subgroupModelsParentIndex.Add (i);
			}
		}
		SetGoal();
	}

	void Start()
	{
		mainScript = FindObjectOfType<Main>();
		waitingAreaController = FindObjectOfType<WaitingAreaController>();

		switch(spawnMethod) {
		case Method.uniformSpawn:
			agentList.AddRange(spawnRandomAgents(numberOfAgents));
			break;
		case Method.areaSpawn:
			spawnAreaAgents(rows, rowLength, node);
			break;
		case Method.circleSpawn:
			agentList.AddRange(circleSpawn(numberOfAgents, circleRadius, Mathf.Min(mainScript.planeSizeX, mainScript.planeSizeZ)));
			break;
		case Method.discSpawn:
			agentList.AddRange(discSpawn(Mathf.Min(mainScript.planeSizeX, mainScript.planeSizeZ), circleRadius, numberOfDiscRows));
			break;
		case Method.continuousSpawn:
				continousSpawn(); 
			break;
		default:
			agentList = new List<Agent> (); 
			break;
		}
	}

	

	// UNIFORM SPAWN
	/**
	 * Spawn a number of regular agents uniformly placed accross the world grid specified by X and Z vectors.
	 * Agents are guaranteed to be spawned on a location not obstructed by a static obstacle.
	 **/
	public List<Agent> spawnRandomAgents(int numberOfAgents) {
		List<Agent> agents = new List<Agent> ();
		int goal = 0;
		if (customGoal == null) {
			Debug.Log ("Please set a goal in the spawner");
			return new List<Agent>();
		}
		//OPT: Use dictionary in mapgen to get constant time access!
		for (int i = 0; i < map.allNodes.Count; ++i) {
			if (map.allNodes [i].transform.position == customGoal.transform.position) {
				goal = i;
				break;
			}
		}

		for (int i = 0; i < numberOfAgents; ++i) {
			Vector3 pos = new Vector3 (Random.Range (X.x, X.y), 10.0f, Random.Range (Z.x, Z.y));
			materialColor = Materials.GetMaterial (Random.Range (0, Settings.numberOfColors)); //Random colors
			while (Physics.Raycast (pos, new Vector3 (0.0f, -1.0f, 0.0f), 20f)) { //Check to see if place occupied by static obstacle
				pos.x = Random.Range (X.x, X.y);
				pos.z = Random.Range (Z.x, Z.y);
			}
			pos.y = 0f;
			Agent a = Instantiate (agentModels.transform.GetChild(Random.Range(0, agentModels.transform.childCount)).GetComponent<Agent>()) as Agent;
			a.transform.position = pos;
			float closest = -1;
			int start = -1;
			bool init = false;
			//Find the closest available customNode as a start node. O(n) time where n is the number of nodes in the world.
			for (int j = 0; j < map.allNodes.Count; ++j) {
				if (!Physics.Raycast (pos, (map.allNodes [j].transform.position - pos).normalized, (map.allNodes [j].transform.position-pos).magnitude)) {
					if (map.allNodes[j].transform.position != transform.position && (!init ||  (map.allNodes [j].transform.position-pos).magnitude < closest)) {
						closest = (map.allNodes [j].transform.position - pos).magnitude;
						start = j;
						init = true;
					} 
				}
			}
			if (start < 0 || goal < 0) {
				Debug.Log (a.transform.position);
				Debug.Log ("Insufficient goal nodes in the map. Please place more in empty environments or use a higher spawn-rate of sampled nodes");
				return new List<Agent> ();
			}

			a.path = map.shortestPaths [start] [goal];
			a.pathIndex = 0; //Walk towards first node
			a.preferredVelocity = (map.allNodes[a.path[a.pathIndex]].getTargetPoint(pos) - pos).normalized;

			if (a.transform.childCount > 0) { //Does the agent have a mesh to color?
				Renderer ss = a.transform.GetChild (0).GetComponent<Renderer> ();
				if (ss != null)
					ss.material.mainTexture = (Texture)Resources.Load (a.tag+"-"+Random.Range(0, skins[a.tag]));
			}
			agents.Add (a);
		}
		return agents;
	}

	// AREA SPAWN
	public void AreaSpawn()
	{
		spawnAreaAgents(rows, rowLength, node);
	}

	/**
	 * Spawn agents centered around this spawner in a rectangular pattern all at once. 
	 * Specified is the percentage of grouped agents compared to individual agents.
	 **/
	void spawnAreaAgents(int rows, int rowLength, int startNode) {
		Vector3 startPos = transform.position - (transform.right * rowLength / 2);
		
		for (int i = 0; i < rows; ++i) {
			for (int j = 0; j < rowLength; ++j) {
				Vector3 posVector = startPos + (transform.right * j) + (transform.right *i);
				posVector.x += 1.5f * j; posVector.z += 1.5f * i; posVector.y = 0.0f;
				spawnOneAgent(posVector);
			}
		}
	}

	// CIRCLE SPAWN
	internal List<Agent> circleSpawn(int numberOfAgents, float r, float planeScale){
		Color[] colors = {Color.green, Color.yellow, Color.red, Color.magenta, 0.15f*Color.white+Color.blue, Color.cyan};
		Vector3 agentPos = new Vector3(0f, 0f, 0f);
		if (r > planeScale* 5 - agentAvoidanceRadius) 
		{
			r = planeScale * 5 - agentAvoidanceRadius;
		}

		agentPos.Set(r, 0.5f, 0f);
		float phi = 360 / (float)numberOfAgents;
		List<Agent> li = new List<Agent> ();
		for (int n = 0; n < numberOfAgents; n++) {
			Agent a = Instantiate (manShirtColor);

			a.transform.position = agentPos;
			a.transform.RotateAround(new Vector3(0f, 0f, 0f), new Vector3(0f, 1f, 0f), (float)(n*phi));
			a.noMap = true;
			a.noMapGoal = new Vector3 (-a.transform.position.x, a.transform.position.y, -a.transform.position.z);
			int index = (int)((n*phi)/60);
			if (a.transform.childCount > 0) {
				SkinnedMeshRenderer smr = a.transform.GetChild (1).GetComponent<SkinnedMeshRenderer> ();
				if (smr != null)
					smr.sharedMaterial = Materials.GetMaterial (index);
			} else {
				MeshRenderer mr = a.GetComponent<MeshRenderer>();
				if(mr != null && mr.materials.GetLength(0) > 0) {
					mr.materials[0].color = Materials.colors[index];
				}
			}
			li.Add (a);
		}
		return li;
	}

	// DISC SPAWN
	internal List<Agent> discSpawn(float planeScale, float startRadius, int numberOfRows) {
		float r;
		int numberOfAgents;
		float d = 0.4f + agentAvoidanceRadius * 2f;
		List<Agent> li = new List<Agent> ();
		for (int n = 0; n < numberOfRows; n++) {
			r = startRadius+n*agentAvoidanceRadius*2f;
			numberOfAgents = (int)((2*Mathf.PI*r)/d);
			li.AddRange(circleSpawn(numberOfAgents, r, planeScale));
		}
		return li;
	}

	// CONTINUOUS SPAWN
	public void continousSpawn() {
		StartCoroutine (spawnContinously(spawnRate));
	}

	internal IEnumerator spawnContinously(float continousSpawnRate) {
		Transform spawnerNode = transform.GetChild(0);

		if(usePoisson)
		{
			float timeBetweenSpawn = CalculateTimeBetweenSpawns();
			yield return new WaitForSeconds (timeBetweenSpawn);
			
		}
		else
		{
			yield return new WaitForSeconds (continousSpawnRate);
		}
		
		if (agentList.Count + waitingAreaController.waitingAgents.Count < mainScript.maxNumberOfAgents) {
			Vector3 startPos = new Vector3 (Random.Range (-0.5f, 0.5f), 0f, Random.Range (-0.5f, 0.5f)); 
			startPos = spawnerNode.TransformPoint (startPos);
			Debug.DrawLine(startPos, startPos + Vector3.up * 0.5f, Color.red, 10f);
			float randomRange = Random.Range(0.0f, 1.0f);
			if (!useGroupedAgents || randomRange < individualAgents) {
				spawnOneAgent(startPos);
			} else {
				int groupSize;
				if (randomRange - individualAgents < percentOfTwoInGroup) {
					groupSize = 2;
				} else if (randomRange - individualAgents - percentOfTwoInGroup < percentOfThreeInGroup) {
					groupSize = 3;
				} else {
					groupSize = 4;
				}
				List<SubgroupAgent> liA = InitGroupAgent (groupSize, startPos, node, goal);
				for (int i = 0; i < liA.Count; ++i) {
					agentList.Add ((Agent)liA [i]);
				}
			}
		}
		
		StartCoroutine (spawnContinously(continousSpawnRate));
	}

	// BURST SPAWN
	public IEnumerator BurstSpawn(int nAgents, float burstRate)
	{
		for (int i = 0; i < nAgents; ++i) {
			Vector3 startPos = new Vector3(transform.position.x + Random.Range(-1.5f, 1.5f), transform.position.y, transform.position.z + Random.Range(-1.5f, 1.5f));
			spawnOneAgent (startPos);
			yield return new WaitForSeconds (burstRate);
		}

	}

	public void spawnOneAgent(Vector3 startPosition)
	{
		Agent agent;
		if (useSimpleAgents) 
		{
			agent = Instantiate (manShirtColor);
		} else 
		{
			agent = Instantiate (agentModels.transform.GetChild(Random.Range(0, agentModels.transform.childCount)).GetComponent<Agent>());
		}

		int agentGoal = goal;

		int trainLine = Random.Range(1,3);
		if(subwayAgents)
		{
			
			agent.subwayData = new Agent.SubwayData(trainLine);

		}

		if(waitingAgents)
		{
			// Find a waiting area goal for the agent. If there are no free waiting area spots their goal will be the ordinary goal for this spawner.
			(int waitingArea,int waitingSpot) waitingAreaSpot = waitingAreaController.GetWaitingAreaSpotNew(startPosition, trainLine);
			if(waitingAreaSpot.waitingArea != -1)
			{
				agent.setWaitingAgent(true);
				agentGoal = waitingAreaSpot.waitingArea;
				agent.waitingSpot = waitingAreaSpot.waitingSpot;
				agent.waitingArea = map.allNodes[waitingAreaSpot.waitingArea].GetComponent<WaitingArea>();
			}
		}

		agent.InitializeAgent (startPosition, node, agentGoal, ref map);
		agent.ApplyMaterials(materialColor, ref skins);

		if (agentEditorContainer != null)
			agent.transform.parent = agentEditorContainer.transform;

		agentList.Add (agent);
	}

	// GROUPS
	private SubgroupAgent getGroupModel(bool fixedParent, bool leader) {
		SubgroupAgent model;
		if (useSimpleAgents) {
			model = manShirtColor.gameObject.GetComponent<SubgroupAgent> ();
			model.gameObject.GetComponent<Agent> ().enabled = false;
		} else {
			if (fixedParent && leader) {
				model = subgroupModels.transform.GetChild (subgroupModelsParentIndex [Random.Range (0, subgroupModelsParentIndex.Count)]).GetComponent<SubgroupAgent> ();
			} else {
				model = subgroupModels.transform.GetChild (Random.Range (0, subgroupModels.transform.childCount)).GetComponent<SubgroupAgent>();
			}
		}
		return model;
	}
	//Supports 4 followers
	private List<SubgroupAgent> InitGroupAgent(int groupSize, Vector3 pos, int start, int goal, Material argMat = null) {
		bool fixedParent = true;
		List<SubgroupAgent> gr = new List<SubgroupAgent> ();

		SubgroupAgent leader = Instantiate (getGroupModel(fixedParent, true));
	
		leader.isLeader = true; 
		leader.transform.position = pos;
		List<Vector3> followerPositions = new List<Vector3> (3); 
		followerPositions.Add (pos);
		float usedValue = 0.6f;//Grid.instance.agentAvoidanceRadius;
		followerPositions.Add (leader.transform.TransformPoint (0.0f, 0.0f, usedValue));
		followerPositions.Add (leader.transform.TransformPoint (0.0f, 0.0f, -usedValue));	
		followerPositions.Add (leader.transform.TransformPoint (0.0f, 0.0f, 2*usedValue));
		followerPositions.Add (leader.transform.TransformPoint (0.0f, 0.0f, -2*usedValue));
		gr.Add (leader);
		for (int i = 0; i < groupSize - 1; ++i) {
			SubgroupAgent follower = Instantiate (getGroupModel(fixedParent, false));
			gr.Add (follower);
		}
		SubgroupAgent.companions comp = new SubgroupAgent.companions (gr, 0, transform.gameObject.name + subgroupTag.ToString());
		subgroupTag++;
		for (int i = 0; i < gr.Count; ++i) {
			gr [i].groupMemberNumber = i; gr [i].number = i;
			gr [i].c = comp;
			Agent sa = gr [i];
			sa.InitializeAgent (followerPositions [i], start, goal, ref map);
			sa.ApplyMaterials(materialColor, ref skins, groupAgentMaterial);
			if (agentEditorContainer != null)
				sa.transform.parent = agentEditorContainer.transform;
		}
		return gr;
	}

	float CalculateTimeBetweenSpawns()
    {
        float u = Random.value;
        // -ln(1-u)/λ
        return -Mathf.Log(1 - u) / spawnRate;
    }
}
