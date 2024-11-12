using UnityEngine;

public class WispMovement : MonoBehaviour
{
    [SerializeField] float idleHeight = 1f;
    [SerializeField] float pingPongSpeed = 0.5f;
    Vector3 newPosition;
    void Start()
    {
        newPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(transform.position.x, newPosition.y + Mathf.PingPong(Time.time, idleHeight) * pingPongSpeed, transform.position.z);
        
        
    }
}
