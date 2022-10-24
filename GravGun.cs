using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GravGun : MonoBehaviour
{
    public TMP_Text hudName, hudMass, hudOrigin;
    public Animator animator;
    public Image reticule;
    public float force = 1;
    public LayerMask obstacleLayers;
    public Transform rayArcMidpoint, rayArcEndpoint;
    public GameObject rayArcObject;
    public AudioSource fireAudio, rayAudio, rayEndAudio;

    public GameObject attachedObj;
    public GravObject attachedObjController;
    GameObject mainCamera;
    Camera cam;
    PlayerInputs input;
    bool holdingObj;
    GameObject lastObservedObj;

    public float desiredDistance;

    void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main.gameObject;
            cam = mainCamera.GetComponent<Camera>();
        }
        input = GetComponent<PlayerInputs>();
        rayArcObject.SetActive(false);
    }

    /// <summary>
    /// Lots of frame-by-frame logic
    /// </summary>
    void Update()
    {
        if (GameController.instance.paused)
        {
            input.primaryfire = false;
            input.secondary = false;
            return;
        }

        // Detect whether the player is looking at an object, and inform any object which is being looked at.
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 40, LayerMask.GetMask("GravObject")))
        {
            if(lastObservedObj != hit.collider.gameObject && lastObservedObj != null)
            {
                lastObservedObj.GetComponent<GravObject>().observed = false;
                attachedObjController = hit.collider.GetComponent<GravObject>();
            }
            lastObservedObj = hit.collider.gameObject;
            hit.collider.GetComponent<GravObject>().observed = true;
            
        }
        else if(lastObservedObj != null)
        {
            
            lastObservedObj.GetComponent<GravObject>().observed = false;
            lastObservedObj = null;
        }
        // If the player is holding an object, determine the arc end and mid points from the object to the player to display the beam effect.
        if (holdingObj)
        {
            rayArcEndpoint.position = attachedObj.transform.position;
            rayArcMidpoint.position = cam.ViewportToWorldPoint(new Vector3(.5f, .5f, Vector3.Distance(transform.position, attachedObj.transform.position) / 4));
            Vector3 screenPoint = cam.WorldToViewportPoint(attachedObj.transform.position);
            bool onScreen = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
            // Disconnect the object if the player cannot see it any more
            if (Physics.Linecast(mainCamera.transform.position, attachedObj.transform.position, obstacleLayers) || !onScreen)
            {
                print("broken due to not on screen");
                DisconnectObject();
                input.primaryfire = false;
            }
            // Allow the player to modify the distance of the target position for the object
            if (input.scroll.y != 0)
            {
                float scrollInput = Mathf.Clamp(input.scroll.y, -1, 1);
                float scrollMultiplier = Mathf.InverseLerp(0, 10, Vector3.Distance(mainCamera.transform.position, attachedObj.transform.position));
                scrollMultiplier = Mathf.Clamp(scrollMultiplier, .1f, 2);
                float dst = desiredDistance += scrollInput * scrollMultiplier;
                desiredDistance = Mathf.Clamp(dst, .5f, 40);
            }
            if(input.primaryfire)
            {
                DisconnectObject();
                input.primaryfire = false;
            }
        }
        if(holdingObj && attachedObj && attachedObjController)
        {
            attachedObjController.ChangeState(true);
            attachedObjController.NewTarget(cam.ViewportToWorldPoint(new Vector3(.5f, .5f, desiredDistance)), force);
        }
        // Grab an object the player is looking at if possible, on left click
        if(DetectObject() && !holdingObj)
        {
            if(input.primaryfire)
            {
                
                TimeManager.instance.taggedObj = attachedObj;
                TimeManager.instance.HandleTaggedObjParent(true);
                fireAudio.Play();
                rayAudio.Play();
                animator.ResetTrigger("Fire");
                animator.SetTrigger("Fire");
                rayArcObject.SetActive(true);
                holdingObj = true;
                input.primaryfire = false;
                desiredDistance = Vector3.Distance(mainCamera.transform.position, attachedObj.transform.position);
                hudName.text = attachedObjController.loreName;
                hudMass.text = attachedObjController.loreMass;
                hudOrigin.text = attachedObjController.loreOrigin;
            }
        }
        if(holdingObj && input.aim)
        {
            DisconnectObject();
            input.aim = false;
        }
        input.primaryfire = false;
        input.aim = false;
    }

    /// <summary>
    /// Performs all functionality to disconnect an object; stop playing sounds, stop animations, etc.
    /// </summary>
    void DisconnectObject()
    {
        TimeManager.instance.HandleTaggedObjParent(false);
        TimeManager.instance.taggedObj = null;
        rayAudio.Stop();
        rayEndAudio.Play();
        rayArcObject.SetActive(false);
        attachedObjController.ChangeState(false);
        holdingObj = false;
        desiredDistance = 0;
        hudName.text = "null";
        hudMass.text = "null";
        hudOrigin.text = "null";
    }

    /// <summary>
    /// Detects if the player is looking at an object in order to change the reticule colour and prepare for grabbing the object.
    /// </summary>
    /// <returns></returns>
    bool DetectObject()
    {
        if (holdingObj) return false;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 40, LayerMask.GetMask("GravObject")))
        {
            reticule.color = new Color(224, 178, 0);
            attachedObj = hit.collider.gameObject;
            if (attachedObjController == null) attachedObjController = attachedObj.GetComponent<GravObject>();
            return true;
        }
        reticule.color = Color.white;
        attachedObj = null;
        attachedObjController = null;
        return false;
    }

    /// <summary>
    /// Debugging the target position within the editor.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (desiredDistance <= 0) return;

        Gizmos.color = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        
        Gizmos.DrawSphere(cam.ViewportToWorldPoint(new Vector3(.5f, .5f, desiredDistance)), .5f);
    }
}
