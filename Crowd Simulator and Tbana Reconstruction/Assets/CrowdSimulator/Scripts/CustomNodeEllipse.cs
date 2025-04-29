using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CustomNodeEllipse : CustomNode
{
    public override bool IsAgentInsideArea(Vector3 agentPosition)
	{
        Vector3 localPos = transform.InverseTransformPoint(agentPosition);
		float x = localPos.x;
		float z = localPos.z;

		return (x * x) / (0.5f * 0.5f) + (z * z) / (0.5f * 0.5f) <= 1f;
	}
    
    public override Vector3 getTargetPoint(Vector3 origin)
    {
        float scaleX = transform.lossyScale.x;
        float scaleZ = transform.lossyScale.z;

        Vector3 localOrigin = transform.InverseTransformPoint(origin);

        Vector3 dir;
        if (scaleX >= scaleZ)
        {
            dir = new Vector3(1, 0, 0);
        }
        else
        {
            dir = new Vector3(0, 0, 1);
        }

        float projection = Vector3.Dot(localOrigin, dir);
        projection = Mathf.Clamp(projection, -0.5f, 0.5f);

        float randomPoint = projection;
        if(projection <= -0.5f)
        {
            randomPoint = Random.Range(-0.5f, 0f);
        }
        else if(projection >= 0.5f)
        {
            randomPoint = Random.Range(0f, 0.5f);
        }
        
        Vector3 target = dir * randomPoint;
        return transform.TransformPoint(target);
    }
    
}
