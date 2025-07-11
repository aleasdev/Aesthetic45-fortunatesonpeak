using System.Collections; // Necesario para IEnumerator
using System.Reflection; // Necesario para BindingFlags
using HarmonyLib;
using UnityEngine;
using UnityEngine.Audio; // Necesario para AudioMixerGroup

namespace fortunatesonpeak;

[HarmonyPatch(typeof(GameOverHandler), "LoadAirport")]
public static class EndScreenPatch
{
    [HarmonyPostfix] // Mantuve Postfix como estaba en tu código original.
    public static void Prefix() // Mantuve Prefix en el nombre del método para reflejar el comportamiento del código.
    {
        FortunateSonPeakPlugin.Log.LogInfo(
            "¡Parche activado! Deteniendo la música de 'Fortunate Son'..."
        );

        if (FortunateSonPeakPlugin.CurrentAudioPlayer != null)
        {
            var audioSource = FortunateSonPeakPlugin.CurrentAudioPlayer.GetComponent<AudioSource>();

            if (audioSource != null)
            {
                if (FortunateSonPeakPlugin.Instance != null)
                {
                    FortunateSonPeakPlugin.Instance.StartCoroutine(
                        StopAudioAfterFade(audioSource, 2.0f)
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
                    UnityEngine.Object.Destroy(FortunateSonPeakPlugin.CurrentAudioPlayer);
                    FortunateSonPeakPlugin.CurrentAudioPlayer = null;
                }
            }
            else
            {
                FortunateSonPeakPlugin.Log.LogWarning(
                    "No se encontró AudioSource en currentAudioPlayer. Destruyendo inmediatamente."
                );
                UnityEngine.Object.Destroy(FortunateSonPeakPlugin.CurrentAudioPlayer);
                FortunateSonPeakPlugin.CurrentAudioPlayer = null;
            }
        }
        else
        {
            FortunateSonPeakPlugin.Log.LogWarning(
                "No hay un reproductor de audio activo para detener la música."
            );
        }
    }

    private static IEnumerator StopAudioAfterFade(AudioSource audioSource, float fadeDuration)
    {
        float startVolume = audioSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsedTime / fadeDuration);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume;

        if (FortunateSonPeakPlugin.CurrentAudioPlayer != null)
        {
            UnityEngine.Object.Destroy(FortunateSonPeakPlugin.CurrentAudioPlayer);
            FortunateSonPeakPlugin.CurrentAudioPlayer = null;
            FortunateSonPeakPlugin.Log.LogInfo("Música del mod desvanecida y objeto destruido.");
        }
    }
}
