using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class NewSpawner : MonoBehaviour {

	private int node;	//The node for this spawner
	private Main mainScript;
	public bool useSimpleAgents;

	// Waiting agents
	public bool waitingAgents;
	private WaitingAreaController waitingAreaController;
	public bool subwayAgents;

	internal List<Agent> agentList; //Reference to global agentlist
	internal MapGen.map map; //map of available spawns / goals
	Vector2 X, Z; //Information about plane sizes
	internal float agentAvoidanceRadius;

	public GameObject agentEditorContainer = null;
	public CustomNode customGoal = null;
	private int goal;

	public float spawnRate;
	public bool usePoisson = false;
    public Agent agentPrefab;


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

	public void InitializeSpawner(ref MapGen.map map,  ref List<Agent> agentList, Vector2 X, Vector2 Z, float agentAvoidanceRadius) {
		this.map = map;
		this.X = X; this.Z = Z;
		this.agentAvoidanceRadius = agentAvoidanceRadius;
		this.agentList = agentList;
		SetGoal();
	}

	void Start()
	{
		mainScript = FindObjectOfType<Main>();
		waitingAreaController = FindObjectOfType<WaitingAreaController>();

		continousSpawn(); 
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
		
		if (agentList.Count < mainScript.maxNumberOfAgents) 
        {
			Vector3 startPos = new Vector3 (Random.Range (-0.5f, 0.5f), 0f, Random.Range (-0.5f, 0.5f)); 
			startPos = spawnerNode.TransformPoint (startPos);
			spawnOneAgent(startPos);
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
		agent = Instantiate (agentPrefab);

		int agentGoal = goal;

		int trainLine = Random.Range(1,3);
		if(subwayAgents)
		{
			
			agent.trainLine = trainLine;

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
		//agent.ApplyMaterials(materialColor, ref skins);

		if (agentEditorContainer != null)
			agent.transform.parent = agentEditorContainer.transform;

		agentList.Add (agent);
	}


	float CalculateTimeBetweenSpawns()
    {
        float u = Random.value;
        // -ln(1-u)/λ
        return -Mathf.Log(1 - u) / spawnRate;
    }
}
