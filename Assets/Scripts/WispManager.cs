using UnityEngine;
using static UnityEngine.Rendering.GPUSort;

public class WispManager : MonoBehaviour
{
    //Collider
    [Header("Collider settings")]
    [SerializeField][Range(1, 9)] int resolution;
    [SerializeField] Vector3 bounds;
    [SerializeField] Color one, two;
    [SerializeField] GameObject colliderModule;
    [SerializeField] Transform gridParent;
    Transform[] colliderGrid;
    [HideInInspector]public bool drawGizmos;

    //Boid
    [Header("Boid settings")]
    [SerializeField] Mesh mesh;
    [SerializeField] Material material;
    [SerializeField] int numBoids;
    [SerializeField] BoidsManager boidsManager;
    [SerializeField] float minSize, maxSize;
    [SerializeField][Range(0.0f, 1.0f)] float sizeCoe, fadeCoe;
    uint[] args;

    //Boid ComputeShader
    [SerializeField] ComputeShader boidCompute;
    [SerializeField] float spinSpeed;
    [SerializeField] float oscillationSpeed;
    [SerializeField] float oscillationMagnitude;
    int kernelID;
    public struct BoidData
    {
        public Vector3 position;
        public Vector3 initialPosition;
        public float oscillationOffset;
        public float size;
    }

    public BoidData[] boids;
    ComputeBuffer argsBuffer, boidBuffer;
    void Awake()
    {
        InstantiateColliders();
        GetPositions();
    }

    void GetPositions()
    {
        boids = new BoidData[numBoids];
        for(int i = 0; i < numBoids; i++)
        {
            var positionBias = Mathf.Pow(Random.Range(0.0f, 1.0f), fadeCoe);
            boids[i].position = Random.insideUnitSphere * Mathf.Lerp(0.0f, bounds.x, positionBias) * Mathf.Pow(bounds.x, 1 / 3) / 2;
            boids[i].initialPosition = boids[i].position;
            boids[i].oscillationOffset = Random.Range(0.0f, Mathf.PI * 2f);
            var sizeBias = Mathf.Pow(Random.Range(0.0f, 1.0f), sizeCoe);
            boids[i].size = Mathf.Lerp(maxSize, minSize, sizeBias);
        }
        boidBuffer = new ComputeBuffer(numBoids, sizeof(float) * (3 + 3 + 1 + 1), ComputeBufferType.IndirectArguments);
        boidBuffer.SetData(boids);

        kernelID = boidCompute.FindKernel("BoidPositions");
        boidCompute.SetBuffer(0, "_BoidBuffer", boidBuffer);
        material.SetBuffer("_BoidBuffer", boidBuffer);
    }
    void Update()
    {
        RenderMesh(mesh, material, bounds, numBoids);
        UpdateShader();
    }
    void InstantiateColliders()
    {
        colliderGrid = new Transform[(int)Mathf.Pow(resolution, 3)];
        int id = 0;

        for(int i = 0; i < resolution; i++)
        {
            for(int j = 0; j < resolution; j++)
            {
                for(int k = 0; k < resolution; k++)
                {
                    GameObject tmp = Instantiate(colliderModule, transform.position - bounds / 2 + bounds / resolution / 2 + new Vector3(i * bounds.x / resolution, j * bounds.y / resolution, k * bounds.z / resolution), transform.rotation, gridParent);
                    colliderGrid[id] = tmp.transform;
                    tmp.GetComponent<BoxCollider>().size = bounds / resolution;
                    id++;
                }
            }
        }
    }

    public void RenderMesh(Mesh mesh, Material material, Vector3 vector, int numBoids)
    {
        if (argsBuffer == null)
        {
            args = GetArgsBuffer(mesh);
        }
        argsBuffer.SetData(args);
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(Vector3.zero, Vector3.one * 1000), argsBuffer);
    }

    uint[] GetArgsBuffer(Mesh mesh)
    {
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

        args = new uint[5] { 0, 0, 0, 0, 0 };

        args[0] = mesh.GetIndexCount(0);
        args[1] = (uint)numBoids;
        args[2] = mesh.GetIndexStart(0);
        args[3] = mesh.GetBaseVertex(0);
        args[4] = 0;

        return args;
    }

    void UpdateShader()
    {
        boidCompute.SetVector("_CorePos", transform.position);
        boidCompute.SetFloat("_SpinSpeed", spinSpeed);
        boidCompute.SetFloat("_OscillationSpeed", oscillationSpeed);
        boidCompute.SetFloat("_OscillationMagnitude", oscillationMagnitude);
        
        boidCompute.Dispatch(kernelID, 1024, 1, 1);
    }

    void OnDisable()
    {
        argsBuffer.Release();
        argsBuffer = null;

        boidBuffer.Release();
        boidBuffer = null;
    }

    public void UpdateBoidSettings()
    {
        GetPositions();
    }

    //Debug tools
    void OnDrawGizmos()
    {
        if (colliderGrid != null && drawGizmos)
        {
            Gizmos.color = one;
            for (int i = 0; i < colliderGrid.Length; i++)
            {
                if (colliderGrid[i].GetComponent<ColliderModule>().collision)
                {
                    Gizmos.color = two;
                }
                else
                {
                    Gizmos.color = one;
                }

                Gizmos.DrawCube(colliderGrid[i].position, colliderGrid[i].GetComponent<BoxCollider>().size);
            }
        }
    }
    public void DrawGizmos()
    {
        drawGizmos = !drawGizmos;
    }
}
