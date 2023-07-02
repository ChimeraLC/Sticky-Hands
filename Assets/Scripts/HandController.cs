using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class HandController : MonoBehaviour
{
        public GameObject head;

        public Vector3 velocity;
        public Vector3 acceleration;
        public Vector3 socketPos;
        public float armLength = 2.5f;
        public float armMaxLength = 5;

        void Start()
        {
                velocity = Vector2.zero;
        }

        // Update is called once per frame
        void Update()
        {
                
        }

        // Calculates the physics for an unlocked hand, with applied acceleration 'external'
        public void PhysicsCalc(Vector3 external) {
                // Calculate each frame's acceleration independantly
                acceleration = external;

                // Distance from other hand
                Vector3 displacement = head.transform.position + socketPos - transform.position;

                // Pull if the arm is stretched
                float pull = Mathf.Max(displacement.magnitude - armLength, 0);

                acceleration += 30 * displacement.normalized * pull;

                transform.position += Time.deltaTime * (velocity + 0.5f * acceleration * Time.deltaTime);

                // Acceleration
                velocity += acceleration * Time.deltaTime;

                // Bounding (Potentially not needed?)
                // TODO: fix this
                displacement = transform.position - head.transform.position - socketPos;
                if (displacement.magnitude > armMaxLength) {
                        transform.position = head.transform.position + socketPos + displacement.normalized * armMaxLength;
                        // Removing velocity in direction of arm
                        //velocity -= Vector2.Dot(velocity, displacement) / displacement.sqrMagnitude * displacement;
                }
                
                

                // Air resistance
                velocity *= Mathf.Pow(0.6f, Time.deltaTime);
        }
}
