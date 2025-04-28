using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Train : MonoBehaviour
{
    public int trainLine;
    public int numberOfAgents;
    private List<TrainSpawner> trainSpawners;
    public List<CustomNode> goalNodes;
    public float burstRate = 0.5f; // Time between agent spawns
    public GameObject agentContainer;
    public Agent agentPrefab;

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

        foreach (Transform spawnerTransform in spawners)
        {
            TrainSpawner spawner = spawnerTransform.GetComponent<TrainSpawner>();
            trainSpawners.Add(spawner);
            int closestGoal = -1;
            float closestDistance = Mathf.Infinity;
            foreach (CustomNode goal in goalNodes)
            {
                float distance = Vector3.Distance(spawner.transform.position, goal.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestGoal = goal.index;
                }
            }
            int nAgentsPerDoor = numberOfAgents / spawners.childCount;
            spawner.Initialize(nAgentsPerDoor,closestGoal, burstRate, agentContainer, agentPrefab);
        }
        
    }

    [ContextMenu("Alight")]
    public void Alight()
    {
        foreach (TrainSpawner spawner in trainSpawners)
        {   
            StartCoroutine (spawner.SpawnAgents());
        }
    }
}
