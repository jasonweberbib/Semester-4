using Unity.Netcode;
using UnityEngine;

public class PillarScoreBonus : NetworkBehaviour
{
    // Die NetworkObjectId des Spielers, der diesen Pillar gespawnt hat.
    // NetworkVariable wird verwendet, damit diese ID auf allen Clients verfügbar ist.
    public NetworkVariable<ulong> spawnerPlayerId = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone);

    private NetPlayer spawnerNetPlayer; // Lokale Referenz zum NetPlayer-Skript des Spawners
    private float scoreBonusTimer = 0f;
    private const float bonusInterval = 2f; // Alle 2 Sekunden
    private const int bonusAmount = 2;     // 1 Punkt Bonus

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Immer den Event-Handler hinzufügen, sobald das Objekt gespawnt ist.
        // Er wird auf Server und Clients aufgerufen.
        spawnerPlayerId.OnValueChanged += OnSpawnerIdChanged;

        // Wenn die ID bereits beim Spawnen gesetzt ist (was der Fall sein sollte, da NetPlayer sie vor dem Spawn auf dem Server setzt),
        // können wir versuchen, den Spawner direkt zu finden.
        // Dies ist besonders wichtig für Clients, die beim ersten OnNetworkSpawn die korrekte ID schon repliziert bekommen haben.
        if (spawnerPlayerId.Value != 0)
        {
            // Direkt versuchen, den Spawner zu finden, ohne auf den Event zu warten
            FindSpawnerPlayer(spawnerPlayerId.Value);
        }
    }

    // Callback, wenn die Spawner-ID gesetzt oder geändert wird
    private void OnSpawnerIdChanged(ulong oldId, ulong newId)
    {
        if (newId != 0) // Nur fortfahren, wenn eine gültige ID gesetzt wurde
        {
            FindSpawnerPlayer(newId);
        }
    }

    // Hilfsmethode zum Finden des Spawner-Spielers
    private void FindSpawnerPlayer(ulong idToFind)
    {
        if (spawnerNetPlayer != null) return; // Schon gefunden, nichts tun

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idToFind, out NetworkObject netObj))
        {
            spawnerNetPlayer = netObj.GetComponent<NetPlayer>();
            if (spawnerNetPlayer == null)
            {
                Debug.LogError($"PillarScoreBonus: Could not find NetPlayer component on NetworkObject with ID {idToFind}");
            }
        }
        else
        {
            // Dies kann passieren, wenn das Objekt noch nicht im SpawnManager ist (z.B. bei Clients kurz nach dem Spawn)
            // Wir verlassen uns darauf, dass OnValueChanged erneut triggert oder Update später den spawnerNetPlayer findet,
            // falls das Objekt zu einem späteren Zeitpunkt verfügbar wird.
            Debug.LogWarning($"PillarScoreBonus: NetworkObject with ID {idToFind} not yet available in SpawnManager.");
        }
    }


    void Update()
    {
        // Nur der Server soll den Score erhöhen
        if (!IsServer) return;

        // Versuche, den SpawnerNetPlayer zu finden, falls er noch null ist
        if (spawnerNetPlayer == null && spawnerPlayerId.Value != 0)
        {
            FindSpawnerPlayer(spawnerPlayerId.Value);
        }

        if (spawnerNetPlayer != null)
        {
            scoreBonusTimer += Time.deltaTime;
            if (scoreBonusTimer >= bonusInterval)
            {
                spawnerNetPlayer.score.Value += bonusAmount; // Erhöhe den Score des Spawners
                scoreBonusTimer -= bonusInterval; // Timer zurücksetzen (damit Reste nicht verloren gehen)
                // Debug.Log($"Pillar granted {bonusAmount} bonus to player {spawnerNetPlayer.NetworkObjectId}. New Score: {spawnerNetPlayer.score.Value}");
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        // Sicherstellen, dass das Event abgemeldet wird, wenn das Objekt despawned wird
        spawnerPlayerId.OnValueChanged -= OnSpawnerIdChanged;
    }
}