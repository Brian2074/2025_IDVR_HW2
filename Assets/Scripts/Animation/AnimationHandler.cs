using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationHandler : MonoBehaviour
{
    private AudioPlayer audioPlayer;
    private Animator animator;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        audioPlayer = FindObjectOfType<AudioPlayer>();
    }

    void Update()
    {
        // TODO: Handle the animation transitions based on your logics
        
        if(audioPlayer.IsAudioPlaying())
            animator.SetBool("isTalking", true);
        else
            animator.SetBool("isTalking", false);
    }
}
