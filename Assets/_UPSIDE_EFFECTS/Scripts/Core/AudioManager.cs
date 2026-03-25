using UnityEngine;
using UpsideEffects.Effects;

namespace UpsideEffects.Core
{
    public sealed class AudioManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AudioSource masterLoopSource;
        [SerializeField] private AudioIntensityDriver audioIntensityDriver;

        private void Awake()
        {
            if (masterLoopSource == null)
            {
                masterLoopSource = GetComponent<AudioSource>();
            }

            if (audioIntensityDriver == null)
            {
                audioIntensityDriver = GetComponent<AudioIntensityDriver>();
            }
        }

        public AudioSource MasterLoopSource => masterLoopSource;
        public AudioIntensityDriver AudioIntensityDriver => audioIntensityDriver;
    }
}
