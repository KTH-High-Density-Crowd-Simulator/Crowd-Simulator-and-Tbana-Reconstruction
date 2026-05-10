using UnityEngine;
using UnityEditor;
using System.IO;

public class ImpostorManager : MonoBehaviour {

	private Renderer renderer;
	private Camera captureCamera;

	private GameObject impostorQuad;
    private GameObject LODGroup;
	
	private Transform cameraTransform;
	private Transform targetTransform;

	private Material impostorMaterial;

    private float distance;

	[Header("Settings")]
	public int resolution = 1024;
	public int frames = 16;
	private int columns, rows;

	[Header("References")]
	public Material impostorNormalMaterial;
	public Material impostorMaterialPrefab;
    public GameObject impostorQuadPrefab;
	public GameObject captureCameraPrefab;
    public GameObject LODGroupPrefab;

	[ContextMenu("Bake and Setup Impostor")]
    void Start() {
		GameObject cameraObject = Instantiate(this.captureCameraPrefab);

        this.LODGroup = Instantiate(this.LODGroupPrefab);
        transform.SetParent(this.LODGroup.transform);

		this.impostorMaterial = Instantiate(this.impostorMaterialPrefab);

		this.captureCamera = cameraObject.GetComponent<Camera>();
		this.cameraTransform = cameraObject.GetComponent<Transform>();

		if (cameraObject == null || this.captureCamera == null || this.cameraTransform == null) {
			Debug.Log("ImpostorManager is not setup correctly!?");
		}
        Renderer renderer = GetComponentInChildren<Renderer>();
		this.BakeModel(renderer);
		DestroyImmediate(cameraObject);

		this.impostorQuad = Instantiate(this.impostorQuadPrefab, this.LODGroup.transform);

        //Adjusts quad height to source objects height        
        this.MatchQuadHeight(renderer, ref impostorQuad);

		// this.impostorQuad = Instantiate(this.impostorQuadPrefab);
		this.renderer = this.impostorQuad.GetComponent<Renderer>();
		this.renderer.material = this.impostorMaterial;

		this.targetTransform = this.impostorQuad.GetComponent<Transform>();


		this.cameraTransform = Camera.main.GetComponent<Transform>();
		this.columns = this.rows = Mathf.FloorToInt(Mathf.Sqrt(frames));

        UnityEngine.LOD[] lods = this.LODGroup.GetComponent<LODGroup>().GetLODs();
        lods[0].renderers = GetComponentsInChildren<Renderer>();
        lods[1].renderers = new Renderer[] { this.renderer };

        this.LODGroup.GetComponent<LODGroup>().SetLODs(lods);
	}

    void Update() {
		if (targetTransform == null) {
			return;
		}

        Vector3 dir = cameraTransform.position - targetTransform.position;
        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

        if (angle < 0) {
            angle += 360;
		}

        int frame = Mathf.FloorToInt(angle / (360f / frames)) % frames;

        int column = frame % columns;
        int row = rows + 5 - frame / columns;

        Vector2 scale = new Vector2(1f / columns, 1f / rows);
        Vector2 offset = new Vector2(column * scale.x, row * scale.y);

        this.renderer.material.SetTextureScale("_MainTex", scale);
        this.renderer.material.SetTextureOffset("_MainTex", offset);
        this.renderer.material.SetTextureScale("_NormalTex", scale);
        this.renderer.material.SetTextureOffset("_NormalTex", offset);

        Vector3 lookAwayDir = this.targetTransform.position - this.cameraTransform.position;
        lookAwayDir.y = 0;

        if (lookAwayDir.sqrMagnitude > 0.001f) {
            this.targetTransform.rotation = Quaternion.LookRotation(lookAwayDir);
        }
	}


    public void BakeModel(Renderer renderer) {
        if (renderer == null){
            Debug.Log("renderer is null");
        }
        if (renderer.materials.Length == 0) {
            Debug.Log("[ImposterCapture] Unable to find any materials on mesh");
        }

        Vector3 centerPos = renderer.bounds.center;
        float radius = renderer.bounds.extents.magnitude;

        this.distance = radius + this.captureCamera.nearClipPlane + 0.01f;
        Debug.Log("Distance: " + this.distance);

        this.captureCamera.orthographic = true;
        this.captureCamera.orthographicSize = radius * 1.05f; // Add just a little bit

        Texture2D colorAtlas = BakeAtlas(centerPos, false);

        Material[] normalMaterials = new Material[renderer.materials.Length];
        for (int i = 0; i < normalMaterials.Length; ++i) normalMaterials[i] = impostorNormalMaterial;

        Material[] realMaterials = renderer.materials;
        renderer.materials = normalMaterials;

        Texture2D normalAtlas = BakeAtlas(centerPos, true);
        renderer.materials = realMaterials;

		this.impostorMaterial.SetTexture("_MainTex", colorAtlas);
		this.impostorMaterial.SetTexture("_NormalTex", normalAtlas);

        Debug.Log("Atli created!");
        RenderTexture.active = null;
    }

	// This doesn't need to be recreated every time, we can also store them on disk and let unity handle it (but remember to set the normal map texture to texturetype NormalMap)
    private Texture2D BakeAtlas(Vector3 centerPos, bool isNormalMap) {
        int columns = Mathf.CeilToInt(Mathf.Sqrt(frames));
        int rows = Mathf.CeilToInt((float)frames / columns);

        RenderTexture rt = new RenderTexture(resolution, resolution, 24);
        captureCamera.targetTexture = rt;

        Texture2D atlas = new Texture2D(resolution * columns, resolution * rows, TextureFormat.RGBA32, false, isNormalMap);

        for (int i = 0; i < frames; i++) {
            float angle = (360f / frames) * i;

            Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.back;

            captureCamera.transform.position = centerPos + dir * distance;
            captureCamera.transform.LookAt(centerPos);
            captureCamera.Render();

            Texture2D frame = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, isNormalMap);

            RenderTexture.active = rt;

            frame.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            frame.Apply();

            int column = i % columns;
            int row = i / columns;

            atlas.SetPixels(column * resolution, (rows - 1 - row) * resolution, resolution, resolution, frame.GetPixels());
            DestroyImmediate(frame);
        }

        atlas.Apply();

		RenderTexture.active = null;
		captureCamera.targetTexture = null;
		DestroyImmediate(rt);

		return atlas;
    }

    private Vector3 GetTargetCenter(GameObject obj) {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) {
            return obj.transform.position;
        }

        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers) {
            bounds.Encapsulate(r.bounds);
        }

        return bounds.center;
    }


    private void MatchQuadHeight(Renderer sourceRenderer, ref GameObject impostorQuad){
        Renderer impostorQuadRenderer = impostorQuad.GetComponentInChildren<Renderer>();
        if(sourceRenderer == null){
            Debug.Log("renderer is null");
        }
        if(impostorQuad == null){
            Debug.Log("impostorQuad is null");
        }

        Bounds sourceBounds = sourceRenderer.bounds;
        Vector3 sourceObjectCenter = sourceRenderer.bounds.center;
        Vector3 impostorQuadCenter = impostorQuadRenderer.bounds.center;
        Vector3 diff = sourceObjectCenter - impostorQuadCenter;

        float newHeight = sourceBounds.size.y * distance;

        impostorQuad.transform.localScale = new Vector3(newHeight,newHeight, 1f);
        impostorQuad.transform.position += diff;
    }
}
