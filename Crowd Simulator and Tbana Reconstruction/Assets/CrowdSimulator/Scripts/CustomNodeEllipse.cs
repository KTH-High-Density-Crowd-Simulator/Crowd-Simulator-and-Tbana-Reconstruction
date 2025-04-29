using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CustomNodeEllipse : CustomNode
{
    private Matrix4x4 inverseTransform;
    private float a; // X radius
    private float b; // Z radius

    void Start()
    {
        inverseTransform = transform.worldToLocalMatrix;
        a = 0.5f * transform.lossyScale.x; // X radius
		b = 0.5f * transform.lossyScale.z; // Z radius
    }

    public bool IsAgentInsideArea(Vector3 agentPosition)
	{
		Vector3 localPos = inverseTransform.MultiplyPoint3x4(agentPosition);
		float x = localPos.x;
		float z = localPos.z;

		return (x * x) / (a * a) + (z * z) / (b * b) <= 1f;
	}

    // The target point is the closest point on the longer
    // axis of the ellipse to the agent's position.
    public override Vector3 getTargetPoint(Vector3 origin)
    {
        Vector3 localOrigin = transform.InverseTransformPoint(origin);

        float scaleX = transform.lossyScale.x;
        float scaleZ = transform.lossyScale.z;

        Vector3 dir;
        if (scaleX >= scaleZ)
        {
            dir = new Vector3(1, 0, 0); // major axis is local X
        }
        else
        {
            dir = new Vector3(0, 0, 1); // major axis is local Z
        }

        float projection = Vector3.Dot(localOrigin, dir);
        projection = Mathf.Clamp(projection, -0.5f, 0.5f);
        Vector3 localTarget = dir * projection;
        return transform.TransformPoint(localTarget);
    }
}
