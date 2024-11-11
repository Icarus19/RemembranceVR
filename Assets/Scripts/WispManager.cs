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

    //BoidTrail
    [Header("Trail settings")]
    [SerializeField] Mesh meshTrail;
    [SerializeField] Material materialTrail;
    [SerializeField] int numTrails;


    //Boid ComputeShader
    [Header("Compute Shader")]
    [SerializeField] ComputeShader boidCompute;
    [SerializeField] float spinSpeed;
    [SerializeField] float oscillationSpeed;
    [SerializeField] float oscillationMagnitude;
    float avoidDst;
    [SerializeField] float avoidStrength;
    [SerializeField] float avoidLerpSpeed;
    Vector3 avoidCoords;
    bool collisionDetected;
    int kernelID;
    public struct BoidData
    {
        public Vector3 position;
        public Vector3 initialPosition;
        public float oscillationOffset;
        public float size;
    }
    public struct PosData
    {
        public Vector3 CollisionPos;
        public Vector3 DefaultPos;
    }

    public BoidData[] boids;
    ComputeBuffer argsBuffer, boidBuffer, trailBuffer, prevPosBuffer;
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

        prevPosBuffer = new ComputeBuffer(numBoids, sizeof(float) * (3 + 3), ComputeBufferType.Default);

        kernelID = boidCompute.FindKernel("BoidPositions");
        boidCompute.SetBuffer(kernelID, "_BoidBuffer", boidBuffer);
        boidCompute.SetBuffer(kernelID, "_PreviousPositionBuffer", prevPosBuffer);
        boidCompute.SetFloat("_AvoidDistance", bounds.x / resolution);
        //This has to be set as initial position or the object moves twice with each translation
        boidCompute.SetVector("_CorePos", transform.position);
        material.SetBuffer("_BoidBuffer", boidBuffer);
        //Testing zone
        materialTrail.SetBuffer("_BoidBuffer", boidBuffer);
        //End of test
    }
    void Update()
    {
        RenderMesh(mesh, material, bounds, numBoids);
        UpdateShader();
        //DrawBounds(new Bounds(transform.position, Vector3.one * bounds.x));
        //DrawBounds(new Bounds(transform.position, Vector3.one * bounds.x));
    }
    void DrawBounds(Bounds b, float delay = 0)
    {
        // bottom
        var p1 = new Vector3(b.min.x, b.min.y, b.min.z);
        var p2 = new Vector3(b.max.x, b.min.y, b.min.z);
        var p3 = new Vector3(b.max.x, b.min.y, b.max.z);
        var p4 = new Vector3(b.min.x, b.min.y, b.max.z);

        Debug.DrawLine(p1, p2, Color.blue, delay);
        Debug.DrawLine(p2, p3, Color.red, delay);
        Debug.DrawLine(p3, p4, Color.yellow, delay);
        Debug.DrawLine(p4, p1, Color.magenta, delay);

        // top
        var p5 = new Vector3(b.min.x, b.max.y, b.min.z);
        var p6 = new Vector3(b.max.x, b.max.y, b.min.z);
        var p7 = new Vector3(b.max.x, b.max.y, b.max.z);
        var p8 = new Vector3(b.min.x, b.max.y, b.max.z);

        Debug.DrawLine(p5, p6, Color.blue, delay);
        Debug.DrawLine(p6, p7, Color.red, delay);
        Debug.DrawLine(p7, p8, Color.yellow, delay);
        Debug.DrawLine(p8, p5, Color.magenta, delay);

        // sides
        Debug.DrawLine(p1, p5, Color.white, delay);
        Debug.DrawLine(p2, p6, Color.gray, delay);
        Debug.DrawLine(p3, p7, Color.green, delay);
        Debug.DrawLine(p4, p8, Color.cyan, delay);
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
            args = GetArgsBuffer(mesh, (uint)numBoids);
        }
        argsBuffer.SetData(args);
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(transform.position, Vector3.one * bounds.x), argsBuffer);
        //Testing stuff here now
        if (trailBuffer == null)
        {
            args = GetArgsBuffer(mesh, (uint)(numBoids * numTrails));
        }
        trailBuffer.SetData(args);
        Graphics.DrawMeshInstancedIndirect(meshTrail, 0, materialTrail, new Bounds(transform.position, Vector3.one * bounds.x), trailBuffer);
        //End of testing here
    }

    uint[] GetArgsBuffer(Mesh mesh, uint num)
    {
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        //Experiment
        trailBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        //end

        args = new uint[5] { 0, 0, 0, 0, 0 };

        args[0] = mesh.GetIndexCount(0);
        args[1] = (uint)num;
        args[2] = mesh.GetIndexStart(0);
        args[3] = mesh.GetBaseVertex(0);
        args[4] = 0;

        return args;
    }

    void UpdateShader()
    {
        var prevAvoidCoords = avoidCoords;
        avoidCoords = GetColliderPosition();
        boidCompute.SetVector("_AvoidPosition", avoidCoords);
        boidCompute.SetVector("_PreviousAvoidPosition", prevAvoidCoords);
        boidCompute.SetBool("_CollisionDetected", collisionDetected);

        boidCompute.SetFloat("_AvoidStrength", avoidStrength);
        boidCompute.SetFloat("_AvoidLerpSpeed", avoidLerpSpeed);
        boidCompute.SetFloat("_SpinSpeed", spinSpeed);
        boidCompute.SetFloat("_OscillationSpeed", oscillationSpeed);
        boidCompute.SetFloat("_OscillationMagnitude", oscillationMagnitude);
        
        boidCompute.Dispatch(kernelID, 1024, 1, 1);
    }
    Vector3 GetColliderPosition()
    {
        for(int i = 0; i < colliderGrid.Length; i++)
        {
            if (colliderGrid[i].GetComponent<ColliderModule>().collision)
            {
                collisionDetected = true;
                return colliderGrid[i].transform.position;
            }
        }
        collisionDetected = false;
        return transform.position;
    }

    void OnDisable()
    {
        argsBuffer.Release();
        argsBuffer = null;

        boidBuffer.Release();
        boidBuffer = null;

        prevPosBuffer.Release();
        prevPosBuffer = null;

        //Testing zone
        trailBuffer.Release();
        trailBuffer = null;
        //end of test
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
