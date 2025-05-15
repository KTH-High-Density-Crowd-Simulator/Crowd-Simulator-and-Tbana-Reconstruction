using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Train : MonoBehaviour
{
    public int trainLine;
    public int numberOfAgents;
    internal List<TrainSpawner> trainSpawners;
    public List<CustomNode> goalNodes;
    public float burstRate = 0.5f; // Time between agent spawns
    public GameObject agentContainer;
    public Agent agentPrefab;
    public Material alightingAgentMaterial;

    // Start is called before the first frame update
    void Start()
    {
        if(goalNodes == null || goalNodes.Count == 0)
        {
            Debug.LogError("Goal nodes not set for train " + gameObject.name);
            return;
        }
        if(agentContainer == null)
        {
            Debug.LogError("Agent container not set for train " + gameObject.name);
            return;
        }
        trainSpawners = new List<TrainSpawner>();

        Transform spawners = transform.Find("TrainSpawners");
        TrainController trainController = FindObjectOfType<TrainController>();
        bool alightBeforeBoarding = trainController.alightBeforeBoarding;
        TrainController.PlatformType platformType = trainController.platformType;

        foreach (Transform spawnerTransform in spawners)
        {
            TrainSpawner spawner = spawnerTransform.GetComponent<TrainSpawner>();
            trainSpawners.Add(spawner);
            int closestGoal = -1;
            float closestDistance = Mathf.Infinity;
            int goal1 = -1;
            int goal2 = -1;
            int closestIndex = -1;

            for (int i = 0; i < goalNodes.Count; i++)
            {
                float distance = Vector3.Distance(spawner.transform.position, goalNodes[i].transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestGoal = goalNodes[i].index;
                    closestIndex = i;
                }
            }

            if (closestIndex == 0 || closestIndex == 3)
            {
                goal1 = closestGoal;
                goal2 = -1;
            }
            else if (closestIndex == 1 || closestIndex == 2)
            {
                goal1 = goalNodes[1].index;
                goal2 = goalNodes[2].index;

                if (closestIndex == 2)
                {
                    // Swap so goal1 is always the closest
                    int temp = goal1;
                    goal1 = goal2;
                    goal2 = temp;
                }
            }

            int nAgentsPerDoor = numberOfAgents / spawners.childCount;
            spawner.Initialize(nAgentsPerDoor, goal1, goal2, burstRate, agentContainer, agentPrefab, alightingAgentMaterial, alightBeforeBoarding, platformType);
        }
        
    }

    [ContextMenu("Alight")]
    public void Alight()
    {
        foreach (TrainSpawner spawner in trainSpawners)
        {   
            spawner.done = false;
            StartCoroutine (spawner.SpawnAgents());
        }
    }
}
