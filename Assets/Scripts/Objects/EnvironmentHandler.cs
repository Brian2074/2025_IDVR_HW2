using UnityEngine;
using Newtonsoft.Json.Linq;

public class EnvironmentHandler : MonoBehaviour
{
    [Header("References")]
    public Light directionalLight;
    public ParticleSystem magicParticlePrefab;
    
    private ParticleSystem currentParticle;

    void Start()
    {
        // 訂閱 Realtime API 的 Function Call 事件
        RealtimeAPIWrapper.OnFunctionCallReceived += HandleFunctionCall;
    }

    void OnDestroy()
    {
        RealtimeAPIWrapper.OnFunctionCallReceived -= HandleFunctionCall;
    }

    private void HandleFunctionCall(string functionName, string arguments)
    {
        if (functionName == "control_environment")
        {
            try
            {
                var json = JObject.Parse(arguments);
                string action = json["action"]?.ToString();
                HandleEnvironmentAction(action);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EnvironmentHandler] Error parsing arguments: {e.Message}");
            }
        }
    }

    public void HandleEnvironmentAction(string action)
    {
        Debug.Log($"[EnvironmentHandler] Received action: {action}");
        
        switch (action)
        {
            case "change_light_red":
                ChangeLightColor(Color.red);
                break;
            case "change_light_blue":
                ChangeLightColor(Color.blue);
                break;
            case "change_light_normal":
                ChangeLightColor(Color.white);
                break;
            case "spawn_magic":
                SpawnMagic();
                break;
            case "clear_magic":
                ClearMagic();
                break;
            case "none":
            default:
                break;
        }
    }

    private void ChangeLightColor(Color color)
    {
        if (directionalLight != null)
        {
            directionalLight.color = color;
            Debug.Log($"[EnvironmentHandler] Light color changed to {color}");
        }
    }

    private void SpawnMagic()
    {
        if (magicParticlePrefab != null && currentParticle == null)
        {
            // Spawn particles directly in front of the user's view
            Vector3 spawnPos = new Vector3(0, 1.5f, 2f); 
            if (Camera.main != null)
            {
                // 1.5 meters in front of the camera
                spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 1.5f;
            }

            currentParticle = Instantiate(magicParticlePrefab, spawnPos, Quaternion.identity);
            Debug.Log("[EnvironmentHandler] Magic spawned!");
        }
    }

    private void ClearMagic()
    {
        if (currentParticle != null)
        {
            Destroy(currentParticle.gameObject);
            currentParticle = null;
            Debug.Log("[EnvironmentHandler] Magic cleared!");
        }
    }
}
