using UnityEngine;
using System.Collections;

public class CustomNode : MonoBehaviour {

	public bool isSpawn = false;
	public bool isGoal = false;
	public int index;

	public virtual bool IsAgentInsideArea(Vector3 agentPosition)
	{
		Vector3 A = transform.TransformPoint(new Vector3 (0.5f, 0, 0));
		Vector3 B = transform.TransformPoint (new Vector3 (0, 0, 0));
		float radius = (A - B).magnitude;
		return Vector3.Distance(transform.position, agentPosition) < radius;
	}

	public virtual Vector3 getTargetPoint(Vector3 origin) {
		return transform.position;
	}

/**
	private void OnDrawGizmos()
	{
		UnityEditor.Handles.color = Color.white;
		UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, index.ToString());
	}
	*/


}
