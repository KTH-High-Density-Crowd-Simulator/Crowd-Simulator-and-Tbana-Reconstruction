// DensityMapVisualizer.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Generates and visualizes a dynamic density map based on agent positions
/// using a Kernel Density Estimation (KDE) like approach with an Epanechnikov kernel.
/// This aims to replicate the "smooth" continuous density described by AnyLogic.
/// Performance is optimized using a spatial hash grid to reduce agent lookups.
/// </summary>
public class DensityMapVisualizer : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Reference to the Main script to access simulation dimensions and agents.")]
    public Main mainScript;

    [Tooltip("The GameObject (e.g., a Quad or Plane) that will display the heatmap texture.")]
    public GameObject heatmapQuad;

    [Header("Density Calculation Settings")]
    [Tooltip("The radius (in meters) around a point within which agents contribute to its density calculation. " +
             "This is akin to AnyLogic's 'local neighborhood' or 'moving probe' radius. " +
             "Smaller values give more detail, larger values give smoother results.")]
    [Range(0.5f, 5.0f)] // Suggesting a reasonable range for pedestrian simulations
    public float densityMeasurementRadius = 1.5f; // meters

    [Tooltip("The maximum density value (people/m^2) that maps to the highest color in the LOS Gradient. " +
             "Values above this will clamp to the highest color. " +
             "Crucial for consistent color mapping across different simulation runs/densities.")]
    [Range(1.0f, 10.0f)] // Fruin's LOS typically goes up to ~5-7 p/m^2, but can be higher for crush.
    public float maxLosDensity = 5.0f; // people/m^2 (e.0g., 5.0 p/m^2)

    [Header("Visualization Settings")]
    [Tooltip("The resolution (width) of the generated heatmap texture in pixels. Higher values mean more detail.")]
    [Range(50, 500)]
    public int resolutionX = 200;

    [Tooltip("The resolution (height) of the generated heatmap texture in pixels. Higher values mean more detail.")]
    [Range(50, 500)]
    public int resolutionY = 200; // This now corresponds to the Z-dimension in world space

    [Tooltip("The color gradient to use for the density map, based on Level of Service (LOS).")]
    public Gradient losGradient;

    [Tooltip("How often the density map is recalculated and updated (in seconds).")]
    [Range(0.05f, 1.0f)]
    public float updateInterval = 0.1f; // Update every 0.1 seconds

    [Header("Optimization Settings")]
    [Tooltip("Size of each cell in the spatial grid (in meters). " +
             "Should be at least 'densityMeasurementRadius' to ensure all relevant agents are checked. " +
             "Smaller cells mean more precise lookups but more cells to manage. Larger cells mean fewer cells but wider agent searches.")]
    [Range(0.5f, 5.0f)]
    public float gridCellSize = 2.0f; // Meters per grid cell

    private Texture2D _heatmapTexture;
    private Coroutine _updateCoroutine;

    // Internal variables to store the actual world dimensions in meters
    private float _actualPlaneWorldSizeX;
    private float _actualPlaneWorldSizeZ;

    // Spatial grid for optimization
    private List<Agent>[,] _agentGrid;
    private int _gridCellsX;
    private int _gridCellsZ;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes references and sets up the heatmap visualization.
    /// </summary>
    void Awake()
    {
        Debug.Log("DensityMapVisualizer: Awake started.");

        if (mainScript == null)
        {
            mainScript = FindObjectOfType<Main>();
            if (mainScript == null)
            {
                Debug.LogError("DensityMapVisualizer: Main script not found! Please assign it in the inspector or ensure it exists in the scene.");
                enabled = false; // Disable this script if Main is not found
                return;
            }
        }
        Debug.Log("DensityMapVisualizer: Main script found.");

        if (heatmapQuad == null)
        {
            Debug.LogError("DensityMapVisualizer: Heatmap Quad GameObject is not assigned! Please assign a Quad or Plane to display the heatmap.");
            enabled = false;
            return;
        }
        Debug.Log("DensityMapVisualizer: Heatmap Quad found.");

        // Calculate actual world dimensions based on Main script's scale values
        // A Unity Plane primitive is 10x10 units by default.
        _actualPlaneWorldSizeX = mainScript.planeSizeX * 10f;
        _actualPlaneWorldSizeZ = mainScript.planeSizeZ * 10f;

        Debug.Log($"DensityMapVisualizer: Derived Actual Plane World Size: X={_actualPlaneWorldSizeX:F2}m, Z={_actualPlaneWorldSizeZ:F2}m.");

        InitializeSpatialGrid();
        SetupHeatmapQuad();
        StartDensityMapUpdate();
        Debug.Log("DensityMapVisualizer: Awake finished. Heatmap update started.");
    }

    /// <summary>
    /// Initializes the spatial hash grid based on plane dimensions and cell size.
    /// </summary>
    private void InitializeSpatialGrid()
    {
        _gridCellsX = Mathf.CeilToInt(_actualPlaneWorldSizeX / gridCellSize);
        _gridCellsZ = Mathf.CeilToInt(_actualPlaneWorldSizeZ / gridCellSize);
        _agentGrid = new List<Agent>[_gridCellsX, _gridCellsZ];

        for (int x = 0; x < _gridCellsX; x++)
        {
            for (int z = 0; z < _gridCellsZ; z++)
            {
                _agentGrid[x, z] = new List<Agent>();
            }
        }
        Debug.Log($"DensityMapVisualizer: Initialized spatial grid: {_gridCellsX}x{_gridCellsZ} cells with cell size {gridCellSize}m.");
    }

    /// <summary>
    /// Populates the spatial grid with current agent positions.
    /// This should be called once per heatmap update.
    /// </summary>
    private void PopulateAgentGrid()
    {
        // Clear previous agent assignments
        for (int x = 0; x < _gridCellsX; x++)
        {
            for (int z = 0; z < _gridCellsZ; z++)
            {
                _agentGrid[x, z].Clear();
            }
        }

        // Assign agents to their respective grid cells
        foreach (Agent agent in mainScript.agentList)
        {
            if (agent == null) continue; // Skip if agent was destroyed

            // Convert agent world position to grid coordinates
            // Assuming plane is centered at origin (0,0,0)
            int gridX = Mathf.FloorToInt((agent.transform.position.x + _actualPlaneWorldSizeX / 2f) / gridCellSize);
            int gridZ = Mathf.FloorToInt((agent.transform.position.z + _actualPlaneWorldSizeZ / 2f) / gridCellSize);

            // Clamp to ensure agent is within grid bounds
            gridX = Mathf.Clamp(gridX, 0, _gridCellsX - 1);
            gridZ = Mathf.Clamp(gridZ, 0, _gridCellsZ - 1);

            _agentGrid[gridX, gridZ].Add(agent);
        }
    }


    /// <summary>
    /// Sets up the Quad/Plane GameObject to display the heatmap.
    /// Scales it to match the simulation area and applies a new texture.
    /// </summary>
    private void SetupHeatmapQuad()
    {
        Debug.Log("DensityMapVisualizer: Setting up Heatmap Quad.");

        // Create the texture for the heatmap
        _heatmapTexture = new Texture2D(resolutionX, resolutionY, TextureFormat.RGB24, false);
        _heatmapTexture.filterMode = FilterMode.Bilinear; // For smoother appearance
        _heatmapTexture.wrapMode = TextureWrapMode.Clamp; // Prevent tiling
        Debug.Log($"DensityMapVisualizer: Created Texture2D with resolution {resolutionX}x{resolutionY}.");

        // Ensure the heatmapQuad has a Renderer and a Material
        Renderer quadRenderer = heatmapQuad.GetComponent<Renderer>();
        if (quadRenderer == null)
        {
            quadRenderer = heatmapQuad.AddComponent<MeshRenderer>();
            Debug.LogWarning("DensityMapVisualizer: Heatmap Quad did not have a Renderer. Added MeshRenderer.");
        }

        Material heatmapMaterial = quadRenderer.material;
        if (heatmapMaterial == null || heatmapMaterial.shader.name != "Unlit/Texture")
        {
            // Create a simple Unlit/Texture material if none or wrong shader is applied
            Shader unlitTextureShader = Shader.Find("Unlit/Texture");
            if (unlitTextureShader == null)
            {
                Debug.LogError("DensityMapVisualizer: 'Unlit/Texture' shader not found! Heatmap will likely not display correctly. " +
                               "Ensure it's in your project's Graphics settings under 'Always Included Shaders' or a material using it is in a scene.");
            }
            heatmapMaterial = new Material(unlitTextureShader);
            quadRenderer.material = heatmapMaterial;
            Debug.LogWarning("DensityMapVisualizer: Heatmap Quad's material was not Unlit/Texture or was null. Created a new one.");
        } else {
            Debug.Log("DensityMapVisualizer: Heatmap Quad's material already uses 'Unlit/Texture'.");
        }

        // Assign the texture to the material
        heatmapMaterial.mainTexture = _heatmapTexture;
        Debug.Log("DensityMapVisualizer: Assigned texture to heatmap material.");

        // Scale the quad to match the ACTUAL simulation area dimensions in meters.
        // A Unity Quad primitive is 1x1 unit by default.
        // After rotating 90 degrees around X, Quad's local X maps to world X, local Y maps to world Z.
        // localScale.z will be the "thickness" in world Y, so set to 1f.
        heatmapQuad.transform.localScale = new Vector3(_actualPlaneWorldSizeX, _actualPlaneWorldSizeZ, 1f);
        heatmapQuad.transform.position = new Vector3(0, 0.01f, 0); // Slightly above Y=0
        heatmapQuad.transform.rotation = Quaternion.Euler(90, 0, 0); // Rotate for XZ plane (Quads are typically XY)
        Debug.Log($"DensityMapVisualizer: Heatmap Quad scaled to {_actualPlaneWorldSizeX:F2}x{_actualPlaneWorldSizeZ:F2} and positioned.");
    }

    /// <summary>
    /// Starts the coroutine for periodically updating the density map.
    /// </summary>
    private void StartDensityMapUpdate()
    {
        if (_updateCoroutine != null)
        {
            StopCoroutine(_updateCoroutine);
        }
        _updateCoroutine = StartCoroutine(UpdateDensityMapCoroutine());
    }

    /// <summary>
    /// Coroutine that continuously calculates and applies the density map at a fixed interval.
    /// </summary>
    private IEnumerator UpdateDensityMapCoroutine()
    {
        while (true)
        {
            CalculateAndApplyDensityMap();
            yield return new WaitForSeconds(updateInterval);
        }
    }

    /// <summary>
    /// Calculates the density for each pixel in the heatmap texture
    /// and applies the corresponding color from the LOS gradient.
    /// Uses an Epanechnikov kernel for smooth density falloff.
    /// Optimized using a spatial grid to only check nearby agents.
    /// </summary>
    private void CalculateAndApplyDensityMap()
    {
        List<Agent> agents = mainScript.agentList;

        if (agents == null || agents.Count == 0)
        {
            Color[] emptyColors = new Color[resolutionX * resolutionY];
            for (int i = 0; i < emptyColors.Length; i++) {
                emptyColors[i] = Color.clear; // Or Color.black;
            }
            _heatmapTexture.SetPixels(emptyColors);
            _heatmapTexture.Apply();
            return; // Exit early if no agents
        }

        // Step 1: Populate the spatial grid with current agent positions
        PopulateAgentGrid();

        Color[] pixelColors = new Color[resolutionX * resolutionY];

        // Use the actual world sizes for coordinate mapping
        float invPlaneSizeX = 1.0f / _actualPlaneWorldSizeX;
        float invPlaneSizeZ = 1.0f / _actualPlaneWorldSizeZ;

        float radiusSq = densityMeasurementRadius * densityMeasurementRadius;

        // For debugging a single pixel's value (e.g., center pixel)
        int debugPixelX = resolutionX / 2;
        int debugPixelY = resolutionY / 2;
        bool isFirstUpdate = true; // To only log detailed pixel info once per run

        for (int y = 0; y < resolutionY; y++)
        {
            for (int x = 0; x < resolutionX; x++)
            {
                // Calculate the world position corresponding to this pixel center
                float normalizedX = (x + 0.5f) / resolutionX;
                float normalizedY = (y + 0.5f) / resolutionY;

                float worldX = (normalizedX - 0.5f) * _actualPlaneWorldSizeX;
                float worldZ = (normalizedY - 0.5f) * _actualPlaneWorldSizeZ;

                Vector3 samplePoint = new Vector3(worldX, 0, worldZ);
                float currentDensity = 0f;

                // Determine the grid cell for the current samplePoint
                int sampleGridX = Mathf.FloorToInt((samplePoint.x + _actualPlaneWorldSizeX / 2f) / gridCellSize);
                int sampleGridZ = Mathf.FloorToInt((samplePoint.z + _actualPlaneWorldSizeZ / 2f) / gridCellSize);

                // Iterate over neighboring grid cells (e.g., 3x3 block around sampleGridX/Z)
                for (int gx = sampleGridX - 1; gx <= sampleGridX + 1; gx++)
                {
                    for (int gz = sampleGridZ - 1; gz <= sampleGridZ + 1; gz++)
                    {
                        // Ensure grid coordinates are within bounds
                        if (gx >= 0 && gx < _gridCellsX && gz >= 0 && gz < _gridCellsZ)
                        {
                            // Iterate through agents only in this specific grid cell
                            foreach (Agent agent in _agentGrid[gx, gz])
                            {
                                if (agent == null) continue; // Should not happen often if grid is populated correctly

                                Vector3 agentPosXZ = new Vector3(agent.transform.position.x, 0, agent.transform.position.z);
                                float distanceSq = (agentPosXZ - samplePoint).sqrMagnitude;

                                if (distanceSq < radiusSq)
                                {
                                    float u = Mathf.Sqrt(distanceSq) / densityMeasurementRadius;
                                    float kernelValue = (3.0f / 4.0f) * (1.0f - u * u);
                                    currentDensity += kernelValue;
                                }
                            }
                        }
                    }
                }

                float normalizedDensityForGradient = Mathf.Clamp01(currentDensity / maxLosDensity);
                pixelColors[y * resolutionX + x] = losGradient.Evaluate(normalizedDensityForGradient);

            }
        }
        isFirstUpdate = false;

        _heatmapTexture.SetPixels(pixelColors);
        _heatmapTexture.Apply();
    }

    /// <summary>
    /// Called when the MonoBehaviour will be destroyed.
    /// Cleans up the coroutine and texture.
    /// </summary>
    void OnDestroy()
    {
        if (_updateCoroutine != null)
        {
            StopCoroutine(_updateCoroutine);
            _updateCoroutine = null;
        }
        if (_heatmapTexture != null)
        {
            Destroy(_heatmapTexture); // Destroy the dynamically created texture
        }
    }
}
