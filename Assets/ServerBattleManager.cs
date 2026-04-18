using Assets.Script.Shared;
using UnityEngine;

public class ServerBattleManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private BattleConfig config;

    private BattleState state;


    void Awake()
    {
        
    }

    private void Initialize()
    {
        state = new BattleState
        {
            Phase = BattlePhase.WaitingForPlayers,
            MaxDeploymentTime = config.DeploymentTime,
            TimeRemaining = config.DeploymentTime,
            MaxUnitsPerPlayer = config.MaxUnitsPerPlayer,
            MinUnitsPerPlayer = config.MinUnitsPerPlayer,
        };
        Debug.Log("[Server] Battle Manager initialized");
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
