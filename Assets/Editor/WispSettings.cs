using UnityEngine;

[CreateAssetMenu(fileName = "WispSettings", menuName = "ScriptableObjects/WispSettings", order = 1)]
public class WispSettings : ScriptableObject
{
    //Collider
    [Header("Collider settings")]
    [Range(1, 9)] public int resolution;
    public Vector3 bounds;
    public Color one, two;
    public GameObject colliderModule;
    public Transform gridParent;

    //Boid
    [Header("Boid settings")]
    public Mesh mesh;
    public Material material;
    public int numBoids;
    [Tooltip("Scale cannot be changed during runtime, and these will scale with the localscale of the wisp")] public float minSize, maxSize;
    [Range(0.0f, 1.0f)] public float sizeCoe, fadeCoe;

    //BoidTrail
    [Header("Trail settings")]
    public Mesh meshTrail;
    public Material materialTrail;
    public int numTrails;


    //Boid ComputeShader
    [Header("Compute Shader")]
    public ComputeShader boidCompute;
    public float spinSpeed;
    public float oscillationSpeed;
    public float oscillationMagnitude;
    public float avoidStrength;
    public float avoidLerpSpeed;
}
