using UnityEngine;

public class ColliderModule : MonoBehaviour
{
    public bool collision = false;
    void OnTriggerEnter()
    {
        collision = true;
    }
    void OnTriggerExit()
    {
        collision = false;
    }
}
