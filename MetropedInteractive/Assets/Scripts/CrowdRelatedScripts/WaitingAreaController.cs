using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/*
*   This class manages all the waiting areas and related functions.
*/
public class WaitingAreaController : MonoBehaviour
{  
    public List<WaitingArea> waitingAreas;                          // All the waiting areas in the scene
    private List<Agent> waitingAgents;                              // Agents that are currently waiting
    private GameObject waitingAgentsContainer;                      // A container for the waiting agent objects in the inspector
    public Dictionary<int, List<int>> spawnerWaitingAreaDistances;  // The distance from each spawner to each waiting area in descending order
    private MapGen.map roadmap;                                     // The map of the nodes in the scene

    public void Initialize()
    {
        waitingAreas = new List<WaitingArea>();
        waitingAgents = new List<Agent>();
        waitingAgentsContainer = GameObject.Find("Waiting Agents");

        foreach(WaitingArea waitingArea in FindObjectsOfType<WaitingArea>())
        {
            waitingAreas.Add(waitingArea);
        }

        roadmap = FindObjectOfType<MapGen>().getRoadmap();

        BuildSpawnerWaitingAreaDistances();
    }

    public void addAgentToWaitingList(Agent agent)
    {
        waitingAgents.Add(agent);
    }

    /*
    *   Get a waiting spot in the closest waiting area that has free spots.
    *   Returns the index of the waiting area in the roadmap and the position of the waiting spot.
    */
    public (int,int) getWaitingAreaSpot(int startNode)
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
    *   After the agent reached the waiting area, they will be teleported to the waiting spot.
    */
    public void putAgentInWaitingArea(Agent agent)
    {
        agent.setAnimatorStanding(true);
        waitingAgents.Add(agent);
        agent.transform.SetParent(waitingAgentsContainer.transform);
        agent.teleportAgent(agent.waitingArea.waitingSpots[agent.waitingSpot]);
        agent.rotateAgent(agent.waitingArea.goal.transform.position);
    }

    [ContextMenu("Board Train")]
    public void BoardTrain()
    {
        for(int i = waitingAgents.Count - 1; i >= 0; i--)
        {
            Agent agent = waitingAgents[i];
            agent.done = false;
            agent.noMap = true;
            agent.noMapGoal = agent.waitingArea.goal.transform.position;
            agent.isWaitingAgent = false;
            FindObjectOfType<Main>().AddToAgentList(agent);
            waitingAgents.RemoveAt(i);
            agent.setAnimatorStanding(false);
        }
        foreach(WaitingArea waitingArea in waitingAreas)
        {
            for(int i = 0; i < waitingArea.isOccupied.Count; i++)
            {
                waitingArea.isOccupied[i] = false;
            }
        }
    }

}
