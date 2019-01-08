﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BaseInteractable : MonoBehaviour {

    public enum InteractableType { PRESS, TOGGLE, HOLD }

    [System.Serializable]
    public class InteractableEvent : UnityEvent<GameObject> { }

    public bool startActive = false;

    [Header("Appearance")]
    public string interactableName = "Placeholder";
    public string tooltip = "";
    public Color textColor = Color.white;
    public float textMeshFadeMultiplier = 4;
    public bool textTowardsCamera = false;
    public Material idleMaterial;
    public Material hoveredMaterial;
    public Material pressedMaterial;

    [Header("Interaction")]
    public bool touchActivates;
    public InteractableType interactableType;
    public InteractableEvent onInteractStart;
    public InteractableEvent onInteractEnd;

    public bool pressed {get; private set;}

    protected GameObject textMeshObject;
    protected TextMesh textMesh;
    protected MeshRenderer meshRenderer;
    protected MeshFilter meshFilter;
    private float lastActive = 0;
    private float textMeshAlpha = 0;
    private bool checkedStartActive = false;
    private float textMeshDistance = 0;
    private Vector3 originalTextPosition;
    private float lastCollisionTrigger = 0;

    protected virtual void Start()
    {
        pressed = false;
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        Transform textMeshTransform = transform.Find("TextMesh");
        if (textMeshTransform)
        {
            textMeshObject = textMeshTransform.gameObject;
            textMesh = textMeshObject.GetComponent<TextMesh>();
            textMeshDistance = Vector3.Distance(transform.position, textMeshObject.transform.position);

            originalTextPosition = textMeshObject.transform.position - transform.position;
        }

        int layer = LayerMask.NameToLayer("Interactable");
        gameObject.layer = layer;
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.layer = layer;
        }
    }

    protected virtual void Update()
    {
        if (!checkedStartActive)
        {
            checkedStartActive = true;
            if (startActive)
            {
                DoInteractStart(null);
            }
        }

        if (textMeshObject)
        {
            if (textTowardsCamera)
            {
                textMeshObject.transform.position = transform.position + originalTextPosition + ((Camera.main.gameObject.transform.position - transform.position).normalized);
            }
            else
            {
                textMeshObject.transform.position = transform.position + (Vector3.up * textMeshDistance);
            }
        }

        bool hovered = IsHovered();

        if(hovered && Input.GetMouseButtonDown(0))
        {
            switch (interactableType) {
                case InteractableType.PRESS:
                    DoInteractStart(GameManager.instance.playerObject);
                    DoInteractEnd(GameManager.instance.playerObject);
                    break;
                case InteractableType.TOGGLE:
                    if (pressed)
                    {
                        DoInteractEnd(GameManager.instance.playerObject);
                    }
                    else
                    {
                        DoInteractStart(GameManager.instance.playerObject);
                    }

                    break;
                case InteractableType.HOLD:
                    if (Input.GetMouseButton(0))
                    {
                        if (!pressed)
                        {
                            DoInteractStart(GameManager.instance.playerObject);
                        }
                    }
                    break;
            }
        }else if((!hovered || !Input.GetMouseButton(0)) && interactableType == InteractableType.HOLD)
        {
            if (pressed)
            {
                DoInteractEnd(GameManager.instance.playerObject);
            }
        }

        if (pressed || Time.time - lastActive < 0.3)
        {
            SetMaterial(pressedMaterial);
        }
        else
        {
            if (hovered)
            {
                SetMaterial(hoveredMaterial);
            }
            else
            {
                SetMaterial(idleMaterial);
            }
        }

        if (textMeshObject)
        {
            Vector3 forward = -(Camera.main.transform.position - transform.position).normalized;

            textMeshObject.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        if (textMesh)
        {
            textMesh.text = interactableName;

            if (hovered)
            {
                textMeshAlpha = Mathf.Min(textMeshAlpha + (Time.deltaTime * textMeshFadeMultiplier), 1);
            }
            else
            {
                textMeshAlpha = Mathf.Max(textMeshAlpha - (Time.deltaTime * textMeshFadeMultiplier), 0);
            }

            textMesh.color = new Color(textColor.r, textColor.g, textColor.b, textMeshAlpha);
        }
    }

    bool IsPressed()
    {
        return pressed;
    }

    bool IsHovered()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        Debug.DrawLine(ray.origin, ray.direction * 100);

        int layerMask = 1 << LayerMask.NameToLayer("Interactable");

        if (Physics.Raycast(ray.origin, ray.direction, out hit, 100, layerMask))
        {
            Transform objectHit = hit.transform;

            if(objectHit == gameObject.transform || objectHit.transform.parent == gameObject.transform)
            {
                return true;
            }
        }

        return false;
    }

    void DoInteractStart(GameObject invokerObject)
    {
        lastActive = Time.time;

        OnInteractableStart(invokerObject);
        onInteractStart.Invoke(invokerObject);

        pressed = true;
    }

    public float GetLastActive()
    {
        return lastActive;
    }

    void DoInteractEnd(GameObject invokerObject)
    {
        OnInteractableEnd(invokerObject);
        onInteractEnd.Invoke(invokerObject);

        pressed = false;
    }

    protected virtual void SetMaterial(Material material)
    {
        if (!meshRenderer || !material) { return; }

        meshRenderer.sharedMaterial = material;
    }

    protected virtual void OnInteractableStart(GameObject invokerObject)
    {

    }

    protected virtual void OnInteractableEnd(GameObject invokerObject)
    {

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!touchActivates) { return; }
        if(Time.time - lastCollisionTrigger < 1) { return; }
        GameObject collisionObject = collision.rigidbody.gameObject;
        if(collisionObject != GameManager.instance.playerObject) { return; }

        DoInteractStart(collisionObject);
        lastCollisionTrigger = Time.time;
    }
}
