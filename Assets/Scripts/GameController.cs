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
        private float lockTimer = 0.25f;
        private float armTimer = 0;
        // Arm details
        private float armLength = 2.5f;
        private float armMaxLength = 4f;
        private float stretch = 1.5f;
        private int armSegments = 15;
        // Displacement from center where arms extend from
        private Vector3 socketPos = new Vector3(0.2f, 0);
        // Segment prefabs
        public GameObject segment;

        // Camera controll
        public GameObject currentCam;

        /*
        private List<KeyCode> possibleKeysLong = new List<KeyCode>(){ KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T,
                KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F,
                KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V,
                KeyCode.B, KeyCode.N, KeyCode.M};
        private List<KeyCode> possibleKeys = new List<KeyCode>() { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R,
                KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V};
        private List<KeyCode> possibleKeysShortest = new List<KeyCode>() { KeyCode.Q, KeyCode.W, KeyCode.E };
        */
        private List<KeyCode> possibleKeysLeft = new List<KeyCode>() {
                KeyCode.Q, KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.Z, KeyCode.X
        };
        private List<KeyCode> possibleKeysCenter = new List<KeyCode>() {
                KeyCode.E, KeyCode.R, KeyCode.D, KeyCode.F, KeyCode.C, KeyCode.V, KeyCode.T, KeyCode.G, KeyCode.B
        };
        private List<KeyCode> possibleKeysRight = new List<KeyCode>() {
                KeyCode.Y, KeyCode.H, KeyCode.N, KeyCode.U, KeyCode.K, KeyCode.M
        };
        private List<KeyCode>[] keyLists;
        private List<Hand> handQueue = new List<Hand>();

        // TODO: make these exclusive
        private List<Vector3> possibleOffsets = new List<Vector3>() {
        new Vector3(-0.2f, 0), new Vector3(0, 0.2f), new Vector3(0.2f, 0)
        };

        // Collision
        public Tilemap tiles;
        public Tilemap tilesBad;

        public Vector3 velocity = Vector3.zero;
        public Vector3 acceleration = Vector3.zero;
        private bool grounded = true;

        // Start is called before the first frame update
        void Start()
        {
                keyLists = new List<KeyCode>[]{ possibleKeysLeft, possibleKeysCenter, possibleKeysRight};
                // Creating initial hands
                CreateHand(new Vector3(-0.2f, 0), 0);
                CreateHand(new Vector3(0, -0.2f), 2);

                CreateHand(new Vector3(0.2f, 0), 1);
        }

        // Update is called once per frame
        void Update()
        {
                // Mouse position
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                // Debug controlls
                if (Input.GetMouseButton(0)) {
                        head.transform.position = mousePosition;
                }

                // Control hands
                foreach (Hand hand in hands.ToArray()) {
                        Handle(mousePosition, hand);
                }

                // Creating hands
                armTimer = Mathf.Max(0, armTimer - Time.deltaTime);
                if (armTimer == 0 && handQueue.Count > 0) {
                        Hand hand = handQueue[0];
                        // TODO: add a delay while also not repeating
                        CreateHand(possibleOffsets[hand.handType], hand.handType);
                        keyLists[hand.handType].Add(hand.handKey);
                        handQueue.RemoveAt(0);
                        armTimer = 0.25f;
                }
                // Head physics
                PhysicsCalc(new Vector3(0, -15));

                // Draw arm
                foreach (Hand hand in hands) {
                        // More stretchy when in use
                        if (Input.GetKey(hand.handKey)) {
                                hand.handCont.SetAngle(DrawArm(hand.handObj.transform.position, head.transform.position + hand.handOffset, hand.handArm));
                        }
                        else
                        {
                                hand.handCont.SetAngle(DrawArm(hand.handObj.transform.position, head.transform.position + hand.handOffset, hand.handArm, 1));

                        }
                }
        }
        // Creates a new hand with the given offset and corresponding key
        void CreateHand(Vector3 offset, int keyType) {
                // Generate random keys
                KeyCode hKey = GetKey(keyType);

                // Creating gameobjects
                GameObject hand = Instantiate(handPrefab, head.transform.position, Quaternion.identity);

                GameObject[] arm = new GameObject[armSegments];
                for (int i = 0; i < armSegments; i++)
                {
                        arm[i] = Instantiate(segmentPrefab);
                }

                // Creating object
                Hand newHand = new Hand(hKey, offset, hand, arm, keyType);

                // Setting values
                newHand.handCont.head = head;
                newHand.handCont.socketPos = offset;
                newHand.handCont.armLength = armLength;
                newHand.handCont.armMaxLength = armMaxLength;
                newHand.handCont.handKey = hKey;
                newHand.handCont.gameController = this;
                newHand.handCont.hand = newHand;
                newHand.handCont.velocity = offset * 20;
                hands.Add(newHand);
        }
        // Returns a random key from the list of available keys
        KeyCode GetKey(int keyType) {
                // TODO: should this be + 1?
                int index = Random.Range(0, keyLists[keyType].Count);
                KeyCode ret = keyLists[keyType][index];
                keyLists[keyType].RemoveAt(index);
                return ret;
        }

        // Draws a catenary curve between the two positions, returns the angle the hand should be at
        float DrawArm(Vector2 first, Vector2 second, GameObject[] segments, float shorter = 0)
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
                if ((left - right).magnitude < (armLength - shorter))
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
                                        yPos = Mathf.Abs(((float)i + 1) / ((float)armSegments + 1) * (armLength - shorter) - (top.y - bottom.y) - droop) - droop;
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
                                float value = Mathf.Sqrt(Mathf.Pow((armLength - shorter), 2) - Mathf.Pow(vertical, 2));
                                value /= horizontal;

                                // Solving
                                float A = SolveCat(value);

                                // Calculating intermediate values
                                float a = horizontal / (2 * A);
                                float b = centerX - a * (float)System.Math.Atanh(vertical / (armLength - shorter));
                                float c = centerY - (armLength - shorter) / (2 * (float)System.Math.Tanh(A));

                                // Placing arm segments
                                float xPos;
                                float yPos;
                                // Initial integral point
                                float start = a * (float)System.Math.Sinh((left.x - b) / a);
                                for (int i = 0; i < armSegments; i++)
                                {
                                        // Calculating desired integral based on arm length
                                        float desired = start + (armLength - shorter) / (armSegments + 1) * (i + 1);
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
                // If hand is free to move
                if (!hand.handLock)
                {
                        // Moving hands towards keys
                        if (Input.GetKey(hand.handKey))
                        {
                                // Disable physics
                                hand.handCont.velocity = Vector2.zero;
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

                                // Attatching
                                if ((hand.handObj.transform.position - desiredPosition).magnitude < 0.5f && HasTile(hand.handObj.transform.position))
                                {
                                        hand.handTimer = Mathf.Min(lockTimer, hand.handTimer + Time.deltaTime);
                                        if (hand.handTimer >= lockTimer) {
                                                hand.handLock = true;
                                        }
                                }
                                else {
                                        hand.handTimer = Mathf.Max(0, hand.handTimer - Time.deltaTime);
                                }

                        }
                        else
                        {
                                hand.handTimer = Mathf.Max(0, hand.handTimer - Time.deltaTime);
                                hand.handCont.PhysicsCalc(new Vector3(0, -5));
                        }
                }
                // Grasping
                if (Input.GetKeyUp(hand.handKey))
                {
                       hand.handCont.Signal();
                }

                hand.handCont.UpdateRadial(hand.handTimer / lockTimer);
                // Animation state
                hand.handCont.handState = hand.handLock;

        }

        // Calculates physics for the head
        void PhysicsCalc(Vector3 external)
        {
                acceleration = external;

                // Adding in acceleration from hands
                Vector3 displacement;
                float pull;
                bool attatched = false;
                // TODO: clean this up
                Vector3 handAcc = Vector3.zero;
                foreach (Hand hand in hands) {
                        if (hand.handLock)
                        {
                                displacement = hand.handObj.transform.position - (head.transform.position + hand.handOffset);
                                pull = Mathf.Max(displacement.magnitude - armLength, 0);

                                acceleration += 15 * displacement.normalized * pull;
                        }

                        if (Input.GetKey(hand.handKey))
                        {
                                displacement = hand.handObj.transform.position - (head.transform.position + hand.handOffset);
                                pull = Mathf.Max(displacement.magnitude - armLength, 0);

                                handAcc += 3 * displacement.normalized * pull;
                        }

                        attatched = attatched || hand.handLock;
                }
                // Normalizing handAcc before including
                if (handAcc.magnitude > external.magnitude * 2 / 3) {
                        handAcc = handAcc.normalized * external.magnitude * 2 / 3;
                }
                acceleration += handAcc;
                // FInd potential position
                Vector3 potentialPosition = head.transform.position + Time.deltaTime * (velocity + 0.5f * Time.deltaTime * acceleration);
                velocity += acceleration * Time.deltaTime;
                //velocity *= Mathf.Pow(0.8f, Time.deltaTime);

                if (attatched)
                {
                        velocity *= Mathf.Pow(0.8f, Time.deltaTime);
                }

                //Debug.Log((head.transform.position, acceleration, velocity, potentialPosition, Time.deltaTime));
                // First check horizontal position
                if (HasTile(new Vector3(potentialPosition.x, head.transform.position.y)) || HasTileBad(new Vector3(potentialPosition.x, head.transform.position.y)))
                {
                        if (head.transform.position.x < potentialPosition.x)
                        {
                                potentialPosition.x = Mathf.Floor(potentialPosition.x) - 0.01f;
                        }
                        else
                        {
                                potentialPosition.x = Mathf.Ceil(potentialPosition.x);

                        }
                        velocity.x = 0;
                }

                // Then check vertical position
                if (HasTile(new Vector3(head.transform.position.x, potentialPosition.y)) || HasTileBad(new Vector3(head.transform.position.x, potentialPosition.y)))
                {
                        if (head.transform.position.y < potentialPosition.y)
                        {
                                potentialPosition.y = Mathf.Floor(potentialPosition.y) - 0.01f;
                        }
                        else
                        {
                                potentialPosition.y = Mathf.Ceil(potentialPosition.y);
                        }
                        velocity.y = 0;
                        // Friction
                        if (velocity.x > 0)
                        {
                                velocity.x = Mathf.Max(0, velocity.x - 10 * Time.deltaTime);
                        }
                        else
                        {
                                velocity.x = Mathf.Min(0, velocity.x + 10 * Time.deltaTime);
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
                return tiles.HasTile(new Vector3Int((int)Mathf.Floor(pos.x), (int)Mathf.Floor(pos.y)));
        }
        bool HasTileBad(Vector3 pos)
        {
                return tilesBad.HasTile(new Vector3Int((int)Mathf.Floor(pos.x), (int)Mathf.Floor(pos.y)));
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
        // Signals a hand is dead and keys can be reclaimed
        public void Signal(Hand hand) {
                foreach (GameObject segment in hand.handArm) {
                        Destroy(segment);
                }
                handQueue.Add(hand);
                hands.Remove(hand);
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
        // Timer for grasping
        public float handTimer { get; set; }
        // Representing what part of the keyboard
        public int handType { get; set; }

        public Hand(KeyCode hKey, Vector3 offset, GameObject hand, GameObject[] arm, int hType) {
                handObj = hand;
                handCont = hand.GetComponent<HandController>();
                handLock = false;
                handOffset = offset;
                handKey = hKey;
                handArm = arm;
                handTimer = 0;
                handType = hType;
        }
}