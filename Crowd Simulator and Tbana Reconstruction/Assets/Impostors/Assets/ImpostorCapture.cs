using UnityEngine;
using System.IO;

public class ImpostorCapture : MonoBehaviour
{
    public Camera captureCamera;
    public GameObject target;

    public int frames = 16;
    public int resolution = 1024;
    public float distance = 5f;

    int columns;
    int rows;

    void Start()
    {
        target.SetActive(true);

        Vector3 centerPos = GetTargetCenter(target);

        Debug.Log("Pos: ");
        Debug.Log(centerPos);

        columns = Mathf.CeilToInt(Mathf.Sqrt(frames));
        rows = Mathf.CeilToInt((float)frames / columns);

        RenderTexture rt = new RenderTexture(resolution, resolution, 24);
        captureCamera.targetTexture = rt;

        Texture2D atlas = new Texture2D(
            resolution * columns,
            resolution * rows,
            TextureFormat.RGBA32,
            false
        );

        for (int i = 0; i < frames; i++) {
            float angle = (360f / frames) * i;

            Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.back;

            captureCamera.transform.position = centerPos + dir * distance;
            captureCamera.transform.LookAt(centerPos);
            captureCamera.Render();

            Texture2D frame = new Texture2D(
                resolution,
                resolution,
                TextureFormat.RGBA32,
                false
            );

            RenderTexture.active = rt;

            frame.ReadPixels(
                new Rect(0,0,resolution,resolution),
                0,
                0
            );

            frame.Apply();

            int column = i % columns;
            int row = i / columns;

            atlas.SetPixels(
                column * resolution,
                (rows - 1 - row) * resolution,
                resolution,
                resolution,
                frame.GetPixels()
            );
        }

        atlas.Apply();

        byte[] bytes = atlas.EncodeToPNG();

        File.WriteAllBytes(
            Application.dataPath + "/ImpostorAtlas.png",
            bytes
        );

        Debug.Log("Atlas created!");

        RenderTexture.active = null;

        target.SetActive(false);
    }

    private Vector3 GetTargetCenter(GameObject obj) {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0) {
            Debug.Log("[ImpostorCapture]: Unable to find renderers of model object");
            return obj.transform.position;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; ++i) {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds.center;
    }
}
