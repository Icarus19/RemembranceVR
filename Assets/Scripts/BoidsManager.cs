using UnityEngine;

public class BoidsManager : MonoBehaviour
{
    public struct Boid
    {
        public Vector3 position;
        public Vector3 forward;
        public float size;
    }

    public Boid[] boids;
    ComputeBuffer argsBuffer;
    
    public void Instantiate(Mesh mesh, Material material, Vector3 vector, int numBoids)
    {
        if(argsBuffer == null)
        {
            argsBuffer.SetData(GetArgsBuffer(mesh));
        }

        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(Vector3.zero, Vector3.one * 1000), argsBuffer);
    }

    uint[] GetArgsBuffer(Mesh mesh)
    {
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

        args[0] = mesh.GetIndexCount(0);
        args[1] = 10000;
        args[2] = mesh.GetIndexStart(0);
        args[3] = mesh.GetBaseVertex(0);
        args[4] = 0;

        return args;
    }

    void OnDisable()
    {
        argsBuffer.Release();
        argsBuffer = null;
    }
}
