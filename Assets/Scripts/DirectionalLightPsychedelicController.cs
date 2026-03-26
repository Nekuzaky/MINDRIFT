using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Mindrift.Core;

[DefaultExecutionOrder(-8500)]
public sealed class DirectionalLightPsychedelicController : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool autoFindDirectionalLights = true;
    [SerializeField] private bool createFallbackLightsIfMissing = true;
    [SerializeField] private bool ensureTriDirectionalRig = true;

    [Header("RGB Cycle")]
    [SerializeField] private float hueCycleSpeed = 0.28f;
    [SerializeField] private float hueOffsetPerLight = 0.27f;
    [SerializeField] [Range(0f, 1f)] private float saturation = 1f;
    [SerializeField] [Range(0f, 1f)] private float value = 1f;
    [SerializeField] [Range(0f, 2f)] private float intensityPulseStrength = 0.55f;
    [SerializeField] private float intensityPulseSpeed = 2.8f;

    [Header("Motion")]
    [SerializeField] private bool rotateDirectionalLights = true;
    [SerializeField] private Vector3 lightRotationSpeedEuler = new(5f, 12f, 0f);

    [Header("Background")]
    [SerializeField] private bool driveAmbientAndFog = true;
    [SerializeField] [Range(0f, 2f)] private float ambientIntensity = 1.25f;
    [SerializeField] [Range(0f, 1f)] private float ambientValue = 0.55f;
    [SerializeField] [Range(0f, 1f)] private float fogValue = 0.45f;
    [SerializeField] [Range(0f, 1f)] private float fogSaturation = 1f;
    [SerializeField] private bool forceHdrpSolidColorBackground = true;
    [SerializeField] [Range(0f, 1f)] private float backgroundSaturation = 1f;
    [SerializeField] [Range(0f, 1f)] private float backgroundValue = 0.8f;
    [SerializeField] private float backgroundHueSpeedMultiplier = 1.35f;

    [Header("Music Sync")]
    [SerializeField] private bool syncWithMusic = true;
    [SerializeField] private bool syncBackgroundWithMusic = true;
    [SerializeField] private bool autoFindMusicSource = true;
    [SerializeField] private bool useAudioListenerFallback = true;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private FFTWindow fftWindow = FFTWindow.BlackmanHarris;
    [SerializeField, Range(32, 1024)] private int spectrumSampleCount = 128;
    [SerializeField, Range(0f, 1f)] private float bassWeight = 0.65f;
    [SerializeField, Range(0f, 1f)] private float midWeight = 0.25f;
    [SerializeField, Range(0f, 1f)] private float highWeight = 0.1f;
    [SerializeField] private float spectrumGain = 46f;
    [SerializeField] private float attackSpeed = 12f;
    [SerializeField] private float releaseSpeed = 4f;
    [SerializeField, Range(0f, 1.5f)] private float musicPulseBoost = 0.42f;
    [SerializeField, Range(0f, 0.4f)] private float backgroundMusicValueBoost = 0.12f;

    [Header("Targets")]
    [SerializeField] private List<Light> directionalLights = new();

    private readonly List<float> baseIntensities = new();
    private float nextRefreshTime;
    private float nextAudioRefreshTime;
    private Camera cachedMainCamera;
    private HDAdditionalCameraData cachedHdCameraData;
    private float[] spectrumBuffer;
    private float smoothedMusicEnergy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (FindFirstObjectByType<DirectionalLightPsychedelicController>() != null)
        {
            return;
        }

        GameObject host = GameObject.Find("Lighting");
        if (host == null)
        {
            host = new GameObject("Lighting");
        }

        host.AddComponent<DirectionalLightPsychedelicController>();
    }

    private void Awake()
    {
        ValidateSpectrumConfig();
        TryResolveMusicSource();
        CacheMainCamera();
        RefreshDirectionalLights();
    }

    private void OnEnable()
    {
        ValidateSpectrumConfig();
        TryResolveMusicSource();
        CacheMainCamera();
        RefreshDirectionalLights();
    }

    private void LateUpdate()
    {
        if ((directionalLights.Count == 0 || Time.time >= nextRefreshTime) && autoFindDirectionalLights)
        {
            RefreshDirectionalLights();
            nextRefreshTime = Time.time + 2f;
        }

        if (autoFindMusicSource && (musicSource == null || Time.time >= nextAudioRefreshTime))
        {
            TryResolveMusicSource();
            nextAudioRefreshTime = Time.time + 2f;
        }

        float musicEnergy = ComputeMusicEnergy();

        if (directionalLights.Count == 0)
        {
            ApplyBackground(Time.time, musicEnergy);
            return;
        }

        float time = Time.time;
        float safeSaturation = Mathf.Clamp01(saturation);
        float safeValue = Mathf.Clamp01(value);

        for (int i = 0; i < directionalLights.Count; i++)
        {
            Light lightComponent = directionalLights[i];
            if (lightComponent == null)
            {
                continue;
            }

            float hue = Mathf.Repeat(
                time * hueCycleSpeed
                + i * hueOffsetPerLight
                + Mathf.Sin(time * 0.8f + i * 1.31f) * 0.08f,
                1f
            );

            Color rgb = Color.HSVToRGB(hue, safeSaturation, safeValue);
            lightComponent.color = rgb;
            lightComponent.useColorTemperature = false;

            float baseIntensity = baseIntensities.Count > i ? baseIntensities[i] : Mathf.Max(1f, lightComponent.intensity);
            float pulse = 1f + Mathf.Sin(time * intensityPulseSpeed + i * 1.17f) * intensityPulseStrength;
            if (syncWithMusic)
            {
                pulse *= 1f + musicEnergy * musicPulseBoost;
            }

            lightComponent.intensity = Mathf.Max(0f, baseIntensity * pulse);

            if (rotateDirectionalLights && lightRotationSpeedEuler.sqrMagnitude > 0.0001f)
            {
                lightComponent.transform.Rotate(lightRotationSpeedEuler * Time.deltaTime, Space.Self);
            }
        }

        ApplyBackground(time, musicEnergy);
    }

    [ContextMenu("Refresh Directional Lights")]
    public void RefreshDirectionalLights()
    {
        directionalLights.RemoveAll(lightComponent => lightComponent == null);

        if (autoFindDirectionalLights)
        {
            directionalLights.Clear();
            Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type == LightType.Directional)
                {
                    directionalLights.Add(lights[i]);
                }
            }
        }

        if (directionalLights.Count == 0 && createFallbackLightsIfMissing)
        {
            CreateFallbackLights();
        }
        else if (ensureTriDirectionalRig && directionalLights.Count > 0 && directionalLights.Count < 3)
        {
            CreateSupplementalLights(3 - directionalLights.Count);
        }

        baseIntensities.Clear();
        for (int i = 0; i < directionalLights.Count; i++)
        {
            Light lightComponent = directionalLights[i];
            float baseIntensity = lightComponent != null && lightComponent.intensity > 0.001f
                ? lightComponent.intensity
                : 35000f;
            baseIntensities.Add(baseIntensity);
        }
    }

    private void CreateFallbackLights()
    {
        CreateSupplementalLights(3);
    }

    private void CreateSupplementalLights(int count)
    {
        count = Mathf.Max(0, count);
        for (int i = 0; i < count; i++)
        {
            int index = directionalLights.Count + 1;
            GameObject lightObject = new($"RGB Directional {index}");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.rotation = Quaternion.Euler(20f + i * 12f, index * 120f, 0f);

            Light lightComponent = lightObject.AddComponent<Light>();
            lightComponent.type = LightType.Directional;
            lightComponent.intensity = 45000f;
            lightComponent.shadows = LightShadows.None;
            lightComponent.useColorTemperature = false;
            directionalLights.Add(lightComponent);
        }
    }

    private void ApplyBackground(float time, float musicEnergy)
    {
        if (driveAmbientAndFog)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientIntensity = ambientIntensity;

            Color sky = Color.HSVToRGB(Mathf.Repeat(time * hueCycleSpeed * 0.72f, 1f), 0.95f, Mathf.Clamp01(ambientValue));
            Color equator = Color.HSVToRGB(Mathf.Repeat(time * hueCycleSpeed * 0.72f + 0.33f, 1f), 0.95f, Mathf.Clamp01(ambientValue * 0.8f));
            Color ground = Color.HSVToRGB(Mathf.Repeat(time * hueCycleSpeed * 0.72f + 0.66f, 1f), 0.95f, Mathf.Clamp01(ambientValue * 0.65f));

            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = equator;
            RenderSettings.ambientGroundColor = ground;

            if (RenderSettings.fog)
            {
                Color fog = Color.HSVToRGB(
                    Mathf.Repeat(time * hueCycleSpeed * 0.92f + 0.18f, 1f),
                    Mathf.Clamp01(fogSaturation),
                    Mathf.Clamp01(fogValue)
                );
                RenderSettings.fogColor = fog;
            }
        }

        if (!forceHdrpSolidColorBackground)
        {
            return;
        }

        CacheMainCamera();
        if (cachedMainCamera == null)
        {
            return;
        }

        Color background = Color.HSVToRGB(
            Mathf.Repeat(time * hueCycleSpeed * backgroundHueSpeedMultiplier + 0.12f, 1f),
            Mathf.Clamp01(backgroundSaturation),
            Mathf.Clamp01(backgroundValue + (syncBackgroundWithMusic ? musicEnergy * backgroundMusicValueBoost : 0f))
        );

        cachedMainCamera.clearFlags = CameraClearFlags.SolidColor;
        cachedMainCamera.backgroundColor = background;

        if (cachedHdCameraData != null)
        {
            cachedHdCameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            cachedHdCameraData.backgroundColorHDR = background;
        }
    }

    private float ComputeMusicEnergy()
    {
        if (!syncWithMusic && !syncBackgroundWithMusic)
        {
            smoothedMusicEnergy = Mathf.MoveTowards(smoothedMusicEnergy, 0f, Time.unscaledDeltaTime * releaseSpeed);
            return smoothedMusicEnergy;
        }

        int sampleCount = Mathf.ClosestPowerOfTwo(Mathf.Clamp(spectrumSampleCount, 32, 1024));
        if (spectrumBuffer == null || spectrumBuffer.Length != sampleCount)
        {
            spectrumBuffer = new float[sampleCount];
        }

        bool hasSpectrum = false;
        if (musicSource != null)
        {
            musicSource.GetSpectrumData(spectrumBuffer, 0, fftWindow);
            hasSpectrum = true;
        }
        else if (useAudioListenerFallback)
        {
            AudioListener.GetSpectrumData(spectrumBuffer, 0, fftWindow);
            hasSpectrum = true;
        }

        if (!hasSpectrum)
        {
            smoothedMusicEnergy = Mathf.MoveTowards(smoothedMusicEnergy, 0f, Time.unscaledDeltaTime * releaseSpeed);
            return smoothedMusicEnergy;
        }

        float lowBand = 0f;
        float midBand = 0f;
        float highBand = 0f;

        int lowEnd = Mathf.Max(1, Mathf.RoundToInt(sampleCount * 0.12f));
        int midEnd = Mathf.Max(lowEnd + 1, Mathf.RoundToInt(sampleCount * 0.45f));

        for (int i = 0; i < sampleCount; i++)
        {
            float value = spectrumBuffer[i];
            if (i < lowEnd)
            {
                lowBand += value;
            }
            else if (i < midEnd)
            {
                midBand += value;
            }
            else
            {
                highBand += value;
            }
        }

        lowBand /= lowEnd;
        midBand /= Mathf.Max(1, midEnd - lowEnd);
        highBand /= Mathf.Max(1, sampleCount - midEnd);

        float weightSum = Mathf.Max(0.0001f, bassWeight + midWeight + highWeight);
        float weightedEnergy = (lowBand * bassWeight + midBand * midWeight + highBand * highWeight) / weightSum;
        float target = Mathf.Clamp01(weightedEnergy * spectrumGain);

        float smoothSpeed = target >= smoothedMusicEnergy ? attackSpeed : releaseSpeed;
        smoothedMusicEnergy = Mathf.Lerp(smoothedMusicEnergy, target, Time.unscaledDeltaTime * Mathf.Max(0.01f, smoothSpeed));
        return smoothedMusicEnergy;
    }

    private void TryResolveMusicSource()
    {
        if (musicSource != null)
        {
            return;
        }

        AudioManager manager = FindFirstObjectByType<AudioManager>();
        if (manager != null && manager.MasterLoopSource != null)
        {
            musicSource = manager.MasterLoopSource;
            return;
        }

        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null)
            {
                continue;
            }

            if (source.clip != null && (source.isPlaying || source.loop))
            {
                musicSource = source;
                return;
            }
        }
    }

    private void OnValidate()
    {
        ValidateSpectrumConfig();
    }

    private void ValidateSpectrumConfig()
    {
        spectrumSampleCount = Mathf.ClosestPowerOfTwo(Mathf.Clamp(spectrumSampleCount, 32, 1024));
        attackSpeed = Mathf.Max(0.01f, attackSpeed);
        releaseSpeed = Mathf.Max(0.01f, releaseSpeed);
        spectrumGain = Mathf.Max(0f, spectrumGain);
    }

    private void CacheMainCamera()
    {
        if (cachedMainCamera == null)
        {
            cachedMainCamera = Camera.main;
            if (cachedMainCamera == null)
            {
                cachedMainCamera = FindFirstObjectByType<Camera>();
            }
        }

        if (cachedMainCamera != null && cachedHdCameraData == null)
        {
            cachedMainCamera.TryGetComponent(out cachedHdCameraData);
        }
    }
}
