using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomControllerChanged : MonoBehaviour
{

        public GameObject roomCam;
        public GameController gameController;
        public int type = 1;

        private void OnTriggerEnter2D(Collider2D other)
        {
                if (other.CompareTag("Player") && !other.isTrigger)
                {
                        gameController.currentCam.SetActive(false);
                        roomCam.SetActive(true);
                        Debug.Log("Enter");
                        gameController.currentCam = roomCam;
                }
                if (type == 1)
                {

                        gameController.started = true;
                }
                else if (type == 2) {
                        gameController.timerUI.enabled = true;
                }
                else {
                        gameController.FinishSignal();
                }
        }
        /*
        private void OnTriggerExit2D(Collider2D other)
        {
                if (other.CompareTag("Player") && !other.isTrigger)
                {
                        roomCam.SetActive(false);
                }
        }
        */
}
