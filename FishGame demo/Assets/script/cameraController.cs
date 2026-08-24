using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraController : MonoBehaviour
{
   
    public Transform player;
    public float smoothSpeed = 0.125f;
    [SerializeField] private float minX, maxX;
    [SerializeField] private float minY, maxY;

    private void Start()
    {
        if (player != null)
        {
            transform.position = new Vector3(player.position.x, player.position.y, transform.position.z);
        }
    }
    private void LateUpdate()
    {
        if (player != null)
        {
            Vector3 desiredPosition = new Vector3(player.position.x, player.position.y, transform.position.z);

            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

            float clampedX = Mathf.Clamp(smoothedPosition.x, minX, maxX);
            float clampedY = Mathf.Clamp(smoothedPosition.y, minY, maxY);
             smoothedPosition = new Vector3(clampedX, clampedY, transform.position.z);

            transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, transform.position.z);
        }
    }
}

