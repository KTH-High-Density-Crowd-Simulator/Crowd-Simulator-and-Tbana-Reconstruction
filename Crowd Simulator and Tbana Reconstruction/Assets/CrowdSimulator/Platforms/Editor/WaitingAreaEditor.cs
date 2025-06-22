using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WaitingArea))]
public class WaitingAreaEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WaitingArea waitingArea = (WaitingArea)target;

        if (waitingArea.priorityMaterials != null &&
            waitingArea.priority >= 0 &&
            waitingArea.priority < waitingArea.priorityMaterials.Length &&
            waitingArea.priorityMaterials[waitingArea.priority] != null)
        {
            Transform area = waitingArea.transform.Find("Area");

            MeshRenderer renderer = area.GetComponent<MeshRenderer>();
            Material selectedMat = waitingArea.priorityMaterials[waitingArea.priority];

            if (renderer.sharedMaterial != selectedMat)
            {
                renderer.sharedMaterial = selectedMat;
                EditorUtility.SetDirty(waitingArea);
            }
        }
    }
}