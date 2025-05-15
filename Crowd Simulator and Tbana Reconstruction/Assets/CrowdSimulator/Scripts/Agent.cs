using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;


public class Agent : MonoBehaviour {
	public Vector3 preferredVelocity, continuumVelocity, collisionAvoidanceVelocity;
	public Vector3 velocity;
	public List<int> path;
	internal int pathIndex = 0;
	internal float agentRelXPos, agentRelZPos;
	internal float neighbourXWeight, neighbourZWeight, neighbourXZWeight, selfWeight;
	internal float selfRightVelocityWeight, selfLeftVelocityWeight, selfUpperVelocityWeight, selfLowerVelocityWeight, 
	neighbourRightVelocityWeight, neighbourLeftVelocityWeight, neighbourUpperVelocityWeight, neighbourLowerVelocityWeight;
	internal float densityAtAgentPosition;

	internal Vector3 targetPoint;
	internal bool done = false;
	internal bool noMap = false;
	internal Vector3 noMapGoal;
	internal int goal;
	internal Animator animator;
	internal Rigidbody rbody;
	internal bool collision = false;
	internal int row,column;
	Vector3 prevPos;
	Vector3 previousDirection;
	public float walkingSpeed;
    public float maxWaitTime = 2f;
	private bool isProblem = false;

	// Waiting
	internal bool isWaitingAgent;
	internal WaitingArea waitingArea;
	internal int waitingSpot;
	// Subway
	internal int trainLine;
	public bool isWaiting = false;
	public bool isPreparingToBoard = false;
	public bool boarding = false;
	public bool isAlighting = false;
	private bool crossingYellowLine = false;
	private TrainController trainController;

	internal void Start() {
		animator = transform.gameObject.GetComponent<Animator> ();
		rbody = transform.gameObject.GetComponent<Rigidbody> ();
		trainController = FindObjectOfType<TrainController>();

		if (rbody != null)
		{
			rbody.isKinematic = false;
			rbody.useGravity = false;
		}
		else
		{
			Debug.LogError("No Rigidbody found!");
		}

		Collider col = GetComponent<Collider>();
		if (col == null)
		{
			Debug.LogError("No Collider found!");
		}

		//Which cell am i in currently?
		calculateRowAndColumn();
		if (!Grid.instance.colHandler && rbody != null) {
			Destroy (rbody);
		}

		Main mainScript = FindObjectOfType<Main>();
		if(this is SubgroupAgent)
		{
			walkingSpeed = mainScript.agentMaxSpeed;
		}
		else
		{
			walkingSpeed = Random.Range(mainScript.agentMinSpeed, mainScript.agentMaxSpeed);
		}
		
	}

/**
	private void OnDrawGizmos()
	{
		UnityEditor.Handles.color = Color.red;
		if(isProblem)
		{
			UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, "Problem!!!");
			Debug.DrawLine(transform.position, transform.position + Vector3.up * 5f, Color.red, 10f);
		}

		if(!noMap && pathIndex < path.Count)
		{
			UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, path[pathIndex].ToString());
		}else if(pathIndex < path.Count)
		{
			//UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, "noMap");
		}

		if(transform.position.y > 0.1f || 
			transform.position.y < -0.1f || 
			transform.rotation.x < -0.1 || 
			transform.rotation.x > 0.1 ||
			transform.rotation.z > 0.1 ||
			transform.rotation.z < -0.1)
		{
			Debug.Log("Problem: " + transform.position.y + " " + transform.rotation.x + " " + transform.rotation.z);
			Debug.DrawLine(transform.position, transform.position + Vector3.up * 5f, Color.red, 10f);
		}
		
	}

*/
	


    public void setWaitingAgent(bool isWaitingAgent)
	{
		this.isWaitingAgent = isWaitingAgent;
	}

	public void setNewPath(int start, int goal, ref MapGen.map map) {
		calculateRowAndColumn();
		this.goal = goal;

		path = map.shortestPaths[start][goal];

		pathIndex = 1;

		if(path.Count <= 1)
		{
			pathIndex = 0;
		}

		targetPoint = map.allNodes[path[pathIndex]].getTargetPoint(transform.position);
		
		//targetPoint = map.allNodes[path[pathIndex]].getTargetPoint(transform.position);
		preferredVelocity = (targetPoint - transform.position).normalized;
	}

	public void InitializeAgent(Vector3 pos, int start, int goal, ref MapGen.map map) {
		transform.position = pos;
		transform.right = transform.right;
		this.goal = goal;
		path = map.shortestPaths[start][goal];

		pathIndex = 1;
		targetPoint = map.allNodes[path[pathIndex]].getTargetPoint(transform.position);
		preferredVelocity = (targetPoint - transform.position).normalized;
		//transform.localScale = new Vector3(1.0f, 1.0f, 1.0f); // Modify this to change the size of characters new Vector3(2.0f, 2.0f, 2.0f) is normal size
	}

	public void ApplyMaterials(Material materialColor, ref Dictionary<string, int> skins, Material argMat = null)
	{
		if (tag == "original") {
			if (transform.childCount > 1) {
				//transform.GetChild(1).GetComponent<SkinnedMeshRenderer> ().sharedMaterial = materialColor;
			}
		} else if (transform.childCount > 0) {
			Renderer ss = transform.GetChild (0).GetComponent<Renderer> ();
			if (ss != null)
				ss.material.mainTexture = (Texture)Resources.Load (tag + "-" + Random.Range (1, skins [tag]+1));
			else {
				Renderer ss2 = transform.GetChild (1).GetComponent<Renderer> ();
				if (ss2 != null)
					ss2.material.mainTexture = (Texture)Resources.Load (tag + "-" + Random.Range (1, skins [tag]+1));
			}
		}
	}

	internal void calculateRowAndColumn() {
		row = (int)((transform.position.z - Main.zMinMax.x)/Grid.instance.cellLength); 
		column = (int)((transform.position.x - Main.xMinMax.x)/Grid.instance.cellLength); 
		if (row < 0)
			row = 0; 
		if (column < 0)
			column = 0;
		if (row > Grid.instance.cellsPerRow - 1) {
			row = Grid.instance.cellsPerRow - 1;
		}
		if (column > Grid.instance.cellsPerRow - 1) {
			column = Grid.instance.cellsPerRow - 1;
		}
		agentRelXPos = transform.position.x - Grid.instance.cellMatrix [row, column].transform.position.x;
		agentRelZPos = transform.position.z - Grid.instance.cellMatrix [row, column].transform.position.z;
	}

	/**
	 * Calculate the actual velocity of this agent, based on continuum, preferred and collision avoidance velocities
	 **/ 
	internal void setCorrectedVelocity() {
		calculateDensityAtPosition ();
		calculateContinuumVelocity ();
		//-1 since we subtract this agents density at position

		velocity = preferredVelocity + (densityAtAgentPosition - 1 / Mathf.Pow (Grid.instance.cellLength, 2)) / Grid.maxDensity
		* (continuumVelocity - preferredVelocity);
		velocity.y = 0f;
		if(velocity != Vector3.zero)
		{
			transform.forward = velocity.normalized;
		}
		velocity = velocity + collisionAvoidanceVelocity;
	}

	internal bool canSeeNext(ref MapGen.map map, int modifier) {
		if (pathIndex + modifier < path.Count && pathIndex + modifier >= 0 && pathIndex + modifier < map.allNodes.Count) {
			//Can we see next goal?
			Vector3 next = map.allNodes[path[pathIndex+modifier]].getTargetPoint(transform.position);
			int layersToIgnore = LayerMask.GetMask("WaitingAgent", "Agent");
			int layerMask = ~layersToIgnore;
			Vector3 targetPosition = transform.position - transform.forward;
			Vector3 dir = next - targetPosition;
			if (!Physics.Raycast(targetPosition, dir.normalized, dir.magnitude, layerMask)) {
				return true;
			}
		}
		return false;
	}
	/**
	 * Calculate the preferred velocity by looking at desired path
	 **/ 
	bool change = false;
	internal void calculatePreferredVelocityMap(ref MapGen.map map) {
		previousDirection = preferredVelocity.normalized;

		if (map.allNodes[path[pathIndex]].IsAgentInsideArea(transform.position) || (Grid.instance.skipNodeIfSeeNext && canSeeNext(ref map, 1))) 
		{
			//New node reached
			collision = false;
			pathIndex += 1;
			if (pathIndex >= path.Count) 
			{
				//Done
				done = true;
			} else 
			{
				targetPoint = map.allNodes[path[pathIndex]].getTargetPoint(transform.position);
				Vector3 nextDirection = (targetPoint - transform.position).normalized;
				if (Vector3.Angle (previousDirection, nextDirection) > 20.0f && Grid.instance.smoothTurns) {
					preferredVelocity = Vector3.RotateTowards (velocity.normalized, nextDirection, Grid.instance.dt*((35.0f - 400*Grid.instance.dt) * Mathf.PI / 180.0f), 15.0f).normalized;
					change = true;
				}
			}
		} else if(pathIndex > 0 && Grid.instance.walkBack && !canSeeNext(ref map, 0)) { //Can we see current heading? Are we trapped?
			//No. We want to go back
			preferredVelocity = (map.allNodes[path[pathIndex-1]].getTargetPoint(transform.position) - transform.position).normalized;
			change = false;
		} else {
			collision = false;
			Vector3 nextDirection = (targetPoint - transform.position).normalized;
			if (change && Vector3.Angle (previousDirection, nextDirection) > 20.0f && Grid.instance.smoothTurns) {
				preferredVelocity = Vector3.RotateTowards(velocity.normalized, nextDirection, Grid.instance.dt*((35.0f - 400*Grid.instance.dt) * Mathf.PI / 180.0f),  15.0f).normalized;
			} else {
				change = false;
				preferredVelocity = (targetPoint - transform.position).normalized;
			}
		}
		//collision = false;
		preferredVelocity = preferredVelocity * walkingSpeed;
		preferredVelocity.y = 0f;
	}

	/**
	 * Calculate the preferred velocity of a single uncharted point as a goal 
	 **/
	internal void calculatePreferredVelocityNoMap() {
		if ((transform.position - noMapGoal).magnitude < MapGen.DEFAULT_THRESHOLD) {
			//New node reached
			//Done
			done = true;
		} else {
			preferredVelocity = (noMapGoal - transform.position).normalized;
		}
		preferredVelocity = preferredVelocity * walkingSpeed;
		preferredVelocity.y = 0f;
	}

    private void Update()
    {
        
			
    }

    internal virtual void calculatePreferredVelocity(ref MapGen.map map) {
		if (noMap) {
			calculatePreferredVelocityNoMap ();
		} else {
			calculatePreferredVelocityMap (ref map);
		}
	}
	/**
	 * Change the position of the agent and reset variables. 
	 * Do animations.
	 **/
	internal void changePosition(ref MapGen.map map) {
		if (done) {
			return; // Don't do anything
		} 

		calculatePreferredVelocity(ref map);
		if((!trainController.dwelling[1] && !trainController.dwelling[2]) || isAlighting)
		{
			ApplyYellowLineForce();
		}
		setCorrectedVelocity ();
	
		prevPos = transform.position;

		Vector3 newPosition = transform.position + velocity * Grid.instance.dt;
		newPosition.y = 0.0f;	// Lock Y position
		transform.position = newPosition;

		CheckYellowLine();

		if(rbody != null) { rbody.velocity = Vector3.zero; }
		collisionAvoidanceVelocity = Vector3.zero;

		Animate(prevPos);
	}

	internal void PassiveMove()
	{
		if(!trainController.dwelling[1] && !trainController.dwelling[2])
		{
			ApplyYellowLineForce();
		}
		Vector3 force = collisionAvoidanceVelocity;
		force.y = 0f;

		if (force.magnitude > 0.01f)
		{
			Vector3 newPosition = transform.position + force * Grid.instance.dt;
			newPosition.y = 0f;
			transform.position = newPosition;
			transform.forward = force.normalized;

			CheckYellowLine();

			collisionAvoidanceVelocity = Vector3.zero;
		}
	}

	private void CheckYellowLine()
	{
		if(trainController.dwelling[1] || trainController.dwelling[2]){ return; }

		float positionX = Mathf.Abs(transform.position.x);

		switch (trainController.platformType)
		{
			case TrainController.PlatformType.Central:
				
				break;

			case TrainController.PlatformType.Mixed:
				if (((positionX < 7f && positionX > 4f) ||
					 (positionX > 2f && positionX < 5f)) 
					 && !crossingYellowLine)
				{
					Debug.Log($"Agent crossed the yellow line");
					Debug.DrawLine(transform.position, transform.position + Vector3.up * 10f, Color.red, 10f);
					crossingYellowLine = true;
				}
				if(crossingYellowLine && 
				(positionX > 7f || positionX < 2f))
				{
					crossingYellowLine = false;
				}
				break;

			case TrainController.PlatformType.Side:
				
				break;
		}
	}

	void Animate(Vector3 previousPosition)
	{
		float realSpeed = Vector3.Distance (transform.position, previousPosition) / Mathf.Max(Grid.instance.dt, Time.deltaTime);
		if (animator != null) {
	
			if (realSpeed < 0.05f) {
				animator.speed = 0;
			} else if(realSpeed > walkingSpeed)
			{
				animator.speed = 1;
			} else {
				animator.speed = realSpeed / walkingSpeed;
			}
		}
	}

	/**
	 * Do a bilinear interpolation of surrounding densities and come up with a density at this agents position.
	 **/
	internal float calculateDensityAtPosition() {
		densityAtAgentPosition = 0.0f;
		int xNeighbour = (int)(column + neighbourXWeight/Mathf.Abs(neighbourXWeight));	//Column for the neighbour which the agent contributes to
		int zNeighbour = (int)(row + neighbourZWeight/Mathf.Abs(neighbourZWeight));		//Row for the neighbour which the agent contributes to

		densityAtAgentPosition += Mathf.Abs(selfWeight)*Grid.instance.density[row, column];

		if (!((xNeighbour) < 0) & !((xNeighbour) > Grid.instance.cellsPerRow - 1)){	//As long as the cell exists
			densityAtAgentPosition += Mathf.Abs(neighbourXWeight)*Grid.instance.density[row, xNeighbour];
		}

		if (!((zNeighbour) < 0) & !((zNeighbour) > Grid.instance.cellsPerRow - 1)){			//As long as the cell exists
			densityAtAgentPosition += Mathf.Abs(neighbourZWeight)*Grid.instance.density[zNeighbour, column];
		}

		if (!((zNeighbour) < 0) & !((zNeighbour) > Grid.instance.cellsPerRow - 1) & !((xNeighbour) < 0) & !((xNeighbour) > Grid.instance.cellsPerRow - 1)){	//As long as the cell exists
			densityAtAgentPosition += Mathf.Abs(neighbourXZWeight)*Grid.instance.density[zNeighbour, xNeighbour];
		}
		return densityAtAgentPosition;
	}

	/**
	 * Calculate the continuum velocity caused by pressure from the grid
	 **/
	internal void calculateContinuumVelocity() {
		Vector3 tempContinuumVelocity = new Vector3(0,0,0);

		int xNeighbour = (int)(column + neighbourXWeight/Mathf.Abs(neighbourXWeight));	//Column for the neighbour which the agent contributes to
		int zNeighbour = (int)(row + neighbourZWeight/Mathf.Abs(neighbourZWeight));		//Row for the neighbour which the agent contributes to

		// Sides in current cell
		tempContinuumVelocity.x += selfLeftVelocityWeight*Grid.instance.cellMatrix[row, column].leftVelocityNode.velocity;

		tempContinuumVelocity.x += selfRightVelocityWeight*Grid.instance.cellMatrix[row, column].rightVelocityNode.velocity;

		tempContinuumVelocity.z += selfUpperVelocityWeight*Grid.instance.cellMatrix[row, column].upperVelocityNode.velocity;

		tempContinuumVelocity.z += selfLowerVelocityWeight*Grid.instance.cellMatrix[row, column].lowerVelocityNode.velocity;

		if (!((zNeighbour) < 0) & !((zNeighbour) > Grid.instance.cellsPerRow - 1)){	//As long as the cell exists
			tempContinuumVelocity.x += neighbourLeftVelocityWeight*Grid.instance.cellMatrix[zNeighbour, column].leftVelocityNode.velocity;
			tempContinuumVelocity.x += neighbourRightVelocityWeight*Grid.instance.cellMatrix[zNeighbour, column].rightVelocityNode.velocity;
		}

		if (!((xNeighbour) < 0) & !((xNeighbour) > Grid.instance.cellsPerRow - 1)){			//As long as the cell exists
			tempContinuumVelocity.z += neighbourUpperVelocityWeight*Grid.instance.cellMatrix[row, xNeighbour].upperVelocityNode.velocity;
			tempContinuumVelocity.z += neighbourLowerVelocityWeight*Grid.instance.cellMatrix[row, xNeighbour].lowerVelocityNode.velocity;
		}

		if (float.IsNaN(tempContinuumVelocity.x)){
			tempContinuumVelocity.Set (0, tempContinuumVelocity.y, tempContinuumVelocity.z);
		}

		if(float.IsNaN(continuumVelocity.z)){
			tempContinuumVelocity.Set (tempContinuumVelocity.x, tempContinuumVelocity.y, 0);
		}
		continuumVelocity = tempContinuumVelocity;
	}

	/**
	 * Move command (and all it includes) for this agent.
	 * Recalculate weights and contributions to grid after update.
	 **/
	internal void move(ref MapGen.map map) {
		changePosition (ref map);
		calculateRowAndColumn ();
		setWeights ();
		Grid.instance.cellMatrix[row, column].addVelocity(this);
		Grid.instance.cellMatrix[row, column].addDensity (this);
	}


	/**
	 * Set weight contributions to current cell radius. (Inverse bilinear interpolation)
	 **/
	public void setWeights(){
		float cellLength = Grid.instance.cellLength;
		float clSquared = Mathf.Pow (cellLength, 2);

		//An area the size of a cell is surrounded by each point.
		//AgentRelXPos: Side length of supposed area, outside current cell of agent - x direction
		//AgentRelZPos: Side length of supposed area, outside current cell of agent - z direction
		float sideOne = cellLength - Mathf.Abs(agentRelXPos); //Side length of supposed area of this agents position, x - direction
		float sideTwo = cellLength - Mathf.Abs(agentRelZPos); //Side length of supposed area of this agents position, z - direction

		// Weights on smaller areas inside and outside current cell
		//Area weight of neighboring cell in..
		neighbourXWeight = sideTwo*agentRelXPos/clSquared; // x direction
		neighbourZWeight = sideOne*agentRelZPos/clSquared; //z direction
		neighbourXZWeight = agentRelXPos*agentRelZPos/clSquared; //both x and z direction (diagonal from this agent's cell)

		//Own cell weight
		selfWeight = sideOne*sideTwo/clSquared; 


		//Now checking velocityNodes contribution
		//Offsets from each velocity node's center (also seen as a cell on each node)
		float rightShiftedRelXPos = cellLength / 2 + agentRelXPos;
		float leftShiftedRelXPos  = cellLength / 2 - agentRelXPos;
		float upperShiftedRelZPos = cellLength / 2 + agentRelZPos;
		float lowerShiftedRelZPos = cellLength / 2 - agentRelZPos;

		//Weight contributions to different velocityNodes (area / totalCellArea)
		selfRightVelocityWeight = rightShiftedRelXPos * sideTwo / clSquared;
		selfLeftVelocityWeight  = leftShiftedRelXPos  * sideTwo / clSquared;
		selfUpperVelocityWeight = upperShiftedRelZPos * sideOne / clSquared;
		selfLowerVelocityWeight = lowerShiftedRelZPos * sideOne / clSquared;

		neighbourRightVelocityWeight = rightShiftedRelXPos * Mathf.Abs(agentRelZPos) / clSquared;
		neighbourLeftVelocityWeight  = leftShiftedRelXPos  * Mathf.Abs(agentRelZPos) / clSquared;
		neighbourUpperVelocityWeight = upperShiftedRelZPos * Mathf.Abs(agentRelXPos) / clSquared;
		neighbourLowerVelocityWeight = lowerShiftedRelZPos * Mathf.Abs(agentRelXPos) / clSquared;
	}

	public void teleportAgent(Vector3 newPosition)
	{
		newPosition.y = 0.0f;
		transform.position = newPosition;
	}

	public void setAnimatorStanding(bool isStanding)
	{
		if(!(animator == null))
		{
			animator.SetBool("Standing",isStanding);
		}
	}

	public void rotateAgent(Vector3 target)
	{
		Vector3 direction = target - transform.position;
		transform.rotation = Quaternion.LookRotation(direction);
	}

	internal void Reset()
	{
		Rigidbody rb = GetComponent<Rigidbody>();
		rb.velocity = Vector3.zero;
		rb.angularVelocity = Vector3.zero;
		velocity = Vector3.zero;
        preferredVelocity = Vector3.zero;
        continuumVelocity = Vector3.zero;
        collisionAvoidanceVelocity = Vector3.zero;
		transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
		transform.rotation = Quaternion.identity;
	}

	private void ApplyYellowLineForce()
	{
		switch (trainController.platformType)
		{
			case TrainController.PlatformType.Central:
				
				break;
			case TrainController.PlatformType.Mixed:
				ApplyYellowLineForceMixed();
				break;
			case TrainController.PlatformType.Side:
				
				break;
		}
	}

	private void ApplyYellowLineForceMixed()
	{
		float agentX = transform.position.x;

		// Side Platforms
		{
			float platformEdge = 6f;
			float yellowLineStart = 7.24f;
			float zoneWidth = yellowLineStart - platformEdge;

			// approaching from -12)
			if (agentX > -yellowLineStart && agentX < -platformEdge)
			{
				float distToEdge = -platformEdge - agentX; // how far into the yellow zone
				float strength = Mathf.Clamp01(distToEdge / zoneWidth);
				Vector3 repel = Vector3.left * strength * walkingSpeed;
				collisionAvoidanceVelocity += repel;
				// Debug.DrawRay(transform.position, repel, Color.yellow);
			}

			// approaching from +12)
			else if (agentX < yellowLineStart && agentX > platformEdge)
			{
				float distToEdge = agentX - platformEdge;
				float strength = Mathf.Clamp01(distToEdge / zoneWidth);
				Vector3 repel = Vector3.right * strength * walkingSpeed;
				collisionAvoidanceVelocity += repel;
				// Debug.DrawRay(transform.position, repel, Color.yellow);
			}
		}

		// Central Platform
		{
			float platformEdge = 3f;
			float yellowLineStart = 1.76f;
			float zoneWidth = platformEdge - yellowLineStart;

			if (agentX > -platformEdge && agentX < -yellowLineStart)
			{
				float distToEdge = agentX + platformEdge; // how far into the yellow zone from the edge
				float strength = Mathf.Clamp01(distToEdge / zoneWidth);
				Vector3 repel = Vector3.right * strength * walkingSpeed;
				collisionAvoidanceVelocity += repel;
				// Debug.DrawRay(transform.position, repel, Color.green);
			}

			else if (agentX < platformEdge && agentX > yellowLineStart)
			{
				float distToEdge = platformEdge - agentX;
				float strength = Mathf.Clamp01(distToEdge / zoneWidth);
				Vector3 repel = Vector3.left * strength * walkingSpeed;
				collisionAvoidanceVelocity += repel;
				// Debug.DrawRay(transform.position, repel, Color.green);
			}
		}
	}

	private void ApplyYellowLineForceCentral()
	{

	}

	private void ApplyYellowLineForceSide()
	{

	}
}
