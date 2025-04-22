using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
*   This script manages one waiting area.
*   It generates a grid of waiting spots and keeps track of which spots are occupied.
*/
public class WaitingArea : MonoBehaviour
{
    // Grid based on number of rows and columns
    public int rows = 3;
    public int columns = 5;
    public bool debug = false;
    public float waitingSpotSize = 0.5f;

    internal List<Vector3> waitingSpots;
    internal List<bool> isOccupied;
    internal List<int> freeWaitingSpots;
    private int currentWaitingSpotIndex = 0;
    private int mapIndex;
    public bool useRowColumns = false;

    void OnEnable()
    {
        if(useRowColumns)
        {
            GenerateRowColumnWaitingSpots();
        }   
        else
        {
            GenerateFixedSizeWaitingSpots();
        }
        freeWaitingSpots = Enumerable.Range(0, waitingSpots.Count).ToList();
    }

    /*
    *   Generates a grid of waiting spots for this waiting area 
    *   based on the number of rows and columns set by the user in the editor.
    */
    void GenerateRowColumnWaitingSpots()
    {
        waitingSpots = new List<Vector3>();
        isOccupied = new List<bool>(new bool[columns*rows]);

        Renderer renderer = transform.Find("Area").GetComponent<Renderer>();
        Bounds bounds = renderer.bounds;

        Vector3 corner = new Vector3(bounds.min.x, transform.position.y, bounds.min.z);
        Vector3 size = bounds.size;

        float cellWidth = size.x / columns;
        float cellHeight = size.z / rows;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                float xPos = corner.x + cellWidth * 0.5f + col*cellWidth;
                float zPos = corner.z + cellHeight * 0.5f + row*cellHeight;
                Vector3 spotPosition = new Vector3(xPos, transform.position.y, zPos);
                waitingSpots.Add(spotPosition);

                isOccupied[col + row * columns] = false;

                if(debug)
                {
                    Debug.DrawLine(spotPosition, spotPosition + Vector3.up * 0.5f, Color.red, 10f);
                }
                
            }
        }
    }

    void GenerateFixedSizeWaitingSpots()
    {
        waitingSpots = new List<Vector3>();

        Renderer renderer = transform.Find("Area").GetComponent<Renderer>();
        Bounds bounds = renderer.bounds;

        Vector3 corner = new Vector3(bounds.min.x, transform.position.y, bounds.min.z);
        Vector3 size = bounds.size;

        int columns = Mathf.FloorToInt(size.x / waitingSpotSize);
        int rows = Mathf.FloorToInt(size.z / waitingSpotSize);

        isOccupied = new List<bool>(new bool[columns * rows]);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                float xPos = corner.x + waitingSpotSize * 0.5f + col * waitingSpotSize;
                float zPos = corner.z + waitingSpotSize * 0.5f + row * waitingSpotSize;
                Vector3 spotPosition = new Vector3(xPos, transform.position.y, zPos);
                waitingSpots.Add(spotPosition);

                isOccupied[col + row * columns] = false;

                if (debug)
                {
                    Debug.DrawLine(spotPosition, spotPosition + Vector3.up * 0.5f, Color.red, 10f);
                }
            }
        }
}

    /*
    *   The waiting area is also a node used for the agents' pathfinding.
    *   All nodes in the scene are stored in a list in the MapGen script.
    *   This is the index of this node in that list.
    */
    public void setMapIndex(int index)
    {
        mapIndex = index;
    }

    /*
    *   Finds a free waiting spot in the waiting area.
    *   Returns the index of the waiting area (in the MapGen roadmap) and the index of the waiting spot (in the waitingSpots list).
    *   If there are no free spots, returns (-1, -1).
    */
    public (int index, int waitingSpot) getWaitingSpot()
    {
        // If there are available spots
        if(!isOccupied.All(spot => spot == true))
        {
            int waitingSpot = getRandomWaitingSpot();
            return (mapIndex, waitingSpot);
        }

        // No available spots
        return (-1, -1);
    }

    private int getWaitingSpotInOrder()
    {
        isOccupied[currentWaitingSpotIndex] = true;
        currentWaitingSpotIndex++;
        return currentWaitingSpotIndex-1;

    }

    private int getRandomWaitingSpot()
    {
        int randomIndex = Random.Range(0, freeWaitingSpots.Count);
        int waitingSpot = freeWaitingSpots[randomIndex];
        freeWaitingSpots.RemoveAt(randomIndex);
        isOccupied[waitingSpot] = true;
        return waitingSpot;
    }

}
