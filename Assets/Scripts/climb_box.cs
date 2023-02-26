using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class climb_box : MonoBehaviour
{
    GameObject player;
    
    private void Start(){
        player = GameObject.Find("Player");
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject==player){
            player.GetComponent<PlayerController>().setClimb(true);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject==player){
            player.GetComponent<PlayerController>().setClimb(false);
        }
    }
}
