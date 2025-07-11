using System;
using System.Collections; // Necesario para IEnumerator
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;

namespace fortunatesonpeak;

[BepInPlugin("com.aleasdev.fortunatesonpeak", "fortunatesonpeak", "1.0.0")]
public partial class FortunateSonPeakPlugin : BaseUnityPlugin
{
    public static ManualLogSource Log { get; private set; } = null!;
    public static AudioClip? FortunateSonClip { get; private set; }
    public static GameObject? CurrentAudioPlayer { get; set; }
    public static FortunateSonPeakPlugin Instance { get; private set; } = null!;

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        Log.LogInfo("fortunatesonpeak plugin is loaded!");
        Log.LogInfo("El mod está cargando...");

        LoadAudioClip();

        var harmony = new Harmony("com.aleasdev.fortunatesonpeak");
        harmony.PatchAll();
        Log.LogInfo("¡El mod se ha cargado correctamente!");
    }

    private void LoadAudioClip()
    {
        try
        {
            string pluginLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string audioFilePath = Path.Combine(pluginLocation, "FortunateSon.wav");

            if (File.Exists(audioFilePath))
            {
                StartCoroutine(LoadWavFile(audioFilePath));
            }
            else
            {
                Log.LogError($"¡Error! No se encontró el archivo de audio: {audioFilePath}");
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"Error al cargar el archivo de audio: {ex}");
        }
    }

    private IEnumerator LoadWavFile(string path)
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
                Log.LogError($"Error al cargar audio: {www.error}");
            }
            else
            {
                FortunateSonClip = DownloadHandlerAudioClip.GetContent(www);
                if (FortunateSonClip != null)
                {
                    Log.LogInfo("Audio 'fortunateson.wav' cargado correctamente.");
                }
                else
                {
                    Log.LogError(
                        "No se pudo obtener el contenido del AudioClip desde el archivo WAV."
                    );
                }
            }
        }
    }
}
