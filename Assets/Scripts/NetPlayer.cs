using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetPlayer : NetworkBehaviour
{
    public NetworkVariable<int> score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private float scoreAccumulator = 0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(!IsOwner || !IsSpawned) return;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3 (x,0,z);
        if (movement.magnitude > 0)
        {
            transform.position += movement * Time.deltaTime;
            MovingServerRPC(movement);
        }
        if ((scoreAccumulator += Time.deltaTime) >= 1.0f)
        {
            score.Value += 1;
            scoreAccumulator -= 1.0f;
        }
    }

    [ServerRpc]
    void MovingServerRPC(Vector3 position)
    {
        transform.position = position;
    }

    [ClientRpc]
    void MovingClientRPC(Vector3 position)
    {
        if (IsOwner) return;
        transform.position = position;
    }
}
