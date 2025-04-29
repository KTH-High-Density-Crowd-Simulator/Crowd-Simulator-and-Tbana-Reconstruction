using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WaitingAreaNode : CustomNode
{
    public float thresholdDistance = 1f;

    /**
    * Returns the closest point on the waiting area plane from the agent's position (origin).
    * The agents will steer toward this point.
    */
    public override Vector3 getTargetPoint(Vector3 origin)
    {
        // Multiply by 5 because a plane's default dimensions is 10x10 world units
        // when its scale is (1,1,1) and we want half the width and height.
        float width = transform.Find("Area").transform.localScale.x * 5f;
        float height = transform.Find("Area").transform.localScale.z * 5f;

        Vector3 localPoint = transform.InverseTransformPoint(origin);

        localPoint.x = Mathf.Clamp(localPoint.x, -width, width);
        localPoint.z = Mathf.Clamp(localPoint.z, -height, height);
        localPoint.y = 0;

        return transform.TransformPoint(localPoint);
    }

    /**
    * The agents have reached this node when they are within the threshold distance of the plane.
    */
    public override bool IsAgentInsideArea(Vector3 agentPosition)
	{
		return Vector3.Distance(agentPosition, getTargetPoint(agentPosition)) < thresholdDistance;
	}
}
