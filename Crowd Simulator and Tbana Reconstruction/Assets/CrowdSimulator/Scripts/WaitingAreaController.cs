using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Data.Common;

/*
*   This class manages all the waiting areas and related functions.
*/
public class WaitingAreaController : MonoBehaviour
{  
    public List<WaitingArea> waitingAreas;                          // All the waiting areas in the scene
    internal List<Agent> waitingAgents;                              // Agents that are currently waiting
    private GameObject waitingAgentsContainer;                      // A container for the waiting agent objects in the inspector
    public Dictionary<int, List<int>> spawnerWaitingAreaDistances;  // The distance from each spawner to each waiting area in descending order
    private MapGen.map roadmap;    
    public GameObject agentContainer;                                 // The map of the nodes in the scene
    public float wdistance = 1.0f, wdensity = 1.0f, wtrainline = 1.0f, wpriority = 1.0f;
    public bool debug = false;
    public float waitingSpotSize = 0.5f;
    public bool useRowColumns = false;
    private TrainController trainController;
    public Material waitingAgentMaterial;
    public Material walkingAgentMaterial;
    public Material boardingAgentMaterial;
    private Main mainScript;

    public void Initialize()
    {
        waitingAreas = new List<WaitingArea>();
        waitingAgents = new List<Agent>();
        waitingAgentsContainer = GameObject.Find("Waiting Agents");

        foreach(WaitingArea waitingArea in FindObjectsOfType<WaitingArea>())
        {
            waitingAreas.Add(waitingArea);
            waitingArea.Initialize(debug, waitingSpotSize, useRowColumns);
        }

        roadmap = FindObjectOfType<MapGen>().getRoadmap();

        BuildSpawnerWaitingAreaDistances();

        trainController = FindObjectOfType<TrainController>();
        if(trainController == null)
        {
            Debug.LogError("TrainController not found in the scene.");
        }
        mainScript = FindObjectOfType<Main>();
    }

    public void addAgentToWaitingList(Agent agent)
    {
        waitingAgents.Add(agent);
    }

    /*
    *   Get a waiting spot in the closest waiting area that has free spots.
    *   Returns the index of the waiting area in the roadmap and the position of the waiting spot.
    */
    public (int,int) GetWaitingAreaSpot(int startNode)
    {

        foreach (int waitingAreaIndex in spawnerWaitingAreaDistances[startNode])
        {
            (int waitingAreaMapIndex, int waitingAreaSpot) areaAndSpot = waitingAreas[waitingAreaIndex].getWaitingSpot();
            if(areaAndSpot.waitingAreaSpot != -1)
            {
                return (areaAndSpot.waitingAreaMapIndex, areaAndSpot.waitingAreaSpot);
            }
        }
        return (-1,-1);
    }

    public (int,int) GetWaitingAreaSpotNew(ref CustomNode startNode, int trainLine, bool forceTrainLine = false)
    {
        float bestScore = Mathf.Infinity;
        WaitingArea bestWaitingArea = null;
        foreach (WaitingArea waitingArea in waitingAreas)
        {   
            if(!waitingArea.HasFreeWaitingSpots())
            {
                continue;
            }
            float score;
            float distance = Vector3.Distance(startNode.transform.position, waitingArea.transform.position);
            distance /= 150f; // Normalize the distance to a value between 0 and 1
            float density = waitingArea.GetDensity();
            int closestTrainLine;

            if(waitingArea.transform.position.x >= 0)
            {
                closestTrainLine = 1;
            }
            else
            {
                closestTrainLine = 2;
            }

            if(forceTrainLine && trainLine != closestTrainLine)
            {
                continue;
            }

            float lineMismatch;

            if(trainLine == closestTrainLine)
            {
                lineMismatch = 0f;
            }
            else
            {
                lineMismatch = 1f;
            }

            float priority = waitingArea.priority * 0.1f;

            score = wdistance * distance + wdensity * density + wtrainline * lineMismatch + wpriority * priority;

            if(score < bestScore)
            {
                bestScore = score;
                bestWaitingArea = waitingArea;
            }
        }
        if(bestWaitingArea == null)
        {
            return (-1,-1);
        }

        (int waitingAreaMapIndex, int waitingAreaSpot) areaAndSpot = bestWaitingArea.getWaitingSpot();
        if(areaAndSpot.waitingAreaSpot != -1)
        {
            return (areaAndSpot.waitingAreaMapIndex, areaAndSpot.waitingAreaSpot);
        }

        return (-1,-1);
    }

    /*
    *   Order the waiting areas by distance from each spawner.
    */
    private void BuildSpawnerWaitingAreaDistances()
    {
        spawnerWaitingAreaDistances = new Dictionary<int, List<int>>();

        foreach(MapGen.spawnNode spawner in roadmap.spawns)
        {
            List<(int index,float distance)> distances = new List<(int, float)>();
            for (int areaIndex = 0; areaIndex < waitingAreas.Count; areaIndex++)
            {
                float distance = Vector3.Distance(roadmap.allNodes[spawner.node].transform.position, waitingAreas[areaIndex].transform.position);
                distances.Add((areaIndex, distance));
            }

            List<int> sortedWaitingAreaIndexes = distances.OrderBy(pair => pair.distance)
                                                            .Select(pair => pair.index)
                                                            .ToList();

            spawnerWaitingAreaDistances.Add(spawner.node, sortedWaitingAreaIndexes);
        }
    }

    /*
    *   After the agent reached the waiting area, they will walk to the waiting spot.
    */
    public void walkAgentToWaitingSpot(Agent agent)
    {
        agent.done = false;
        agent.noMap = true;
        agent.noMapGoal = agent.waitingArea.waitingSpots[agent.waitingSpot];
    }

    /*
    *   After the agent reached their waiting spot, they will be teleported to the exact position of the waiting spot.
    *   The agent will be frozen in place and will be an obstacle for other agents.
    *   The closest train door (node) will be set as the agent's goal.
    */
    public void putAgentInWaitingArea(Agent agent)
    {
        agent.setAnimatorStanding(true);
        waitingAgents.Add(agent);
        agent.transform.SetParent(waitingAgentsContainer.transform);

        // Add random offset to the waiting spot position
        // to make the agents look more natural and less aligned
        Vector3 adjustedPosition = agent.waitingArea.waitingSpots[agent.waitingSpot];
        adjustedPosition.x += Random.Range(-0.3f, 0.3f);
        adjustedPosition.z += Random.Range(-0.3f, 0.3f);

        agent.teleportAgent(adjustedPosition);
        
        int closestTrainDoor = FindClosestTrainDoor(ref agent);
        
        agent.rotateAgent(roadmap.allNodes[closestTrainDoor].transform.position);
        
        // Freeze the agent's position and rotation
        Rigidbody rb = agent.GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeAll;

        agent.setNewPath(agent.goal, closestTrainDoor, ref roadmap);
        agent.noMap = false;
        agent.GetComponentInChildren<Renderer>().material = waitingAgentMaterial;
        agent.isWaiting = true;
    }

    internal int FindClosestTrainDoor(ref Agent agent)
    {   
        GameObject train = GameObject.Find("Train"+agent.trainLine);
        GameObject trainDoors = train.transform.Find("NodesInsideTrain").gameObject;

        float closestDistance = Mathf.Infinity;
        Vector3 currentPosition = agent.transform.position;
        Vector3 closestNode = Vector3.zero;

        foreach (Transform node in trainDoors.transform)
        {   
            float distance = Vector3.Distance(currentPosition, node.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestNode = node.position;
            }
        }
        
        int index = -1;

        for(int i = 0; i < roadmap.allNodes.Count; i++)
        {   
            if(roadmap.allNodes[i].transform.position == closestNode)
            {
                index = i;
                break;
            }
        }
        if(index == -1)
        {
            Debug.LogError("No train door found");
        }
        return index;
    }
        

}
