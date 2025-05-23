using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.Animations;
using UnityEngine;

public class NetPlayer : NetworkBehaviour
{
    public NetworkVariable<int> score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public int pillerscore = 25;
    private float scoreAccumulator = 0f;
    public GameObject piller;
    public GameObject ball;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner || !IsSpawned) return;

        if(score.Value < 0)
        {
            score.Value = 0;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3(x, 0, z);
        if (movement.magnitude > 0)
        {
            
            transform.position += movement * Time.deltaTime;
            MovingServerRPC(transform.position);
        }
        if ((scoreAccumulator += Time.deltaTime) >= 1.0f)
        {
            score.Value += 1;
            scoreAccumulator -= 1.0f;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            CreateObjectServerRpc();
        }

    }

    // [ServerRpc (RequierOwnership = false)]
    [ServerRpc]
    public void CreateObjectServerRpc()
    {
        if(score.Value >= pillerscore)
        {
            GameObject obj = Instantiate(piller);
            obj.GetComponent<NetworkObject>().Spawn(true);
            score.Value -= 25;
        }
        if(score.Value >= 75)
        {
            GameObject obj = Instantiate(ball);
            obj.GetComponent<NetworkObject>().Spawn(true);
            score.Value -= 75;
        }
    }

    [ServerRpc]
    void MovingServerRPC(Vector3 position)
    {
        // Die Position wird nun direkt übernommen (als absolute Position)
        transform.position = position;
        MovingClientRPC(transform.position);
    }

    [ClientRpc]
    void MovingClientRPC(Vector3 position)
    {
        if (IsOwner) return;
        transform.position = position;
    }
}