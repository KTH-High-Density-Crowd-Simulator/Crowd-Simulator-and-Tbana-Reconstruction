using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainController : MonoBehaviour
{
    public bool spawnTrains = true;
    public GameObject[] trains = new GameObject[3];
    private float arrivalTimer = 0f;
    public float arriveInterval = 10f;
    internal bool[] dwelling = new bool[3];
    public float dwellTime = 5f;
    private float[] dwellTimer = new float[3];
    private WaitingAreaController waitingAreaController;
    private Main mainScript;
    public int trainCapacity = 500;
    internal int[] nBoardedAgents = new int[3];
    public int nAgents = 100;
    public bool waitForMinimumAgents = false;
    public bool boardWithCapacity = false;
    internal bool[] isPreparingToBoard = new bool[3];
    internal bool[] boarding = new bool[3];
    public bool alightBeforeBoarding = true;
   
 
    void Start()
    {
        waitingAreaController = FindObjectOfType<WaitingAreaController>();
        if(waitingAreaController == null)
        {
            Debug.LogError("WaitingAreaController not found");
        }
        mainScript = FindObjectOfType<Main>();
        if(mainScript == null)
        {
            Debug.LogError("Main not found");
        }
        ToggleTrain(1);
        ToggleTrain(2);
    }

    void Update()
    {
        if(!spawnTrains)
        {
            return;
        }
        arrivalTimer += Time.deltaTime;
        if(arrivalTimer >= arriveInterval)
        {
            if(waitForMinimumAgents && mainScript.agentList.Count < nAgents)
            {
                return;
            }
            arrivalTimer = 0f;
            //UnityEditor.EditorApplication.isPaused = true;
            dwelling[1] = true;
            dwelling[2] = true;

            ToggleTrain(1);
            ToggleTrain(2);

            PrepareBoarding(1);
            PrepareBoarding(2);

            StartCoroutine(Alight(1));
            StartCoroutine(Alight(2));
        }
        Dwell(1);
        Dwell(2);
        
    }

    private void ToggleTrain(int trainLine)
    {
        trains[trainLine].transform.GetChild(0).gameObject.SetActive(dwelling[trainLine]);
        trains[trainLine].transform.GetChild(1).gameObject.SetActive(dwelling[trainLine]);
        trains[trainLine].transform.GetChild(2).gameObject.SetActive(dwelling[trainLine]);
    }

    private IEnumerator Alight(int trainLine)
    {
        yield return new WaitForSeconds(15f);
        Train trainScript = trains[trainLine].GetComponent<Train>();
        trainScript.Alight();
        if(!alightBeforeBoarding)
        {
            isPreparingToBoard[trainLine] = false;
            Board(trainLine);
        }
        else
        {
            bool allSpawnersDone = false;
            while (!allSpawnersDone)
            {
                allSpawnersDone = true;
                foreach (var spawner in trainScript.trainSpawners)
                {
                    if (!spawner.done)
                    {
                        allSpawnersDone = false;
                        break;
                    }
                }
                yield return null;
            }
            isPreparingToBoard[trainLine] = false;
            Board(trainLine);
        }
    }

    public void PrepareBoarding(int trainLine)
    {
        isPreparingToBoard[trainLine] = true;
        PrepareWaitingAgents(trainLine);
        PrepareWalkingAgents(trainLine);
        nBoardedAgents[trainLine] = 0;
    }


    private void Dwell(int trainLine)
    {
        if(dwelling[trainLine])
        {
            dwellTimer[trainLine] += Time.deltaTime;
            if (dwellTimer[trainLine] >= dwellTime)
            {
                dwelling[trainLine] = false;
                ToggleTrain(trainLine);
                dwellTimer[trainLine] = 0f;
                ResetLostAgents();
                boarding[trainLine] = false;
            }
        }
    }

    public void PrepareWaitingAgents(int trainLine)
    {
        for (int i = 0; i < waitingAreaController.waitingAgents.Count; i++)
        {
            Agent agent = waitingAreaController.waitingAgents[i];
            if (agent.trainLine == trainLine)
            {
                agent.GetComponentInChildren<Renderer>().material = waitingAreaController.boardingAgentMaterial;

                agent.isWaitingAgent = false;
                agent.waitingArea.isOccupied[agent.waitingSpot] = false;
                agent.waitingArea.freeWaitingSpots.Add(agent.waitingSpot);

                Rigidbody rb = agent.GetComponent<Rigidbody>();
                rb.constraints = RigidbodyConstraints.None;

                StartCoroutine(WaitOutsideTrain(agent));

                nBoardedAgents[trainLine]++;
                if(nBoardedAgents[trainLine] >= trainCapacity && boardWithCapacity)
                {
                    break;
                }
            }
        }
    }

    private void PrepareWalkingAgents(int trainLine)
    {
        for(int i = 0; i < mainScript.agentList.Count; i++)
		{
			Agent agent = mainScript.agentList[i];
			if(agent.trainLine == trainLine && !agent.boarding && !agent.isWaiting)
			{
                PrepareWalkingAgent(agent);
                nBoardedAgents[trainLine]++;
                if(nBoardedAgents[trainLine] >= trainCapacity && boardWithCapacity)
                {
                    break;
                }
			}
            
		}
    }

    internal void PrepareWalkingAgent(Agent agent)
    {
        agent.GetComponentInChildren<Renderer>().material = waitingAreaController.boardingAgentMaterial;

        int closestTrainDoor = waitingAreaController.FindClosestTrainDoor(ref agent);
        int closestNode = FindClosestNode(agent.transform.position);
        agent.setNewPath(closestNode, closestTrainDoor, ref mainScript.roadmap);

        if(agent.isWaitingAgent)
        {
            agent.waitingArea.isOccupied[agent.waitingSpot] = false;
            agent.waitingArea.freeWaitingSpots.Add(agent.waitingSpot);
            agent.isWaitingAgent = false;
        }
        StartCoroutine(WaitOutsideTrain(agent));
    }

    private IEnumerator WaitOutsideTrain(Agent agent)
    {
        // Wait for a bit so all start moving
        // at exactly the same time
        float delay = Random.Range(0.1f, 3f);
        yield return new WaitForSeconds(delay);

        if (!agent) yield break;

        // Wait outside the train close to the door
        Vector3 targetPoint = mainScript.roadmap.allNodes[agent.path[agent.pathIndex]].transform.position;
        Vector3 waitPosition;
        if(agent.transform.position.z < targetPoint.z)
        {
            waitPosition = new Vector3(targetPoint.x, 0, targetPoint.z - Random.Range(1.5f, 2.5f));
        }
        else
        {
            waitPosition = new Vector3(targetPoint.x, 0, targetPoint.z + Random.Range(1.5f, 2.5f));
        }
        if(agent.transform.position.x < targetPoint.x)
        {
            waitPosition.x = targetPoint.x + Random.Range(-5f, 0.4f);
        }
        else
        {
            waitPosition.x = targetPoint.x + Random.Range(-0.4f, 5f);
        }

        agent.noMapGoal = waitPosition;
        agent.noMap = true;

        // Reset these values to avoid unexpected movement
        agent.velocity = Vector3.zero;
        agent.preferredVelocity = Vector3.zero;
        agent.continuumVelocity = Vector3.zero;
        agent.collisionAvoidanceVelocity = Vector3.zero;

        // Start moving towards the train door
        agent.walkingSpeed = Random.Range(0.5f, 1f);
        agent.done = false;
        agent.isWaiting = false;
        agent.isPreparingToBoard = true;
    }

    public void Board(int trainLine)
    {
        boarding[trainLine] = true;
        for (int i = mainScript.agentList.Count - 1; i >= 0; i--)
        {
            Agent agent = mainScript.agentList[i];
            if (agent.trainLine == trainLine)
            {
                if(!agent.isPreparingToBoard)
                {
                    continue;
                }
                StartCoroutine(BoardAgent(agent));
            }
        }
    }

    internal IEnumerator BoardAgent(Agent agent)
    {
        float delay = Random.Range(1.5f, 3f);
        yield return new WaitForSeconds(delay);

        if (!agent) yield break;

        waitingAreaController.waitingAgents.Remove(agent);

        agent.velocity = Vector3.zero;
        agent.preferredVelocity = Vector3.zero;
        agent.continuumVelocity = Vector3.zero;
        agent.collisionAvoidanceVelocity = Vector3.zero;

        if (waitingAreaController.agentContainer != null)
        {
            agent.transform.SetParent(waitingAreaController.agentContainer.transform);
        }
        else
        {
            agent.transform.SetParent(null);
        }

        
        agent.noMap = false;
        agent.done = false;
        agent.isWaiting = false;
        agent.isPreparingToBoard = false;
        agent.boarding = true;
    }

    int FindClosestNode(Vector3 position)
    {
        int closestNode = -1;
        float closestDistance = Mathf.Infinity;

        int layersToIgnore = LayerMask.GetMask("WaitingAgent", "Agent");
        int layerMask = ~layersToIgnore; // ignore these layers

        for (int j = 0; j < mainScript.roadmap.allNodes.Count; ++j) {
            Vector3 nodePos = mainScript.roadmap.allNodes[j].transform.position;
            float distance = (nodePos - position).magnitude;

            if (!Physics.Raycast(position, (nodePos - position).normalized, distance, layerMask)) {
                if (nodePos != transform.position && distance < closestDistance) {
                    closestDistance = distance;
                    closestNode = j;
                }
            }
        }
        return closestNode;
    }

    private void ResetLostAgents()
    {
        for (int i = mainScript.agentList.Count - 1; i >= 0; i--)
        {
            Agent agent = mainScript.agentList[i];
            if(agent.isPreparingToBoard || agent.boarding)
            {
                /**
                agent.isPreparingToBoard = false;
                agent.boarding = false;
                agent.pathIndex = 1;
                agent.Reset();
                agent.transform.position = new Vector3(Mathf.Clamp(agent.transform.position.x, -7.5f, 7.5f), 0f, agent.transform.position.z);
                agent.isWaiting = true;
                waitingAreaController.waitingAgents.Add(agent);
                */
                mainScript.agentList.RemoveAt(i);
                Destroy(agent.gameObject);
            }
            if(agent.isAlighting)
            {
                agent.Reset();
            }
        }
    }
}
