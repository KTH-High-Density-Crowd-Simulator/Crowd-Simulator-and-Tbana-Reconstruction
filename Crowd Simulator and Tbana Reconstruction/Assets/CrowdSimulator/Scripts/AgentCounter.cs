using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentCounter : MonoBehaviour
{
    public Main main;
    public WaitingAreaController waitingAreaController;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmos()
	{
        if(main.agentList == null || waitingAreaController.waitingAgents == null)
        {
            return;
        }
        int nAgents = main.agentList.Count + waitingAreaController.waitingAgents.Count;
		UnityEditor.Handles.color = Color.red;
		UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, nAgents.ToString());
		
	}
}
