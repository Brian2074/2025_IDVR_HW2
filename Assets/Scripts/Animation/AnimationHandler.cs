using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

public class AnimationHandler : MonoBehaviour
{
    private AudioPlayer audioPlayer;
    private Animator animator;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        audioPlayer = FindObjectOfType<AudioPlayer>();
        
        // 訂閱 Function Call 事件
        RealtimeAPIWrapper.OnFunctionCallReceived += HandleFunctionCall;
    }

    private void OnDestroy()
    {
        RealtimeAPIWrapper.OnFunctionCallReceived -= HandleFunctionCall;
    }

    void Update()
    {
        // 取得當前動畫狀態資訊 (Layer 0)
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // 檢查是否正在播放特殊動作 (假設你的 State 名稱就叫 Dancing, Clapping, Waving)
        // 注意：這裡的名稱必須跟 Animator Controller 裡的 State 名稱一致
        bool isPerformingAction = stateInfo.IsName("Dancing") || 
                                  stateInfo.IsName("Clapping") || 
                                  stateInfo.IsName("Wave"); // 修正：你的截圖裡是 Wave，不是 Waving

        // 只有在沒有做特殊動作時，才允許切換到說話狀態
        if (!isPerformingAction)
        {
            if(audioPlayer.IsAudioPlaying())
                animator.SetBool("isTalking", true);
            else
                animator.SetBool("isTalking", false);
        }
        else
        {
            // 如果正在做動作，強制把 isTalking 設為 false，避免 Animator 在動作和說話間跳來跳去
            // (除非你有設定 Avatar Mask 讓說話只影響嘴巴/頭部)
            animator.SetBool("isTalking", false);
        }
    }

    private void HandleFunctionCall(string functionName, string arguments)
    {
        if (functionName == "trigger_animation")
        {
            try
            {
                var json = JObject.Parse(arguments);
                string animName = json["animation_name"]?.ToString();
                PlayAnimation(animName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error parsing animation arguments: {e.Message}");
            }
        }
    }

    // 公開方法供 BackgroundReasoningManager 呼叫
    public void PlayAnimation(string animName)
    {
        Debug.Log($"[AnimationHandler] Requesting animation: {animName}");
        if (!string.IsNullOrEmpty(animName))
        {
            animator.SetTrigger(animName);
            
            // 除錯：檢查 Animator 是否真的有這個參數
            bool hasParam = false;
            foreach(var param in animator.parameters)
            {
                if(param.name == animName && param.type == AnimatorControllerParameterType.Trigger)
                {
                    hasParam = true;
                    break;
                }
            }
            
            if(!hasParam)
            {
                Debug.LogError($"[AnimationHandler] Animator does NOT have a Trigger named '{animName}'! Please check your Animator Controller.");
            }
        }
    }
}
