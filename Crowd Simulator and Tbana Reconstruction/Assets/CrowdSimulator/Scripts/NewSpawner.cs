using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NewSpawner : MonoBehaviour {

	internal int node;	//The node for this spawner
	protected Main mainScript;

	// Waiting agents
	internal WaitingAreaController waitingAreaController;

	internal List<Agent> agentList; //Reference to global agentlist
	internal MapGen.map map; //map of available spawns / goals
	Vector2 X, Z; //Information about plane sizes
	internal float agentAvoidanceRadius;

	public GameObject agentEditorContainer = null;
	public CustomNode customGoal = null;
	internal int goal;

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

		int agentGoal = SetSubwayData(agent, startPosition);
		agent.InitializeAgent (startPosition, node, agentGoal, ref map);

		if (agentEditorContainer != null)
			agent.transform.parent = agentEditorContainer.transform;

		agentList.Add (agent);
		if(mainScript.trainController.isPreparingToBoard[agent.trainLine])
		{
			//mainScript.trainController.PrepareWalkingAgent(agent);
		}
	}

	internal virtual int SetSubwayData(Agent agent, Vector3 startPosition)
	{
		int agentGoal = goal;
		int trainLine = 0;
		if(mainScript.trainController.flow == TrainController.Flow.Symmetric)
		{
			trainLine = Random.Range(1,3);
		}
		else if(mainScript.trainController.flow == TrainController.Flow.Asymmetric)
		{
			float rand = Random.value;

			if (rand < 0.20f) 
			{
				trainLine = 1;  // Reduced flow
			} else 
			{
				trainLine = 2;  // Increased flow
			}
		}
		agent.trainLine = trainLine;

		// Find a waiting area goal for the agent. If there are no free waiting area spots their goal will be the ordinary goal for this spawner.
		CustomNode startNode = transform.GetChild(0).GetComponent<CustomNode>();
		(int waitingArea,int waitingSpot) waitingAreaSpot = waitingAreaController.GetWaitingAreaSpotNew(ref startNode, trainLine);
		if(waitingAreaSpot.waitingArea != -1)
		{
			agent.setWaitingAgent(true);
			agentGoal = waitingAreaSpot.waitingArea;
			agent.waitingSpot = waitingAreaSpot.waitingSpot;
			agent.waitingArea = map.allNodes[waitingAreaSpot.waitingArea].GetComponent<WaitingArea>();
		}

		return agentGoal;
	}


	float CalculateTimeBetweenSpawns()
    {
        float u = Random.value;
        // -ln(1-u)/λ
        return -Mathf.Log(1 - u) / spawnRate;
    }
}
