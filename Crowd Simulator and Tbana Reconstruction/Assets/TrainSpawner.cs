using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainSpawner : MonoBehaviour
{
    private int numberOfAgents;
    private int goal;
    private float burstRate;
    private GameObject agentContainer;
    private Main mainScript;
    private Agent agentPrefab;

    // Start is called before the first frame update
    public void Initialize(int numberOfAgents, int goal, float burstRate, GameObject agentContainer, Agent agentPrefab)
    {
        this.agentPrefab = agentPrefab;
        this.burstRate = burstRate;
        this.agentContainer = agentContainer;
        this.numberOfAgents = numberOfAgents;
        this.goal = goal;
        mainScript = FindObjectOfType<Main>();
    }

    public IEnumerator SpawnAgents()
    {
        for (int i = 0; i < numberOfAgents; ++i) {
			Vector3 startPos = new Vector3(transform.position.x + Random.Range(-1.5f, 1.5f), transform.position.y, transform.position.z + Random.Range(-1.5f, 1.5f));
			spawnOneAgent (startPos);
			yield return new WaitForSeconds (burstRate);
		}
    }

    public void spawnOneAgent(Vector3 startPosition)
	{
		Agent agent;
		agent = Instantiate (agentPrefab);

        int node = transform.GetComponent<CustomNode>().index;

		agent.InitializeAgent (startPosition, node, goal, ref mainScript.roadmap);
		//agent.ApplyMaterials(materialColor, ref skins);

		if (agentContainer != null)
			agent.transform.parent = agentContainer.transform;

		mainScript.agentList.Add (agent);
	}
}
