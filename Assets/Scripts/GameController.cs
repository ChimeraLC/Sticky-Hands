using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.XR;
using static UnityEditor.PlayerSettings;

public class GameController : MonoBehaviour
{
        // Lists corresponding to the collection of hands
        public List<Hand> hands = new List<Hand>();

        // Prefabs
        public GameObject handPrefab;
        public GameObject segmentPrefab;
        public GameObject head;

        // Arm details
        private float armLength = 2f;
        private float armMaxLength = 4f;
        private float stretch = 2f;
        private int armSegments = 15;
        // Displacement from center where arms extend from
        private Vector3 socketPos = new Vector3(0.2f, 0);
        // Segment prefabs
        public GameObject segment;

        // State control
        private int state = 1;

        // Collision
        public Tilemap tiles;


        public Vector3 velocity = Vector3.zero;
        public Vector3 acceleration = Vector3.zero;
        private bool grounded = true;

        // Start is called before the first frame update
        void Start()
        {
                // Creating initial hands
                CreateHand(new Vector3(0.2f, 0), KeyCode.E);
                CreateHand(new Vector3(-0.2f, 0), KeyCode.Q);
        }

        // Update is called once per frame
        void Update()
        {
                // Mouse position
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                // Control hands
                foreach (Hand hand in hands) {
                        Handle(mousePosition, hand);
                }

                // Head physics
                PhysicsCalc(new Vector3(0, -5));


                // Draw arm
                foreach (Hand hand in hands) {
                        hand.handCont.SetAngle(DrawArm(hand.handObj.transform.position, head.transform.position + hand.handOffset, hand.handArm));
                }
        }
        // Creates a new hand with the given offset and corresponding key
        void CreateHand(Vector3 offset, KeyCode key) {
                // Creating gameobjects
                GameObject hand = Instantiate(handPrefab);
                GameObject[] arm = new GameObject[armSegments];
                for (int i = 0; i < armSegments; i++)
                {
                        arm[i] = Instantiate(segmentPrefab);
                }

                // Creating object
                Hand newHand = new Hand(key, offset, hand, arm);

                // Setting values
                newHand.handCont.head = head;
                newHand.handCont.socketPos = offset;
                newHand.handCont.armLength = armLength;
                newHand.handCont.armMaxLength = armMaxLength;

                hands.Add(newHand);
        }

        // Draws a catenary curve between the two positions, returns the angle the hand should be at
        float DrawArm(Vector2 first, Vector2 second, GameObject[] segments)
        {

                // Calculating which object is on the left and right
                Vector2 left;
                Vector2 right;

                if (first.x < second.x)
                {
                        left = first;
                        right = second;
                }
                else
                {
                        left = second;
                        right = first;
                }

                // Calculating distances v and h
                float vertical = right.y - left.y;
                float horizontal = right.x - left.x;
                float centerX = (right.x + left.x) / 2;
                float centerY = (right.y + left.y) / 2;

                // If arm is 'limp'
                if ((left - right).magnitude < armLength)
                {
                        // Edge case when vertical
                        if (left.x == right.x)
                        {
                                // Manually calculating shape based on length
                                Vector2 top;
                                Vector2 bottom;
                                if (left.y < right.y)
                                {
                                        top = right;
                                        bottom = left;
                                }
                                else
                                {
                                        top = left;
                                        bottom = right;
                                }
                                float droop = (armLength - (top.y - bottom.y)) / 2;
                                float yPos;
                                // Placing segments
                                for (int i = 0; i < armSegments; i++)
                                {
                                        // TODO: clean this up
                                        yPos = Mathf.Abs(((float)i + 1) / ((float)armSegments + 1) * armLength - (top.y - bottom.y) - droop) - droop;
                                        segments[i].transform.position = new Vector2(left.x, bottom.y + yPos);
                                }
                                return 0;

                        }
                        else
                        {
                                /*
                                 * Solving catenary equation
                                 */

                                // sqrt(L^2 - v^2) / h
                                float value = Mathf.Sqrt(Mathf.Pow(armLength, 2) - Mathf.Pow(vertical, 2));
                                value /= horizontal;

                                // Solving
                                float A = SolveCat(value);

                                // Calculating intermediate values
                                float a = horizontal / (2 * A);
                                float b = centerX - a * (float)System.Math.Atanh(vertical / armLength);
                                float c = centerY - armLength / (2 * (float)System.Math.Tanh(A));

                                // Placing arm segments
                                float xPos;
                                float yPos;
                                // Initial integral point
                                float start = a * (float)System.Math.Sinh((left.x - b) / a);
                                for (int i = 0; i < armSegments; i++)
                                {
                                        // Calculating desired integral based on arm length
                                        float desired = start + armLength / (armSegments + 1) * (i + 1);
                                        // Corresponding xna d y points
                                        xPos = (float)System.Math.Asinh(desired / a) * a + b;
                                        yPos = a * (float)System.Math.Cosh((xPos - b) / a) + c;
                                        // Place segment at that location
                                        segments[i].transform.position = new Vector2(xPos, yPos);
                                }
                                // Calculating directino hand should face
                                if (first.x > second.x)
                                {
                                        return AccurateTan(new Vector2(1, (float)System.Math.Sinh((first.x - b) / a))) * Mathf.Rad2Deg - 90;
                                }
                                else
                                {

                                        return AccurateTan(new Vector2(1, (float)System.Math.Sinh((first.x - b) / a))) * Mathf.Rad2Deg + 90;
                                }
                        }
                }
                // Stretched arm
                else
                {
                        Vector2 dif = right - left;
                        /*
                        if (dif.magnitude > armMaxLength) {
                                dif = armMaxLength * dif.normalized;
                        }
                        */
                        // Placing arm segments
                        for (int i = 0; i < armSegments; i++)
                        {
                                segments[i].transform.position = left + dif / (armSegments + 1) * (i + 1);
                        }

                        return AccurateTan(first - second) * Mathf.Rad2Deg - 90;
                }

        }
        // Finds an approximate solution to sinh(A) / A = r
        float SolveCat(float value)
        {
                float A;
                // Starting values dependent on value
                if (value < 3)
                {
                        A = Mathf.Sqrt(6 * (value - 1));
                }
                else
                {
                        A = Mathf.Log(2 * value) + Mathf.Log(Mathf.Log(2 * value));
                }

                // Iterate 5 times TODO: maybe check in between?
                for (int i = 0; i < 5; i++)
                {
                        A = A - ((float)System.Math.Sinh(A) - value * A) / ((float)System.Math.Cosh(A) - value);
                }

                return A;
        }
        // Provides the larger solution to the quadratic equation to ax^2 + bx + c = 0, returns 0 on no real solutions
        float SolveQuad(float a, float b, float c)
        {
                // Determinant
                float det = b * b - 4 * a * c;
                // No real solutions
                if (det < 0) { return 0; }
                else
                        return (-b + Mathf.Sqrt(b * b - 4 * a * c)) / (2 * a);
        }

        // Helper function for dragging and releasing hands
        void Handle(Vector3 mousePosition, Hand hand)
        {

                // Moving hands towards keys
                if (Input.GetKeyDown(hand.handKey))
                {
                        // Disable physics
                        hand.handLock = false;
                        hand.handCont.velocity = Vector2.zero;
                        //TODO: fix this
                }
                if (Input.GetKey(hand.handKey))
                {
                        // Bounding length from other hand
                        Vector3 desiredPosition;
                        if (((Vector3)mousePosition - (head.transform.position + hand.handOffset)).magnitude > armLength + stretch)
                        {
                                desiredPosition = ((Vector3)mousePosition -
                                        (head.transform.position + hand.handOffset)).normalized * (armLength + stretch)
                                        + head.transform.position + hand.handOffset;
                        }
                        else
                        {
                                desiredPosition = mousePosition;

                        }

                        hand.handObj.transform.position = Vector3.Lerp(hand.handObj.transform.position, desiredPosition, 0.1f);

                }
                else if (!hand.handLock)
                {
                        hand.handCont.PhysicsCalc(new Vector3(0, -5));
                }
                // Grasping
                if (Input.GetKeyUp(hand.handKey))
                {
                        hand.handLock = HasTile(hand.handObj.transform.position);
                }

                // Animation state
                hand.handCont.handState = hand.handLock;

        }

        // Calculates physics for the head
        void PhysicsCalc(Vector3 external)
        {
                acceleration = external * 3;

                // Adding in acceleration from hands
                Vector3 displacement;
                float pull;
                bool attatched = false;
                // TODO: clean this up
                foreach (Hand hand in hands) {
                        if (hand.handLock)
                        {
                                displacement = hand.handObj.transform.position - (head.transform.position + hand.handOffset);
                                pull = Mathf.Max(displacement.magnitude - armLength, 0);

                                acceleration += 40 * displacement.normalized * pull;
                        }
                        else if (Input.GetKey(hand.handKey))
                        {
                                displacement = hand.handObj.transform.position - (head.transform.position + hand.handOffset);
                                pull = Mathf.Max(displacement.magnitude - armLength, 0);

                                acceleration += 3 * displacement.normalized * pull;
                        }
                        attatched = attatched || hand.handLock;
                }

                // FInd potential position
                Vector3 potentialPosition = head.transform.position + Time.deltaTime * (velocity + 0.5f * Time.deltaTime * acceleration);
                velocity += acceleration * Time.deltaTime;
                velocity *= Mathf.Pow(0.8f, Time.deltaTime);

                if (attatched)
                {
                        velocity *= Mathf.Pow(0.4f, Time.deltaTime);
                }

                //Debug.Log((head.transform.position, acceleration, velocity, potentialPosition, Time.deltaTime));
                // First check horizontal position
                if (HasTile(new Vector3(potentialPosition.x, head.transform.position.y)))
                {
                        if (head.transform.position.x < potentialPosition.x)
                        {
                                potentialPosition.x = Mathf.Floor(potentialPosition.x * 2) / 2 - 0.01f;
                        }
                        else
                        {
                                potentialPosition.x = Mathf.Ceil(potentialPosition.x * 2) / 2;

                        }
                        velocity.x = 0;
                }

                // Then check vertical position
                if (HasTile(new Vector3(head.transform.position.x, potentialPosition.y)))
                {
                        if (head.transform.position.y < potentialPosition.y)
                        {
                                potentialPosition.y = Mathf.Floor(potentialPosition.y * 2) / 2 - 0.01f;
                        }
                        else
                        {
                                potentialPosition.y = Mathf.Ceil(potentialPosition.y * 2) / 2;
                        }
                        velocity.y = 0;
                        // Friction
                        if (velocity.x > 0)
                        {
                                velocity.x = Mathf.Max(0, velocity.x - 50 * Time.deltaTime);
                        }
                        else
                        {
                                velocity.x = Mathf.Min(0, velocity.x + 50 * Time.deltaTime);
                        }

                        // Grounded state
                        grounded = true;
                }
                else
                {
                        grounded = false;
                }
                // TODO: fix the calculations here?
                if (HasTile(potentialPosition))
                {
                        Debug.Log("Failed");
                }
                else
                {
                        head.transform.position = potentialPosition;
                }


        }
        // Moves the head in a direction until it reaches a wall
        void moveTowards(Vector3 position)
        {
                Vector3 potentialPosition;
                if ((position - head.transform.position).magnitude < 20 * Time.deltaTime)
                {
                        potentialPosition = position;
                }
                else
                {
                        velocity = (position - head.transform.position).normalized * 20;
                        potentialPosition = head.transform.position + Time.deltaTime * velocity;
                }
                // First check horizontal position
                if (HasTile(new Vector3(potentialPosition.x, head.transform.position.y)))
                {
                        if (head.transform.position.x < potentialPosition.x)
                        {
                                potentialPosition.x = Mathf.Floor(potentialPosition.x * 2) / 2 - 0.01f; ;
                        }
                        else
                        {
                                potentialPosition.x = Mathf.Ceil(potentialPosition.x * 2) / 2 + 0.01f; ;

                        }
                }

                // Then check vertical position
                if (HasTile(new Vector3(head.transform.position.x, potentialPosition.y)))
                {
                        if (head.transform.position.y < potentialPosition.y)
                        {
                                potentialPosition.y = Mathf.Floor(potentialPosition.y * 2) / 2 - 0.01f; ;
                        }
                        else
                        {
                                potentialPosition.y = Mathf.Ceil(potentialPosition.y * 2) / 2 + 0.01f;
                        }
                }
                head.transform.position = potentialPosition;
        }
        // Checks if there is a collision tile at that specific point
        bool HasTile(Vector3 pos)
        {
                return tiles.HasTile(new Vector3Int((int)Mathf.Floor(pos.x * 2), (int)Mathf.Floor(pos.y * 2)));
        }
        // Returns a 0-2pi tan of the given direction
        float AccurateTan(Vector2 dir)
        {
                float ret = Mathf.Atan(dir.y / dir.x);
                if (dir.x < 0)
                {
                        ret = Mathf.PI + ret;
                }
                return ret;
        }
}

// Class containing info for each hand
public class Hand
{
        // Gameobject of hand
        public GameObject handObj { get; set; }
        // Controller of hand object
        public HandController handCont { get; set; }
        // Lock on hand movement
        public bool handLock { get; set; }
        // Offset of hand from body
        public Vector3 handOffset { get; set; }
        // Key corresponding to hand
        public KeyCode handKey { get; set; }
        // List of gameobjects making up arm
        public GameObject[] handArm { get; set; }

        public Hand(KeyCode key, Vector3 offset, GameObject hand, GameObject[] arm) {
                handObj = hand;
                handCont = hand.GetComponent<HandController>();
                handLock = false;
                handOffset = offset;
                handKey = key;
                handArm = arm;

        }
}