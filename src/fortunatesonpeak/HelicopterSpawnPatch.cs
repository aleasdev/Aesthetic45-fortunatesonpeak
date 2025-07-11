using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Audio;

namespace fortunatesonpeak;

[HarmonyPatch(typeof(PeakHandler), "SummonHelicopter")]
public static class HelicopterSpawnPatch
{
    [HarmonyPrefix] // Cambié a Prefix ya que tu código original estaba en Prefix en el ejemplo del usuario, aunque el comentario decía Postfix.
    public static void Postfix() // Mantuve Postfix en el nombre del método para reflejar el comportamiento del código.
    {
        FortunateSonPeakPlugin.Log.LogInfo(
            "¡Parche activado! Intentando reproducir 'Fortunate Son'..."
        );

        if (FortunateSonPeakPlugin.FortunateSonClip != null)
        {
            if (FortunateSonPeakPlugin.CurrentAudioPlayer != null)
            {
                UnityEngine.Object.Destroy(FortunateSonPeakPlugin.CurrentAudioPlayer);
            }

            FortunateSonPeakPlugin.CurrentAudioPlayer = new GameObject("FortunateSonAudioPlayer");
            UnityEngine.Object.DontDestroyOnLoad(FortunateSonPeakPlugin.CurrentAudioPlayer);

            AudioSource source =
                FortunateSonPeakPlugin.CurrentAudioPlayer.AddComponent<AudioSource>();
            source.clip = FortunateSonPeakPlugin.FortunateSonClip;
            source.loop = true;

            var musicGroup = Resources
                .FindObjectsOfTypeAll<AudioMixerGroup>()
                .FirstOrDefault(g => g.name == "Music_Setting");

            if (musicGroup != null)
            {
                source.outputAudioMixerGroup = musicGroup;
                source.volume = 0.7f;
                FortunateSonPeakPlugin.Log.LogInfo("[✔] Asignado a MusicVolumeSetting.");
            }
            else if (StaticReferences.Instance?.masterMixerGroup != null)
            {
                source.outputAudioMixerGroup = StaticReferences.Instance.masterMixerGroup;
                source.volume = 0.7f;
                FortunateSonPeakPlugin.Log.LogWarning("[⚠] Usando masterMixerGroup como fallback.");
            }
            else
            {
                source.volume = 0.7f;
                FortunateSonPeakPlugin.Log.LogError("[✖] No se encontró ningún AudioMixerGroup.");
            }

            source.Play();
            FortunateSonPeakPlugin.Log.LogInfo("¡'Fortunate Son' debería estar sonando ahora!");
        }
        else
        {
            FortunateSonPeakPlugin.Log.LogWarning(
                "El AudioClip de 'Fortunate Son' no se ha cargado. No se puede reproducir la canción."
            );
        }
    }
}
