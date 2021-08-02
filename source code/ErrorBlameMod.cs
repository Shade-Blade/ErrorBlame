using BepInEx;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ErrorBlameMod
{
    [BepInPlugin(pluginGuid,pluginName,pluginVersion)]
    [BepInProcess("Bug Fables.exe")]
    public class ErrorBlameMod : BaseUnityPlugin
    {
        public const string pluginGuid = "com.bugfables.errorblame";
        public const string pluginName = "Error Blame";
        public const string pluginVersion = "0.9.0";

        public static BepInEx.Logging.ManualLogSource logSource;
        public static BepInEx.Configuration.ConfigEntry<string> cfgBundleName;
        public static AssetBundle bundle;

        public void Awake()
        {
            logSource = Logger;
            logSource.LogInfo("Error Blame awake");

            cfgBundleName = Config.Bind("AssetBundle", "BundleName", "errorblamebundle", "The filename of AssetBundle");
            //bundle organization
            //assets/base/dialogues/*
            //assets/base/dialogues/maps/*
            //assets/base/textures/*
            //  title0, battlem0, rank0

            try
            {
                string text = Paths.PluginPath + Path.DirectorySeparatorChar + cfgBundleName.Value;
                bundle = AssetBundle.LoadFromFile(text);
                logSource.LogMessage("Loaded bundle: " + text);
            }
            catch (Exception arg)
            {
                logSource.LogError("LoadBundleError:" + Environment.NewLine + arg);
            }

            Harmony harmony = new Harmony("com.bugfables.errorblame");
            try
            {
                harmony.PatchAll();
            }
            catch (Exception ex)
            {
                logSource.LogError(ex);
            }
            finally
            {
                logSource.LogMessage("Error Blame patched");
            }

        }

        //use this to obtain certain dialogue files
        //input a file path
        //after the "dialogues"
        //maps files are in the maps folder
        public static string GetDialogue(string name)
        {
            string text = "assets/base/" + name + ".txt";
            //logSource.LogInfo("Loading " + text);
            string result;
            try
            {
                result = bundle.LoadAsset<TextAsset>(text).ToString();
            }
            catch (Exception ex)
            {
                logSource.LogError("Failed to access " + text);
                logSource.LogError(ex);
                throw ex;
            }
            return result;
        }

        public static string GetMapDialogue(string name)
        {
            string text = "assets/base/maps/" + name + ".txt";
            //logSource.LogInfo("Loading " + text);
            string result;
            try
            {
                result = bundle.LoadAsset<TextAsset>(text).ToString();
            }
            catch (Exception ex)
            {
                logSource.LogError("Failed to access " + text);
                logSource.LogError(ex);
                throw ex;
            }
            return result;
        }
    }
}