using UnityEngine;
using UnityEngine.InputSystem;

public class PickupObject : MonoBehaviour
{
    public Camera playerCamera;
    //private float rayDistance = 1.5f;
    Vector3 objectOffset = new Vector3(0f, 0f, 1f);
    Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
    [SerializeField] float throwStrenght = 5f;
    private float cooldownTimer; private float cooldownTimerDuration = 0.5f;
    private float rotationSpeed = 2000f;
    
    private PlayerInput playerInput; 
    private InputAction interactAction; private InputAction throwAction; private InputAction consumeAction; private InputAction rotateAction; private InputAction modAction;
    private GameObject pickedObject; private Rigidbody pickedObjectRigidbody; private Collider pickedObjectCollider;
    private bool isHoldingObject = false; private bool canPickObject = false; private bool isModifier = false;
    public ConsumableItem consumableItem;
    private CentralizedRayManager rayManager;


    void Awake()
    {
        if (playerCamera == null)
        {
            Debug.Log("No player camera was assigned to PickupObject.cs script, reverting to main camera in scene.");
            playerCamera = Camera.main;
        }
        playerInput = GetComponent<PlayerInput>();
        rayManager = GetComponent<CentralizedRayManager>();
        interactAction = playerInput.actions["ObjectInteract"]; throwAction = playerInput.actions["ThrowObject"]; consumeAction = playerInput.actions["UseObject"];
        rotateAction = playerInput.actions["RotateObjectInHand"]; modAction = playerInput.actions["Modifier"];
    }

    private void OnEnable()
    {
        interactAction.Enable(); throwAction.Enable(); consumeAction.Enable(); rotateAction.Enable(); modAction.Enable();
    }

    private void OnDisable()
    {
        interactAction.Disable(); throwAction.Disable(); consumeAction.Disable(); rotateAction.Disable(); modAction.Disable();
    }

    void Update()
    {
        ManageObjectInputs();

        if(cooldownTimer > 0f) { cooldownTimer -= Time.deltaTime; } //cooldown timer makes it so we can switch between picking up objects and dropping them
    }

    void FixedUpdate()
    {
        if(!isHoldingObject) //if we're not holding anything, we begin casting rays so we save performance
        {
            RayCheckForObjects();
        }
    }

    void RayCheckForObjects() //we cast a ray horizontally from the center of the screen so we can check for objects to pick up
    {
        //Ray rayObj = playerCamera.ScreenPointToRay(screenCenter);
        Ray rayObj = rayManager.CheckForObjectsRay();
        //Debug.DrawRay(rayObj.origin, rayObj.direction * 10, Color.red);
        if(Physics.Raycast(rayObj, out RaycastHit hit, rayManager.rayDistanceObj))
        {
            if(hit.collider.CompareTag("PickableObject"))
            {
                canPickObject = true;
                pickedObject = hit.collider.gameObject;

                try { pickedObjectCollider = pickedObject.GetComponent<Collider>(); }
                catch(System.NullReferenceException) { Debug.Log("No collider component assigned to object."); }
                
                try { pickedObjectRigidbody = pickedObject.GetComponent<Rigidbody>(); }//get rigid body component from picked object
                catch(System.NullReferenceException) { Debug.Log("No Rigidbody component assigned to object. Won't be possible to drop or throw."); }
                
                try { consumableItem = pickedObject.GetComponent<ConsumableItemReference>().consumableItem; } // Get the consumable item scriptable object
                catch(System.NullReferenceException) { Debug.Log("No ConsumableItemReference assigned to object, thus it is not consumable."); consumableItem = null; }
            }
            else { canPickObject = false; pickedObject = null; pickedObjectRigidbody = null; pickedObjectCollider = null; consumableItem = null; } //
        } 
    }


    void ManageObjectInputs() //we check for input and conditions to switch between states.
    {
        if(interactAction.ReadValue<float>() > 0f)
        {
            if(canPickObject && cooldownTimer <= 0f)
            {
                PickObject();
            }
            else if (isHoldingObject && cooldownTimer <= 0f)
            {
                DropObject();
            }
        }
        if(throwAction.ReadValue<float>() > 0f && isHoldingObject)
        {
            ThrowObject();
        }
        if(consumeAction.ReadValue<float>() > 0f && isHoldingObject)
        {
            UseObject();
        }
        if(rotateAction.ReadValue<float>() != 0f && isHoldingObject)
        {
            RotateObject();
        }
    }


    void PickObject()
    {
        if(canPickObject && !isHoldingObject) //if it's pick-able and we're not holding anything.
        {
            //Debug.Log("Picked up object " + pickedObject.gameObject.name);
            if(pickedObject != null && pickedObjectRigidbody != null) //if the references are not null
            {
                pickedObject.transform.position = transform.position + transform.forward * objectOffset.z + transform.up * objectOffset.y; //apply player transform + offset
                pickedObject.transform.parent = transform; //cache transform

                pickedObjectRigidbody.useGravity = false; //suspend gravity
                pickedObjectRigidbody.isKinematic = true; //kinematic
                pickedObjectCollider.enabled = false;
                
                isHoldingObject = true; canPickObject = false;
            }
            else { Debug.Log("Empty pickedObject reference"); isHoldingObject = false;}

            cooldownTimer = cooldownTimerDuration;
        }
    }

    void ThrowObject()
    {
        if(pickedObject != null && pickedObjectRigidbody != null) //if we have an object in hand with a rigidbody
        {
            Debug.Log("Throwing Object. IsHolding: " + isHoldingObject + " canPick: " + canPickObject + " Object: " + pickedObject + " Rigidbody: " + pickedObjectRigidbody);
            pickedObjectRigidbody.useGravity = true;
            pickedObjectRigidbody.isKinematic = false;
            pickedObjectCollider.enabled = true;
            pickedObjectRigidbody.AddForce(playerCamera.transform.forward * throwStrenght, ForceMode.Impulse); //add throwing force
            NullifyPickedObjectReference(); //and reset the transform
        }
    }

    void DropObject()
    {
        if(pickedObject != null && pickedObjectRigidbody != null) //if we have an object in hand with a rigidbody
        {
            Debug.Log("Dropping Object. IsHolding: " + isHoldingObject + " canPick: " + canPickObject + " Object: " + pickedObject + " Rigidbody: " + pickedObjectRigidbody);
            pickedObjectRigidbody.useGravity = true; //we just enable gravity
            pickedObjectRigidbody.isKinematic = false;
            pickedObjectCollider.enabled = true;
            NullifyPickedObjectReference(); //and reset the transform
        }
        cooldownTimer = cooldownTimerDuration;
    }

    void UseObject()
    {
        if(consumableItem != null)
        {
            Debug.Log("Used Item " + pickedObject.name);
            Destroy(pickedObject);
            NullifyPickedObjectReference();
        }
        else { Debug.Log("Not a consumable."); }
    }

    void RotateObject()
    {
        if(pickedObject != null)
        {
            isModifier = modAction.ReadValue<float>() > 0f; //bool to check modifier key
            float rotationInput = rotateAction.ReadValue<float>(); //1d pos-neg axis

            Vector3 rotAxis = isModifier ? Vector3.right : Vector3.forward; //if positive right, if negative forward.
            float rotAmount = rotationSpeed * rotationInput * Time.deltaTime; 
            
            pickedObject.transform.Rotate(rotAxis, rotAmount, Space.Self); //actual rotation. 

            Debug.Log("Rotating with input " + rotationInput + " Modifier: " + isModifier);
        }

    }

    void NullifyPickedObjectReference() //standard function to reset object reference so we're not caching old objects and we're resetting all pertinent states
    {
        pickedObject.transform.parent = null;
        pickedObject = null;
        pickedObjectCollider = null;
        pickedObjectRigidbody = null;
        isHoldingObject = false;
        consumableItem = null;
    }

}
