using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
//using UnityEngine.Rendering.PostProcessing;

public class GetDamageEffect : MonoBehaviour
{
    public float intensity = 0;
    public float maxDamageIntensity = 0.8f;
    public float fadeSpeed = 2f;
    public float _Effectduration = 0f;
    Volume _volume;
    Vignette _vignette;

    private void Awake()
    {
        _volume = GetComponent<Volume>();
        _volume.profile.TryGet(out _vignette);
       

    }
    private void Update()
    {
        
       
        if(_vignette == null) return;
        _vignette.intensity.value = Mathf.Lerp(_vignette.intensity.value, intensity, Time.deltaTime * fadeSpeed);
    }

    public IEnumerator DamageEffect()
    {
        intensity = maxDamageIntensity;
        yield return new WaitForSeconds(_Effectduration);
        intensity = 0;
    }
}
