using System.Collections; // Necesario para IEnumerator
using HarmonyLib;
using UnityEngine;

namespace fortunatesonpeak;

[HarmonyPatch(typeof(GameOverHandler), "LoadAirport")]
public static class EndScreenPatch
{
    [HarmonyPostfix]
    public static void Prefix()
    {
        FortunateSonPeakPlugin.Log.LogInfo(
            "¡Parche activado! Deteniendo la música de 'Fortunate Son'..."
        );

        if (AudioHandler.CurrentAudioPlayer != null)
        {
            var audioSource = AudioHandler.CurrentAudioPlayer.GetComponent<AudioSource>();

            if (audioSource != null)
            {
                if (FortunateSonPeakPlugin.Instance != null)
                {
                    // Iniciar la corrutina de fade out usando la instancia del plugin
                    FortunateSonPeakPlugin.Instance.StartCoroutine(
                        AudioHandler.StopAudioWithFade(audioSource, 2.0f)
                    );
                    FortunateSonPeakPlugin.Log.LogInfo(
                        "¡Iniciando fade out de la música de 'Fortunate Son'!"
                    );
                }
                else
                {
                    FortunateSonPeakPlugin.Log.LogError(
                        "La instancia de FortunateSonPeakPlugin no está disponible para iniciar el fade out. Deteniendo el audio inmediatamente."
                    );
                    audioSource.Stop();
                    UnityEngine.Object.Destroy(AudioHandler.CurrentAudioPlayer);
                    AudioHandler.CurrentAudioPlayer = null;
                }
            }
            else
            {
                FortunateSonPeakPlugin.Log.LogWarning(
                    "No se encontró AudioSource en currentAudioPlayer. Destruyendo inmediatamente."
                );
                UnityEngine.Object.Destroy(AudioHandler.CurrentAudioPlayer);
                AudioHandler.CurrentAudioPlayer = null;
            }
        }
        else
        {
            FortunateSonPeakPlugin.Log.LogWarning(
                "No hay un reproductor de audio activo para detener la música."
            );
        }
    }
}
