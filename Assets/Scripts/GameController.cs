using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEditor.PlayerSettings;

public class GameController : MonoBehaviour
{
        // Hands
        public GameObject leftHand;
        public GameObject rightHand;
        public GameObject head;
        private HandController leftCont;
        private HandController rightCont;
        private GameObject selected;
        private bool leftLocked = true;
        private bool rightLocked = true;
        private float handCoef = 1;
        private Vector3 socketPos = new Vector3(0.2f, 0);
        // Arm details
        private float armLength = 2.5f;
        private float armMaxLength = 5;
        private int armSegments = 10;
        public GameObject segment;
        private GameObject[] rightSegments;
        private GameObject[] leftSegments;
        // State control
        private int state = 1;

        // Collision
        public Tilemap tiles;
        public Vector3 velocity;
        public Vector3 acceleration;

        // Start is called before the first frame update
        void Start()
        {
                // Grabbing controllers
                leftCont = leftHand.GetComponent<HandController>();
                rightCont = rightHand.GetComponent<HandController>();
                // Creating arm segments
                rightSegments = new GameObject[armSegments];
                leftSegments = new GameObject[armSegments];
                for (int i = 0; i < armSegments; i++)
                {
                        rightSegments[i] = Instantiate(segment);
                        leftSegments[i] = Instantiate(segment);
                }
        }

        // Update is called once per frame
        void Update()
        {

                // Mouse position
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                /*
                 * Selecting hands
                 */
                if (Input.GetMouseButtonDown(0)) {
                        // Selecting head
                        if ((mousePosition - (Vector2)head.transform.position).magnitude < 0.5f) {
                                selected = head;
                        }
                        // Selecting hands
                        else if ((mousePosition - (Vector2)rightHand.transform.position).magnitude < 0.25f)
                        {
                                selected = rightHand;
                                handCoef = 1;
                                rightLocked = false;
                        }
                        else if ((mousePosition - (Vector2)leftHand.transform.position).magnitude < 0.25f)
                        {
                                selected = leftHand;
                                handCoef = -1;
                                leftLocked = false;
                        }
                }
                // Dragging body parts
                if (Input.GetMouseButton(0)) {
                        // Dragging head
                        if (selected == head) {
                                //If at least one hand is locked
                                if (rightLocked || leftLocked)
                                {
                                        // Midpoint of hands
                                        Vector3 midpoint;
                                        if (leftLocked && rightLocked)
                                        {
                                                midpoint = (leftHand.transform.position + rightHand.transform.position) / 2;
                                        }
                                        else if (leftLocked)
                                        {
                                                midpoint = leftHand.transform.position;
                                        }
                                        else {
                                                midpoint = rightHand.transform.position;
                                        }
                                        float ang;
                                        float midDis;
                                        Vector3 displacement = (Vector3)mousePosition - midpoint;
                                        // If both hands are locked
                                        if (leftLocked && rightLocked)
                                        {
                                                Vector3 handLine = rightHand.transform.position - leftHand.transform.position;
                                                ang = Mathf.Acos(Vector3.Dot(handLine, displacement) / (displacement.magnitude * handLine.magnitude));
                                                midDis = handLine.magnitude / 2 - socketPos.magnitude;

                                        }

                                        // Otherwise, only one hand is locked
                                        else {
                                                ang = Mathf.PI - Mathf.Acos(Vector3.Dot(socketPos, displacement) / (displacement.magnitude * socketPos.magnitude));
                                                midDis = socketPos.magnitude;
                                        }
                                        // Finding furthest either hand could stretch
                                        // Only include if hand is locked
                                        float maxLeft = Mathf.Infinity;
                                        if (leftLocked) maxLeft = SolveQuad(1, -2 * midDis * Mathf.Cos(Mathf.PI - ang), midDis * midDis - armMaxLength * armMaxLength);
                                        float maxRight = Mathf.Infinity;
                                        if (rightLocked) maxRight = SolveQuad(1, -2 * midDis * Mathf.Cos(ang), midDis * midDis - armMaxLength * armMaxLength);
                                        // Choosing length that satisfies all distances
                                        selected.transform.position = midpoint + displacement.normalized * Mathf.Min(displacement.magnitude, maxLeft, maxRight);
                                }

                        }
                        // Dragging hands
                        else if (selected != null)
                        {
                                // Bounding length from other hand
                                if (((Vector3)mousePosition - (head.transform.position + handCoef * socketPos)).magnitude > armLength)
                                {
                                        selected.transform.position = ((Vector3)mousePosition -
                                                (head.transform.position + handCoef * socketPos)).normalized * armLength
                                                + head.transform.position + handCoef * socketPos;
                                }
                                else
                                        selected.transform.position = mousePosition;
                        }
                }
                // Releasing hands
                if (Input.GetMouseButtonUp(0))
                {
                        if (selected == rightHand)
                        {
                                rightCont.velocity = Vector2.zero;
                                rightLocked = HasTile(rightHand.transform.position);
                        }
                        if (selected == leftHand)
                        {
                                leftCont.velocity = Vector2.zero;
                                leftLocked = HasTile(leftHand.transform.position);
                        }

                        selected = null;
                }

                // Moving hands towards keys
                if (Input.GetKey(KeyCode.Q))
                {
                        // Bounding length from other hand
                        Vector3 desiredPosition;
                        if (((Vector3)mousePosition - (head.transform.position - socketPos)).magnitude > armLength)
                        {
                                desiredPosition = ((Vector3)mousePosition -
                                        (head.transform.position - socketPos)).normalized * armLength
                                        + head.transform.position - socketPos;
                        }
                        else
                        {
                                desiredPosition = mousePosition;

                        }

                        leftHand.transform.position = Vector3.Lerp(leftHand.transform.position, desiredPosition, 0.1f);
                }

                if (Input.GetKeyUp(KeyCode.Q))
                {
                        leftLocked = HasTile(leftHand.transform.position);
                }
                // Moving hands towards keys
                if (Input.GetKey(KeyCode.E))
                {
                        // Bounding length from other hand
                        Vector3 desiredPosition;
                        if (((Vector3)mousePosition - (head.transform.position + socketPos)).magnitude > armLength)
                        {
                                desiredPosition = ((Vector3)mousePosition -
                                        (head.transform.position + socketPos)).normalized * armLength
                                        + head.transform.position + socketPos;
                        }
                        else
                        {
                                desiredPosition = mousePosition;

                        }

                        rightHand.transform.position = Vector3.Lerp(rightHand.transform.position, desiredPosition, 0.1f);
                }
                if (Input.GetKeyUp(KeyCode.E))
                {
                        rightLocked = HasTile(rightHand.transform.position);
                }
                // Toggling hand grips
                if (Input.GetKeyDown(KeyCode.A)) {
                        rightLocked = !rightLocked;
                }
                if (Input.GetKeyDown(KeyCode.D)) {
                        leftLocked = !leftLocked;
                }
                if (Input.GetKeyDown(KeyCode.W)) {
                        rightLocked = false;
                        leftLocked = false;
                }
                // Physics
                if (!rightLocked && selected != rightHand) rightCont.PhysicsCalc(new Vector3(0, -5));
                if (!leftLocked && selected != leftHand) leftCont.PhysicsCalc(new Vector3(0, -5));

                if (selected != head) PhysicsCalc(new Vector3(0, -5));
                else velocity = Vector2.zero;


                // Colors TODO: fix this
                if (rightLocked)
                {
                        rightCont.GetComponent<SpriteRenderer>().color = Color.black;
                }
                else
                {
                        rightCont.GetComponent<SpriteRenderer>().color = Color.white;
                }
                if (leftLocked)
                {
                        leftCont.GetComponent<SpriteRenderer>().color = Color.black;
                }
                else
                {
                        leftCont.GetComponent<SpriteRenderer>().color = Color.white;
                }

                // Draw arm
                DrawArm(leftHand.transform.position, head.transform.position - new Vector3(0.2f, 0), leftSegments);

                DrawArm(rightHand.transform.position, head.transform.position + new Vector3(0.2f, 0), rightSegments);
        }

        void DrawArm(Vector2 first, Vector2 second, GameObject[] segments)
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
                                if (left.y < right.y) {
                                        top = right;
                                        bottom = left;
                                }
                                else
                                {
                                        top = left;
                                        bottom = right;
                                }
                                float droop = (armLength - (top.y - bottom.y))/ 2;
                                float yPos;
                                // Placing segments
                                for (int i = 0; i < armSegments; i++) {
                                        // TODO: clean this up
                                        yPos = Mathf.Abs(((float) i + 1) / ((float) armSegments + 1) * armLength - (top.y - bottom.y) - droop) - droop;
                                        segments[i].transform.position = new Vector2(left.x, bottom.y + yPos);
                                }
                                
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
                        }
                }
                // Stretched arm
                else
                {
                        Vector2 dif = right - left;
                        if (dif.magnitude > armMaxLength) {
                                dif = armMaxLength * dif.normalized;
                        }
                        // Placing arm segments
                        for (int i = 0; i < armSegments; i++)
                        {
                                segments[i].transform.position = left + dif / (armSegments + 1) * (i + 1);
                        }
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
                        return ( -b + Mathf.Sqrt(b * b - 4 * a * c) ) / (2 * a);
        }

        void PhysicsCalc(Vector3 external)
        {
                acceleration = external * 3;

                // Adding in acceleration from hands
                if (rightLocked) {
                        Vector3 displacement = rightHand.transform.position - (head.transform.position + socketPos);
                        float pull = Mathf.Max(displacement.magnitude - armLength, 0);

                        acceleration += 60 * displacement.normalized * pull;
                }
                if (leftLocked)
                {
                        Vector3 displacement = leftHand.transform.position - (head.transform.position - socketPos);
                        float pull = Mathf.Max(displacement.magnitude - armLength, 0);

                        acceleration += 60 * displacement.normalized * pull;
                }

                // FInd potential position
                Vector3 potentialPosition = head.transform.position + Time.deltaTime * (velocity + 0.5f * Time.deltaTime * acceleration);
                velocity += acceleration * Time.deltaTime;
                velocity *= Mathf.Pow(0.8f, Time.deltaTime);
                if (leftLocked)
                {
                        velocity *= Mathf.Pow(0.4f, Time.deltaTime);
                }
                if (rightLocked)
                {       
                        velocity *= Mathf.Pow(0.4f, Time.deltaTime);
                }
                // First check horizontal position
                if (HasTile(new Vector3(potentialPosition.x, head.transform.position.y))) {
                        if (head.transform.position.x < potentialPosition.x)
                        {
                                potentialPosition.x = Mathf.Floor(potentialPosition.x * 2) / 2 - 0.01f; ;
                        }
                        else
                        {
                                potentialPosition.x = Mathf.Ceil(potentialPosition.x * 2) / 2 + 0.01f; ;

                        }
                        velocity.x = 0;
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
                }
                head.transform.position = potentialPosition;


        }

        bool HasTile(Vector3 pos) {
                return tiles.HasTile(new Vector3Int((int)Mathf.Floor(pos.x * 2), (int) Mathf.Floor(pos.y * 2)));
        }
}