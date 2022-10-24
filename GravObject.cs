using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravObject : MonoBehaviour
{
    public string loreName = "undefined";
    public string loreMass = "unmeasurable";
    public string loreOrigin = "unknowable";
    [SerializeField] Rigidbody rb;  // You can reference in inspector or get it however
    [SerializeField] GameObject outlineObj;
    public GameObject splashAudioPrefab;
    public bool observed;
    float force, changeInVelocity, changeInVelocityClamp, lastVel = 0;
    bool underForce;
    Vector3 startPos;
    Quaternion startRot;
    Vector3 endPos;
    Renderer rendererMat;
    Material[] dissolveMaterials;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        outlineObj.SetActive(false);
        rendererMat = outlineObj.GetComponent<Renderer>();
        startPos = transform.position;
        startRot = transform.rotation;
        List<Material> materialList = new List<Material>();
        MeshRenderer[] meshRenderer = GetComponentsInChildren<MeshRenderer>();
        if (meshRenderer.Length > 0)
        {
            foreach (MeshRenderer renderer in meshRenderer)
            {
                foreach (Material mat in renderer.materials)
                {
                    materialList.Add(mat);
                }
            }
            dissolveMaterials = materialList.ToArray();
        }
    }

    /// <summary>
    /// Respawns the object if it has fallen out of the world (below the ocean level).
    /// </summary>
    void FixedUpdate()
    {
        if(transform.position.y <= -6.5f)
        {
            Instantiate(splashAudioPrefab, transform.position, transform.rotation);
            rb.velocity = new Vector3(0, 0, 0);
            transform.rotation = startRot;
            transform.position = startPos;
            if (dissolveMaterials.Length > 0)
            {
                foreach (Material mat in dissolveMaterials)
                {
                    mat.SetFloat("DissolveAmount", 1f);
                }
            }
            StartCoroutine(IntegrateMaterials());
            return;
        }
        GravGunLogic();
    }

    /// <summary>
    /// Runs a lerp operation on a shader which makes the object looks as though it is phasing into the world.
    /// </summary>
    /// <returns></returns>
    IEnumerator IntegrateMaterials()
    {
        if (dissolveMaterials.Length > 0)
        {
            float counter = 1f;
            while (dissolveMaterials[0].GetFloat("DissolveAmount") > -.5f)
            {
                counter -= 0.075f;
                foreach (Material mat in dissolveMaterials)
                {
                    mat.SetFloat("DissolveAmount", counter);
                }
                yield return new WaitForSeconds(0.05f);
            }
        }
    }

    /// <summary>
    /// Outlines the object as required and performs rigidbody manipulations to move where the player is aiming it.
    /// </summary>
    void GravGunLogic()
    {
        if (observed && !underForce)
        {
            outlineObj.SetActive(true);
            rendererMat.material.color = Color.yellow;
        }
        else if (!observed && !underForce)
        {
            outlineObj.SetActive(false);
            rendererMat.material.color = Color.yellow;
        }
        if (!underForce || endPos == null) return;

        rendererMat.material.color = Color.green;
        float dstToTarget = Vector3.Distance(transform.position, endPos);
        Vector3 direction = Vector3.Normalize(endPos - transform.position) * (Mathf.InverseLerp(0, 4, dstToTarget) * force);
        rb.velocity = direction;
        transform.rotation = Quaternion.AngleAxis(0, Vector3.up);
        lastVel = Vector3.Magnitude(direction);
    }

    /// <summary>
    /// Sets a new target position and applied force.
    /// </summary>
    /// <param name="newTarget"></param>
    /// <param name="force"></param>
    public void NewTarget(Vector3 newTarget, float force)
    {
        endPos = newTarget;
        this.force = force;
    }

    /// <summary>
    /// Changes whether the object is under external force (from the gravity gun) or under normal gravity.
    /// </summary>
    /// <param name="newState"></param>
    public void ChangeState(bool newState)
    {
        underForce = newState;
        rb.useGravity = !newState;
    }

    /// <summary>
    /// Applies a force in a given direction.
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="strength"></param>
    public void ApplyForce(Vector3 direction, float strength)
    {
        rb.velocity = Vector3.Normalize(direction) * strength;
    }
}
