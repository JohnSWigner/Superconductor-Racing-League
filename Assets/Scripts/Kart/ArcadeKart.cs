using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.VFX;

namespace SRL
{
    public class ArcadeKart : MonoBehaviour
    {

        public InputData Input     { get; private set; }
        // the input sources that can control the kart
        IInput[] m_Inputs;

        void GatherInputs()
        {
            // reset input
            Input = new InputData();

            // gather nonzero input from our sources
            for (int i = 0; i < m_Inputs.Length; i++)
            {
                Input = m_Inputs[i].GenerateInput();
            }
        }



        [Header("Hover Settings")]
        public float hoverHeight = 2.0f;
        public float hoverForce = 100f;
        public LayerMask groundLayer;
        public float hoverDampening = 5f;

        [Header("Movement Settings")]
        public float forwardSpeed = 50f;
        public float turnSpeed = 20f;
        public float strafeSpeed = 15f;
        public float drag = 2f;


        private Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            m_Inputs = GetComponents<IInput>();
        }

        void FixedUpdate()
        {
            GatherInputs();
            Hover();
            Move();
        }

        void Hover()
        {
            // Raycast to ground to simulate hover
            Ray ray = new Ray(transform.position, -transform.up);
            if (Physics.Raycast(ray, out RaycastHit hit, hoverHeight * 2, groundLayer))
            {
                float hoverError = hoverHeight - hit.distance;
                float upwardSpeed = rb.linearVelocity.y;
                float appliedHoverForce = hoverError * hoverForce - upwardSpeed * hoverDampening;

                rb.AddForce(Vector3.up * appliedHoverForce, ForceMode.Acceleration);
            }
        }

        void Move()
        {
            // Forward movement
            float forwardInput = Input.Accelerate ? 1f : 0f;
            Vector3 forwardForce = transform.forward * forwardInput * forwardSpeed;
            rb.AddForce(forwardForce, ForceMode.Acceleration);

            // Turning (rotate around Y-axis)
            float turnInput = Input.TurnInput;
            rb.AddTorque(Vector3.up * turnInput * turnSpeed, ForceMode.Acceleration);

            // Apply drag to reduce endless acceleration
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, drag * Time.fixedDeltaTime);
        }
    }
}
