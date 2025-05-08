using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LOSVisualizer : MonoBehaviour
{
    private Vector2 areaSize;
    public float cellSize = 1.0f; 
    private GameObject[,] gridCells;
    private Main mainScript;
    public Gradient losGradient;
    private const float maxDensity = 5.26f;
    private float[,] densityMap;
    public int smoothingRadius = 1; // 1 = 3x3, 2 = 5x5

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
        densityMap = new float[cols, rows];

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
                cell.transform.SetParent(transform);
            }
        }
    }

void UpdateLOS()
    {
        int cols = gridCells.GetLength(0);
        int rows = gridCells.GetLength(1);
        System.Array.Clear(densityMap, 0, densityMap.Length);

        foreach (Agent agent in mainScript.agentList)
        {
            Vector3 pos = agent.transform.position;
            int x = Mathf.FloorToInt((pos.x + areaSize.x / 2f) / cellSize);
            int y = Mathf.FloorToInt((pos.z + areaSize.y / 2f) / cellSize);

            if (x >= 0 && x < cols && y >= 0 && y < rows)
            {
                densityMap[x, y] += 1.0f;
            }
        }
        
        // Assign gradient colors based on smoothed density
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                float smoothedDensity = GetSmoothedDensity(x, y, cols, rows);
                float normalized = Mathf.Clamp01(smoothedDensity / maxDensity);
                Color color = losGradient.Evaluate(normalized);
                gridCells[x, y].GetComponent<Renderer>().material.color = color;
            }
        }
    }

    float GetSmoothedDensity(int cx, int cy, int cols, int rows)
    {
        float total = 0;
        int count = 0;

        for (int dx = -smoothingRadius; dx <= smoothingRadius; dx++)
        {
            for (int dy = -smoothingRadius; dy <= smoothingRadius; dy++)
            {
                int x = cx + dx;
                int y = cy + dy;
                if (x >= 0 && x < cols && y >= 0 && y < rows)
                {
                    total += densityMap[x, y];
                    count++;
                }
            }
        }

        float averageDensity = total / count;
        return averageDensity / (cellSize * cellSize);
    }

}
