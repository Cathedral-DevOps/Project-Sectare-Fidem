/*
CornerPeek.cs
Attach to the player (or to the object that owns the camera).
Place empty GameObjects named "corner" near corners in the scene.
Hold Q when within `peekRange` to smoothly move and yaw the camera to peek.

Usage:
- Add this script to your player GameObject.
- Ensure corner objects are named exactly "corner" (case-insensitive).
- Optionally assign `cameraTransform` in the inspector or let the script find the main camera.
*/

using UnityEngine;
using System.Collections.Generic;

public class CornerPeek : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Name of the empty GameObjects representing corners (case-insensitive).")]
    public string cornerObjectName = "corner";
    [Tooltip("Maximum distance to a corner to allow peeking.")]
    public float peekRange = 2f;
    [Tooltip("How often (seconds) the script refreshes corner lookup.")]
    public float refreshInterval = 0.25f;

    [Header("Input")]
    public KeyCode peekKey = KeyCode.Q;
    [Tooltip("If true, hold the key to peek. If false, press to toggle peek.")]
    public bool holdToPeek = true;

    [Header("Peek movement")]
    [Tooltip("Lateral offset applied to the camera when peeking.")]
    public float peekDistance = 0.6f;
    [Tooltip("Yaw angle (degrees) to rotate the camera when peeking.")]
    public float peekAngle = 20f;
    [Tooltip("How quickly the camera moves/rotates to the peek pose.")]
    public float peekSpeed = 8f;

    [Header("Camera (optional)")]
    [Tooltip("Assign the player camera here. If left empty the script will try to find one.")]
    public Transform cameraTransform;

    Vector3 originalLocalPos;
    Vector3 originalLocalEuler;
    float nextRefreshTime;
    List<Transform> corners = new List<Transform>();
    Transform nearestCorner;
    bool peekToggled;

    void Start()
    {
        if (cameraTransform == null)
        {
            Camera c = GetComponentInChildren<Camera>();
            if (c != null) cameraTransform = c.transform;
            else if (Camera.main != null) cameraTransform = Camera.main.transform;
        }

        if (cameraTransform == null)
        {
            Debug.LogWarning("CornerPeek: No camera found or assigned. Disable script or assign cameraTransform.");
            enabled = false;
            return;
        }

        originalLocalPos = cameraTransform.localPosition;
        originalLocalEuler = cameraTransform.localEulerAngles;

        RefreshCorners();
    }

    void Update()
    {
        if (Time.time >= nextRefreshTime)
        {
            nextRefreshTime = Time.time + refreshInterval;
            RefreshCorners();
            nearestCorner = FindNearestCorner();
        }

        bool inRange = nearestCorner != null && Vector3.Distance(transform.position, nearestCorner.position) <= peekRange;

        bool wantPeek;
        if (holdToPeek)
            wantPeek = inRange && Input.GetKey(peekKey);
        else
        {
            if (inRange && Input.GetKeyDown(peekKey))
                peekToggled = !peekToggled;
            wantPeek = inRange && peekToggled;
        }

        if (wantPeek)
        {
            float side = Mathf.Sign(transform.InverseTransformPoint(nearestCorner.position).x);
            Vector3 targetLocalPos = ComputeTargetLocalPosition(side);
            Vector3 targetLocalEuler = originalLocalEuler + new Vector3(0f, side * peekAngle, 0f);
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, targetLocalPos, Time.deltaTime * peekSpeed);
            cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, Quaternion.Euler(targetLocalEuler), Time.deltaTime * peekSpeed);
        }
        else
        {
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, originalLocalPos, Time.deltaTime * peekSpeed);
            cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, Quaternion.Euler(originalLocalEuler), Time.deltaTime * peekSpeed);
            if (!holdToPeek)
                peekToggled = false;
        }
    }

    Vector3 ComputeTargetLocalPosition(float side)
    {
        if (cameraTransform.parent != null)
        {
            Vector3 rightInParentSpace = cameraTransform.parent.InverseTransformDirection(cameraTransform.right);
            return originalLocalPos + rightInParentSpace * side * peekDistance;
        }
        else
        {
            return originalLocalPos + cameraTransform.right * side * peekDistance;
        }
    }

    void RefreshCorners()
    {
        corners.Clear();
        Transform[] all = FindObjectsOfType<Transform>();
        string target = cornerObjectName.ToLowerInvariant();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name.ToLowerInvariant() == target)
                corners.Add(all[i]);
        }
    }

    Transform FindNearestCorner()
    {
        Transform best = null;
        float bestSqr = float.MaxValue;
        Vector3 p = transform.position;
        for (int i = 0; i < corners.Count; i++)
        {
            float d = (corners[i].position - p).sqrMagnitude;
            if (d < bestSqr)
            {
                bestSqr = d;
                best = corners[i];
            }
        }
        return best;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, peekRange);

#if UNITY_EDITOR
        // Draw tiny markers for corners in the editor when selected
        if (!Application.isPlaying)
        {
            Transform[] all = FindObjectsOfType<Transform>();
            string target = cornerObjectName;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name.Equals(target, System.StringComparison.OrdinalIgnoreCase))
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(all[i].position, 0.05f);
                }
            }
        }
#endif
    }
}
