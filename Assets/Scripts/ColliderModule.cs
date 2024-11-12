using UnityEngine;

public class ColliderModule : MonoBehaviour
{
    public bool collision = false;
    public Vector3 initialPosition;

    void Awake()
    {
        initialPosition = transform.position;
    }
    void OnTriggerEnter()
    {
        collision = true;
    }
    void OnTriggerExit()
    {
        collision = false;
    }
}
