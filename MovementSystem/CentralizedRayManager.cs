using UnityEngine;

public class CentralizedRayManager : MonoBehaviour
{
    public float rayDistanceObj = 1.5f;
    //public Vector3 objectOffset = new Vector3(1f, 0.5f, 0.5f);
    Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2); // Use normalized screen coordinates
    public Camera playerCamera; // Made public to allow assignment from the editor


    float pivotOffsetGround = 0.8f;
    float rayDistanceVert = 0.5f; 
    Vector3 rayOrigin; Vector3 rayDirection; 
    private Transform playerTransform;


    private void Awake()
    {
        if (playerCamera == null)
        {
            Debug.Log("No player camera was assigned to CentralizedRayManager.cs script, reverting to main camera in scene.");
            playerCamera = Camera.main;
        }
        playerTransform = transform;
    }

    public Ray CheckHorizontalSurface()
    {
        Vector3 offsetPositionGround = playerTransform.position - new Vector3(0, pivotOffsetGround, 0); //transform - offset
        Vector3 verRayDirection = Vector3.down * rayDistanceVert; // -1 * ray distance. Absolute coordinates cause we don't want players to walk on walls
        Ray horRay = new Ray(offsetPositionGround, verRayDirection);
        return horRay;
    }

    public Ray CheckForObjectsRay()
    {
        Ray rayObj = playerCamera.ScreenPointToRay(screenCenter);
        return rayObj;
    }
}

