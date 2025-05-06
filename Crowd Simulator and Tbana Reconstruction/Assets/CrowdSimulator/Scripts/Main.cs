using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Data.Common;

public class Main : MonoBehaviour {

	public enum LCPSolutioner {
		mprgp,
		mprgpmic0,
		psor
	}
	public float epsilon;
	public int solverMaxIterations;
	public LCPSolutioner solver;
	

	public float planeSizeX;
	public float planeSizeZ;
	

	
	public float agentAvoidanceRadius;
	public float agentMaxSpeed;
	public float agentMinSpeed;
	public bool usePresetGroupDistances;
	public float p1p2, p2p3, p3p4;



	public Grid gridPrefab;
	public NewSpawner spawnerPrefab;
	public MapGen mapGen;
	public Plane plane;
	internal static Vector2 xMinMax;
	internal static Vector2 zMinMax;
	internal MapGen.map roadmap;

	public int cellsPerRow;
	public int neighbourBins;
	public int roadNodeAmount; // Number of nodes that are placed automatically
	public bool visibleMap; // Show or hide the nodes in the world
	internal float ringDiameter;

	public bool customTimeStep;
	public float timeStep; 

	[Range(0.01f, 1f)]
	public float alpha; 

	internal List<Agent> agentList = new List<Agent>();
	public int maxNumberOfAgents = 1000; // Maximum number of agents when spawning continuously

	public bool showSplattedDensity = false;
	public bool showSplattedVelocity = false;
	public bool walkBack = false;
	public bool skipNodeIfSeeNext = false;
	public bool smoothTurns = false;
	public bool handleCollision = false;
	internal WaitingAreaController waitingAreaController;

	/**
	 * Initialize simulation by taking the user's options into consideration and spawn agents.
	 * Then create the Staggered Grid along with all cells and velocity nodes.
	**/
	void OnEnable () {
		bool error = false; 
		if (error)
			return;
		
		plane.transform.localScale = new Vector3 (planeSizeX, 1.0f, planeSizeZ);
		Vector3 planeLength = plane.getLengths (); //Staggered grid length
		xMinMax = new Vector2 (plane.transform.position.x - planeLength.x / 2, 
			                   plane.transform.position.x + planeLength.x / 2);
		zMinMax = new Vector2 (plane.transform.position.z - planeLength.z / 2, 
							  plane.transform.position.z + planeLength.z / 2);

		ringDiameter = agentAvoidanceRadius * 2; //Prefered distance between two agents

		//Creates roadmap / pathfinding for agents based on map
		MapGen m = Instantiate (mapGen) as MapGen; 
		roadmap = m.generateRoadMap (roadNodeAmount, xMinMax, zMinMax, visibleMap);

		waitingAreaController = FindObjectOfType<WaitingAreaController>();
		if(waitingAreaController != null)
		{
			waitingAreaController.Initialize();
		}


		Grid grid = Instantiate (gridPrefab) as Grid;
		grid.showSplattedDensity = showSplattedDensity;
		grid.showSplattedVelocity = showSplattedVelocity;
		grid.cellsPerRow = cellsPerRow;
		grid.agentMaxSpeed = agentMaxSpeed;
		grid.ringDiameter = ringDiameter;
		grid.usePresetGroupDistances = usePresetGroupDistances;
		grid.groupDistances = new float[] {p1p2, p2p3, p3p4};
		grid.mapGen = mapGen;
		grid.dt = timeStep; 
		grid.neighbourBins = neighbourBins;
		grid.solver = solver;
		grid.solverEpsilon = epsilon;
		grid.solverMaxIterations = solverMaxIterations;
		grid.colHandler = handleCollision;
		grid.agentAvoidanceRadius = agentAvoidanceRadius;
		Grid.instance = grid;
		Grid.instance.initGrid (xMinMax, zMinMax, alpha, agentAvoidanceRadius);

		for (int i = 0; i < roadmap.spawns.Count; ++i)
		{
			//roadmap.spawns[i].spawner.InitializeSpawner (ref agentPrefabs, ref groupAgentPrefabs, ref shirtColorPrefab, ref roadmap, 
			//								 ref agentList, xMinMax, zMinMax, agentAvoidanceRadius);
			roadmap.spawns[i].spawner.InitializeSpawner(ref roadmap, 
											 ref agentList, xMinMax, zMinMax, agentAvoidanceRadius);
		}
	}


    /**
	 * Main simulation loop which is called every frame
	**/
    void Update () {
		Grid.instance.solver = solver;
		Grid.instance.solverEpsilon = epsilon;
		Grid.instance.solverMaxIterations = solverMaxIterations;

		// Update grid with new density and velocity values
		Grid.instance.updateCellDensity ();
		Grid.instance.updateVelocityNodes ();
		//Solve linear constraint problem
		Grid.instance.PsolveRenormPsolve ();
		//Move agents
		for (int i = agentList.Count - 1; i >= 0; i--)
		{
			Agent agent = agentList[i];
			if(agent.isWaiting)
			{
				continue;
			}

			// remove agent if it is outside the bounds of the plane
			if(Mathf.Abs(agent.transform.position.x) > planeSizeX * 5f || Mathf.Abs(agent.transform.position.z) > planeSizeZ * 5f || agent.transform.position.y > 0.5f)
			{
				if(agent.isWaitingAgent)
				{
					agent.waitingArea.isOccupied[agent.waitingSpot] = false;
                	agent.waitingArea.freeWaitingSpots.Add(agent.waitingSpot);
					agent.isWaitingAgent = false;
				}
				Debug.Log("Agent outside of bounds, removing");
				agentList.RemoveAt(i);
				Destroy(agent.gameObject);
			}

			if (agent.done)
			{
				if(agent.isWaitingAgent)
				{
					// Agent reached the waiting area
					if(!agent.noMap)
					{
						waitingAreaController.walkAgentToWaitingSpot(agent);
						agent.move(ref roadmap);
					}
					// Agent reached the waiting spot
					else
					{
						waitingAreaController.putAgentInWaitingArea(agent);
						//agentList.RemoveAt(i);
					}
				}
				else
				{
					agentList.RemoveAt(i);
					Destroy(agent.gameObject);
				}
				continue;
			}
			agent.move(ref roadmap);
			
		}
		//Pair-wise collision handling between agents
		Grid.instance.collisionHandling(ref agentList);

		//flags
		Grid.instance.showSplattedDensity = showSplattedDensity;
		Grid.instance.showSplattedVelocity = showSplattedVelocity;
		Grid.instance.walkBack = walkBack;
		Grid.instance.skipNodeIfSeeNext = skipNodeIfSeeNext;
		Grid.instance.smoothTurns = smoothTurns;

		Grid.instance.dt = customTimeStep ? timeStep : Time.deltaTime;

	}
	public void AddToAgentList(Agent agent)
	{
		agentList.Add(agent);
	}

}
