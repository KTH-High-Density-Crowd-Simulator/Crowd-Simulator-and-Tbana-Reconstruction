using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainSpawner : MonoBehaviour
{
    private int numberOfAgents;
    private int[] goals = new int[2];
    private float burstRate;
    private GameObject agentContainer;
    private Main mainScript;
    private Agent agentPrefab;
    internal Material alightingAgentMaterial;
    internal bool done = false;
    private bool alightBeforeBoarding;

    // Start is called before the first frame update
    public void Initialize(int numberOfAgents, int goal1, int goal2, float burstRate, GameObject agentContainer, Agent agentPrefab, Material alightingAgentMaterial, bool alightBeforeBoarding)
    {
        this.agentPrefab = agentPrefab;
        this.burstRate = burstRate;
        this.agentContainer = agentContainer;
        this.numberOfAgents = numberOfAgents;
        goals[0] = goal1;
        goals[1] = goal2;
        this.alightingAgentMaterial = alightingAgentMaterial;
        this.alightBeforeBoarding = alightBeforeBoarding;
        mainScript = FindObjectOfType<Main>();
    }

    public IEnumerator SpawnAgents()
    {
        for (int i = 0; i < numberOfAgents; ++i) {
			//Vector3 startPos = new Vector3(transform.position.x + Random.Range(-1.5f, 1.5f), transform.position.y, transform.position.z + Random.Range(-0.5f, 0.5f));
			Vector3 startPos = new Vector3(transform.position.x, 0f, transform.position.z + Random.Range(-0.5f, 0.5f));
            spawnOneAgent (startPos);
			yield return new WaitForSeconds (burstRate + Random.Range(-0.1f, 0.2f));
		}
        done = true;
    }

    public void spawnOneAgent(Vector3 startPosition)
	{
		Agent agent;
		agent = Instantiate (agentPrefab);
        agent.GetComponentInChildren<Renderer>().material = alightingAgentMaterial;

        int node = transform.GetComponent<CustomNode>().index;

        int goal;
        if(goals[1] == -1)
        {
            goal = goals[0];
        }
        else
        {
            // Choose goals[0] 70% of the time, goals[1] 30% of the time
            goal = (Random.value < 0.7f) ? goals[0] : goals[1];
        }

		agent.InitializeAgent (startPosition, node, goal, ref mainScript.roadmap);

        if(alightBeforeBoarding)
        {
            if(startPosition.x > 0)
            {
                agent.noMapGoal = new Vector3(transform.position.x - 4f, 0f, startPosition.z);
            }else
            {
                agent.noMapGoal = new Vector3(transform.position.x + 4f, 0f, startPosition.z);
            }
            agent.noMap = true;
        }
		
        agent.isAlighting = true;
		if (agentContainer != null)
			agent.transform.parent = agentContainer.transform;

		mainScript.agentList.Add (agent);
	}
}
