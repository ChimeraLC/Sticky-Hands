using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomController : MonoBehaviour
{

        public GameObject roomCam;

        private void OnTriggerEnter2D(Collider2D other)
        {
                if (other.CompareTag("Player") && !other.isTrigger)
                {
                        roomCam.SetActive(true);
                        Debug.Log("Enter");
                }

        }
        private void OnTriggerExit2D(Collider2D other)
        {
                if (other.CompareTag("Player") && !other.isTrigger)
                {
                        roomCam.SetActive(false);
                }
        }
}
