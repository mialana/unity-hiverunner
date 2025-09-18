using UnityEngine;

[ExecuteInEditMode]
public class SkyboxManager : MonoBehaviour
{
    public float skySpeed = 5f;
    public Material skybox;

    private float rotation = 0f;

    void Awake()
    {
        if (!skybox)
        {
            skybox = Resources.Load("Materials/SkyboxMat", typeof(Material)) as Material;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        rotation += Time.deltaTime * skySpeed;

        if (rotation >= 360f)
            rotation = 0f;

        RenderSettings.skybox.SetFloat("_Rotation", rotation);
    }

    void OnApplicationQuit()
    {
        RenderSettings.skybox.SetFloat("_Rotation", 0f);
    }
}
