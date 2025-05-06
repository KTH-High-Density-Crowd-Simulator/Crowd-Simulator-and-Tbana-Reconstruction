using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LOSVisualizer : MonoBehaviour
{
    private Vector2 areaSize;
    public float cellSize = 1.0f; 
    public Material[] losMaterials;  

    private GameObject[,] gridCells;
    private Main mainScript;

    void Start()
    {
        mainScript = FindObjectOfType<Main>();
        areaSize = new Vector2(mainScript.planeSizeX*10, mainScript.planeSizeZ*10);
        CreateGrid();
    }

    void Update()
    {
        UpdateLOS();
    }

    void CreateGrid()
    {
        int cols = Mathf.CeilToInt(areaSize.x / cellSize);
        int rows = Mathf.CeilToInt(areaSize.y / cellSize);
        gridCells = new GameObject[cols, rows];

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Vector3 gridOrigin = new Vector3(-areaSize.x / 2f, 0.01f, -areaSize.y / 2f);
                cell.transform.position = new Vector3(
                    gridOrigin.x + x * cellSize + cellSize / 2f,
                    0.01f,
                    gridOrigin.z + y * cellSize + cellSize / 2f
                );
                cell.transform.localScale = new Vector3(cellSize, cellSize, 1);
                cell.transform.rotation = Quaternion.Euler(90, 0, 0); // Face up
                Destroy(cell.GetComponent<Collider>());
                gridCells[x, y] = cell;
            }
        }
    }

    void UpdateLOS()
    {
        int cols = gridCells.GetLength(0);
        int rows = gridCells.GetLength(1);
        int[,] densityMap = new int[cols, rows];

        List<Agent> walkingAgents = mainScript.agentList;
        List<Agent> waitingAgents = mainScript.waitingAreaController.waitingAgents;

        // Count agents in each cell
        foreach (Agent agent in walkingAgents)
        {
            Vector3 pos = agent.transform.position;
            Vector3 gridOrigin = new Vector3(-areaSize.x / 2f, 0.01f, -areaSize.y / 2f);
            int x = Mathf.FloorToInt((pos.x - gridOrigin.x) / cellSize);
            int y = Mathf.FloorToInt((pos.z - gridOrigin.z) / cellSize);

            if (x >= 0 && x < cols && y >= 0 && y < rows)
            {
                densityMap[x, y]++;
            }
        }
        foreach (Agent agent in waitingAgents)
        {
            Vector3 pos = agent.transform.position;
            Vector3 gridOrigin = new Vector3(-areaSize.x / 2f, 0.01f, -areaSize.y / 2f);
            int x = Mathf.FloorToInt((pos.x - gridOrigin.x) / cellSize);
            int y = Mathf.FloorToInt((pos.z - gridOrigin.z) / cellSize);

            if (x >= 0 && x < cols && y >= 0 && y < rows)
            {
                densityMap[x, y]++;
            }
        }

        // Assign LOS color
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                float area = cellSize * cellSize;
                float density = densityMap[x, y] / area;
                int losIndex = GetLOSIndex(density);
                gridCells[x, y].GetComponent<Renderer>().material = losMaterials[losIndex];
            }
        }
    }

    int GetLOSIndex(float density)
    {
        if (density <= 0.31f) return 0;         // A
        else if (density <= 0.72f) return 1;    // B
        else if (density <= 1.08f) return 2;    // C
        else if (density <= 2.17f) return 3;    // D
        else if (density <= 3.26f) return 4;    // E
        else return 5;                          // F
    }
}
