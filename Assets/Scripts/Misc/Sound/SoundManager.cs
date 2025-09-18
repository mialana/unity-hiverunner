using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [Serializable]
    public class SoundFeedback
    {
        public SoundEffectType soundType;
        public AudioClip soundClip;
    }

    public List<SoundFeedback> soundFeedbacks = new List<SoundFeedback>();

    public AudioSource audioSource;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    { 
    }

    // Update is called once per frame
    void Update()
    { 
        
        
    }
}
