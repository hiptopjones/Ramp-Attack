﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehiclePhysicsController : MonoBehaviour
{
    [SerializeField] Vector3 centerOfMass;
    [SerializeField] WheelCollider[] wheelColliders;
    [SerializeField] GameObject[] wheelMeshes;

    [SerializeField] float speed = 25;
    [SerializeField] float pitchCorrectionTime = 5;

    private VehicleRenderController vehicleRenderController;
    private Rigidbody vehicleRigidbody;

    private float lastGroundedTime = 0;

    private bool isRunning = false;

    private float correctedPitchAngle = 0;

    void Start()
    {
        vehicleRenderController = FindObjectOfType<VehicleRenderController>();
        if (vehicleRenderController == null)
        {
            throw new System.Exception($"Unable to find object of type {nameof(VehicleRenderController)}");
        }

        vehicleRigidbody = GetComponentInParent<Rigidbody>();
        if (vehicleRigidbody == null)
        {
            throw new System.Exception($"Unable to get component in parent of type {nameof(Rigidbody)}");
        }

        // Increase stability
        vehicleRigidbody.centerOfMass = centerOfMass;
        vehicleRigidbody.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        if (false == isRunning)
        {
            return;
        }
    }

    void FixedUpdate()
    {
        if (false == isRunning)
        {
            return;
        }

        if (IsAirborne())
        {
            Vector3 eulerAngles = vehicleRigidbody.rotation.eulerAngles;

            float newPitchAngle = eulerAngles.x;
            float newYawAngle = 0;
            float newRollAngle = 0;

            if (Mathf.Abs(eulerAngles.x % 360) < 1)
            {
                newPitchAngle = 0;
            }
            else
            {
                // Straighten out any rotation so we land properly
                //  - Should do this on all axes to be safe?
                //  - Force the end of the trick if too close to the ground
                float elapsedAirTime = Time.time - lastGroundedTime;
                float correctedPitchAngleFraction = Mathf.Clamp(elapsedAirTime / pitchCorrectionTime, 0, 1);

                newPitchAngle = Mathf.LerpAngle(eulerAngles.x, correctedPitchAngle, correctedPitchAngleFraction);
            }

            Vector3 newEulerAngles = new Vector3(newPitchAngle, newYawAngle, newRollAngle);
            vehicleRigidbody.rotation = Quaternion.Euler(newEulerAngles);
        }
        else
        {
            lastGroundedTime = Time.time;

            if (IsGrounded())
            {
                // Maintain speed when grounded
                vehicleRigidbody.velocity = Vector3.forward * speed;

                // Remove any stray rotations (why isn't this fixed by rigidbody constraints?)
                vehicleRigidbody.rotation = Quaternion.identity;

                // Remove any stray position drift (why isn't this fixed by rigidbody constraints?)
                vehicleRigidbody.position = new Vector3(0, transform.position.y, transform.position.z);
            }
        }
    }

    public bool IsGrounded()
    {
        // Determine if all four wheels are on the ground
        foreach (WheelCollider wheelCollider in wheelColliders)
        {
            WheelHit hit;
            if (false == wheelCollider.GetGroundHit(out hit))
            {
                return false;
            }

            if (hit.collider.tag != "Ground")
            {
                return false;
            }
        }

        return true;
    }

    public bool IsAirborne()
    {
        // Determine if all four wheels are off the ground
        foreach (WheelCollider wheelCollider in wheelColliders)
        {
            WheelHit hit;
            if (wheelCollider.GetGroundHit(out hit))
            {
                return false;
            }
        }

        return true;
    }

    public bool IsRunning()
    {
        return isRunning;
    }

    public void StartPhysics()
    {
        isRunning = true;

        // Start motion
        vehicleRigidbody.velocity = Vector3.forward * speed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            // Stop everything
            isRunning = false;
            vehicleRigidbody.velocity = Vector3.zero;

            // Notify the render twin
            vehicleRenderController.OnPlayerDeath();
        }
    }
}
