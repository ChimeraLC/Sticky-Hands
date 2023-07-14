using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class HandController : MonoBehaviour
{
        public GameObject head;

        public Vector3 velocity;
        public Vector3 acceleration;
        public Vector3 socketPos;
        public float armLength;
        public float armMaxLength;
        public Sprite[] sprites;
        public SpriteRenderer handSR;

        public GameObject handMain;
        public GameController gameController;
        public Hand hand;
        public TextMeshProUGUI textTop;
        public UnityEngine.UI.Image radialProgress;
        public KeyCode handKey;

        // Animating arm after detatchment
        private float state = 0;
        private Vector3 fakeHand;
        private Vector3 fakeVel;



        // Lifetime
        float lifetime = 0f;
        float lifetimeMax = Mathf.Infinity;

        // Whether the hand is gripping or not
        public bool handState = false;
        void Start()
        {
                handSR = handMain.GetComponent<SpriteRenderer>();
                textTop.text = handKey.ToString();

        }
        public void UpdateText() {
                textTop.text = handKey.ToString();
         }
        // Update is called once per frame
        void Update()
        {
                // Update sprites
                if (handState)
                {
                        handSR.sprite = sprites[0];
                }
                else
                {
                        handSR.sprite = sprites[1];

                }
                if (state > 0) {
                        state += Time.deltaTime;
                        PhysicsCalcFake(new Vector2(0, -5));
                        gameController.DrawArm(transform.position, fakeHand, hand.handArm);
                        if (state > 5) {
                                foreach (GameObject segment in hand.handArm) {
                                        Destroy(segment);
                                }
                                Destroy(gameObject);
                        }
                }
        }

        public void Signal() {
                gameController.Signal(hand);
                state = 1;
                handSR.sortingOrder = -1;
                fakeHand = gameController.head.transform.position;
                velocity = Vector3.zero;
                Destroy(textTop);
                Destroy(radialProgress);
        }

        public void SignalNoResponse()
        {
                state = 6;
                fakeHand = gameController.head.transform.position;
                velocity = Vector3.zero;
                Destroy(textTop);
                Destroy(radialProgress);
        }
        // Updates radial loading bar
        public void UpdateRadial(float percentage) {
                radialProgress.fillAmount = percentage;
        }

        // Calculates the physics for an unlocked hand, with applied acceleration 'external'
        public void PhysicsCalc(Vector3 external) {
                // Calculate each frame's acceleration independantly
                acceleration = external;

                // Distance from other hand
                Vector3 displacement = head.transform.position + socketPos - transform.position;

                // Pull if the arm is stretched
                float pull = Mathf.Max(displacement.magnitude - armLength + 1, 0);

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
                velocity *= Mathf.Pow(0.2f, Time.deltaTime);
        }
        // Calculates the physics for an unlocked hand, with applied acceleration 'external'
        public void PhysicsCalcFake(Vector3 external)
        {
                // Calculate each frame's acceleration independantly
                acceleration = external;

                // Distance from other hand
                Vector3 displacement = transform.position - fakeHand;

                // Pull if the arm is stretched
                float pull = Mathf.Max(displacement.magnitude - armLength, 0);

                acceleration += 40 * displacement.normalized * pull;

                fakeHand += Time.deltaTime * (velocity + 0.5f * acceleration * Time.deltaTime);

                // Acceleration
                velocity += acceleration * Time.deltaTime;

                // Bounding (Potentially not needed?)
                // TODO: fix this
                displacement = fakeHand - transform.position;
                if (displacement.magnitude > armMaxLength)
                {
                        fakeHand = transform.position +  displacement.normalized * armMaxLength;
                        // Removing velocity in direction of arm
                        //velocity -= Vector2.Dot(velocity, displacement) / displacement.sqrMagnitude * displacement;
                }



                // Air resistance
                velocity *= Mathf.Pow(0.6f, Time.deltaTime);
        }
        public void SetAngle(float ang) {
                handMain.transform.eulerAngles = Vector3.forward * ang;
        }
}
