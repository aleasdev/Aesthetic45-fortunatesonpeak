using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace fortunatesonpeak
{
    public static class AudioHandler
    {
        public static AudioClip? FortunateSonClip { get; private set; }
        public static GameObject? CurrentAudioPlayer { get; set; }

        public static void LoadFortunateSonClip(MonoBehaviour coroutineRunner)
        {
            try
            {
                string pluginLocation = Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location
                );
                string audioFilePath = Path.Combine(pluginLocation, "FortunateSon.wav");

                if (File.Exists(audioFilePath))
                {
                    // Necesitamos una instancia de MonoBehaviour para iniciar la corrutina
                    coroutineRunner.StartCoroutine(LoadWavFile(audioFilePath));
                }
                else
                {
                    FortunateSonPeakPlugin.Log.LogError(
                        $"¡Error! No se encontró el archivo de audio: {audioFilePath}"
                    );
                }
            }
            catch (System.Exception ex)
            {
                FortunateSonPeakPlugin.Log.LogError($"Error al cargar el archivo de audio: {ex}");
            }
        }

        private static IEnumerator LoadWavFile(string path)
        {
            using (
                UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(
                    "file://" + path,
                    AudioType.WAV
                )
            )
            {
                yield return www.SendWebRequest();

                if (
                    www.result == UnityWebRequest.Result.ConnectionError
                    || www.result == UnityWebRequest.Result.ProtocolError
                )
                {
                    FortunateSonPeakPlugin.Log.LogError($"Error al cargar audio: {www.error}");
                }
                else
                {
                    FortunateSonClip = DownloadHandlerAudioClip.GetContent(www);
                    if (FortunateSonClip != null)
                    {
                        FortunateSonPeakPlugin.Log.LogInfo(
                            "Audio 'fortunateson.wav' cargado correctamente."
                        );
                    }
                    else
                    {
                        FortunateSonPeakPlugin.Log.LogError(
                            "No se pudo obtener el contenido del AudioClip desde el archivo WAV."
                        );
                    }
                }
            }
        }

        public static void PlayFortunateSon()
        {
            if (FortunateSonClip != null)
            {
                // Destruir el objeto de audio anterior si existe
                if (CurrentAudioPlayer != null)
                {
                    UnityEngine.Object.Destroy(CurrentAudioPlayer);
                }

                // Crear nuevo objeto de audio
                CurrentAudioPlayer = new GameObject("FortunateSonAudioPlayer");
                UnityEngine.Object.DontDestroyOnLoad(CurrentAudioPlayer);

                // Agregar el componente AudioSource
                AudioSource source = CurrentAudioPlayer.AddComponent<AudioSource>();
                source.clip = FortunateSonClip;
                source.loop = true;

                // Buscar el AudioMixerGroup de música
                var musicGroup = Resources
                    .FindObjectsOfTypeAll<UnityEngine.Audio.AudioMixerGroup>()
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
                    FortunateSonPeakPlugin.Log.LogWarning(
                        "[⚠] Usando masterMixerGroup como fallback."
                    );
                }
                else
                {
                    source.volume = 0.7f;
                    FortunateSonPeakPlugin.Log.LogError(
                        "[✖] No se encontró ningún AudioMixerGroup."
                    );
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

        public static IEnumerator StopAudioWithFade(AudioSource audioSource, float fadeDuration)
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
            audioSource.volume = startVolume; // Restablecer volumen por si se reutiliza
        }
    }
}
