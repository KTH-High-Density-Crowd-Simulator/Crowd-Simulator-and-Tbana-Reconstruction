using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainController : MonoBehaviour
{
    public GameObject train1;
    public GameObject train2;
    private float arrivalTimer = 0f;
    public float arriveInterval = 10f;
    private bool train1Dwelling = false;
    private bool train2Dwelling = false;
    public float dwellTime = 5f;
    private float dwellTimer = 0f;
    public WaitingAreaController waitingAreaController;
    private Main mainScript;
 
    
    private void Alight(int trainLine)
    {
        if (trainLine == 1)
        {
            train1.GetComponent<Train>().Alight();
        }
        else if (trainLine == 2)
        {
            train2.GetComponent<Train>().Alight();
        }
        else
        {
            Debug.LogError("Invalid train line: " + trainLine);
        }
    }

    private void ToggleTrain(int trainLine)
    {
        if (trainLine == 1)
        {
            train1.transform.GetChild(0).gameObject.SetActive(train1Dwelling);
            train1.transform.GetChild(1).gameObject.SetActive(train1Dwelling);
            train1.transform.GetChild(2).gameObject.SetActive(train1Dwelling);
        }
        else if (trainLine == 2)
        {
            train2.transform.GetChild(0).gameObject.SetActive(train2Dwelling);
            train2.transform.GetChild(1).gameObject.SetActive(train2Dwelling);
            train2.transform.GetChild(2).gameObject.SetActive(train2Dwelling);
        }

    }
    

    void Start()
    {
        if(waitingAreaController == null)
        {
            Debug.LogError("WaitingAreaController is not set in TrainController");
        }
        mainScript = FindObjectOfType<Main>();
    }

    void Update()
    {
        arrivalTimer += Time.deltaTime;

        // The train arrives
        if (arrivalTimer >= arriveInterval)
        {
            arrivalTimer = 0f;
            train1Dwelling = true;
            train2Dwelling = true;

            ToggleTrain(1);
            ToggleTrain(2);

            Alight(1);
            Alight(2);

            Board(1);
            Board(2);
        }
        // The train is dwelling
        if(train1Dwelling || train2Dwelling)
        {
            dwellTimer += Time.deltaTime;
            if (dwellTimer >= dwellTime)
            {
                train1Dwelling = false;
                train2Dwelling = false;
                ToggleTrain(1);
                ToggleTrain(2);
                dwellTimer = 0f;
            }
        }

    }

    public void Board(int trainLine)
    {

		for(int i = mainScript.agentList.Count - 1; i >= 0; i--)
		{
			Agent agent = mainScript.agentList[i];
			if(agent.subwayData.HasValue && agent.subwayData.Value.trainLine == trainLine && !agent.subwayData.Value.boarding)
			{
				var data = agent.subwayData ?? new Agent.SubwayData();
				data.boarding = true;
				agent.subwayData = data;

				int closestTrainDoor = waitingAreaController.FindClosestTrainDoor(ref agent);
				int closestNode = FindClosestNode(agent.transform.position);
				agent.setNewPath(closestNode, closestTrainDoor, ref mainScript.roadmap);
				if(agent.isWaitingAgent)
				{
					agent.waitingArea.isOccupied[agent.waitingSpot] = false;
                	agent.waitingArea.freeWaitingSpots.Add(agent.waitingSpot);
					agent.isWaitingAgent = false;
				}
			}
		}

		waitingAreaController.BoardWaitingAgents(trainLine);
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
}
