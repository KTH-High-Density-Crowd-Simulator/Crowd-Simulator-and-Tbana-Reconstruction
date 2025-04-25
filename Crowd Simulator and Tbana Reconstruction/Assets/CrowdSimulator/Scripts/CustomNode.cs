using UnityEngine;
using System.Collections;

public class CustomNode : MonoBehaviour {

	public bool isSpawn = false;
	public bool isGoal = false;
	public int index;

	public virtual float getThreshold() {
		Vector3 A = transform.TransformPoint(new Vector3 (0.5f, 0, 0));
		Vector3 B = transform.TransformPoint (new Vector3 (0, 0, 0));
		return (A - B).magnitude; //Radius

	}

	public virtual Vector3 getTargetPoint(Vector3 origin) {
		return transform.position;

	}
}
