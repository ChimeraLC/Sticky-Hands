using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomController : MonoBehaviour
{

        public GameObject roomCam;
        public GameController gameController;

        private void OnTriggerEnter2D(Collider2D other)
        {
                if (other.CompareTag("Player") && !other.isTrigger)
                {
                        gameController.currentCam.SetActive(false);
                        roomCam.SetActive(true);
                        Debug.Log("Enter");
                        gameController.currentCam = roomCam;
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
