using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public bool IsClient = false;
    //private PlayerNet myPlayer;
    // Start is called before the first frame update
    void Start()
    {
        if (!IsClient)
        {
            NetworkManager.Singleton.StartHost();
        }
        else
        {
            NetworkManager.Singleton.StartClient();
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
