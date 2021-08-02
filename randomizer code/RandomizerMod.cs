using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RandomizerMod
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    [BepInProcess("Bug Fables.exe")]
    public class RandomizerClass : BaseUnityPlugin
    {
        public const string pluginGuid = "com.bugfables.randomizer";
        public const string pluginName = "Randomizer";
        public const string pluginVersion = "0.9.0";

        public static BepInEx.Logging.ManualLogSource logSource;

        //config
        ConfigEntry<uint> conSeed;
        //BepInEx.Configuration.ConfigEntry<bool> conNew;
        ConfigEntry<bool> conSprite;
        ConfigEntry<bool> conItem;
        ConfigEntry<bool> conEnemy;
        ConfigEntry<bool> conMiscVis;
        ConfigEntry<bool> conMap;
        ConfigEntry<bool> conMusic;
        ConfigEntry<bool> conMap2;
        ConfigEntry<bool> conSound;

        ConfigEntry<int> conMapChaosLevel;
        ConfigEntry<float> conMapChaosStrength;

        ConfigEntry<bool> conFreeFly;
        ConfigEntry<bool> conFlyJump;

        public static uint hash { get; private set; } = 1;

        //public static string RandConfigPath;

        //public static bool NewSeedOnStartup = true;

        //Settings
        //Dial back the chaos
        public static bool SpriteRand = true; //NPC sprites mainly
        public static bool ItemVisualRand = true;
        public static bool EnemyRand = true;
        public static bool MiscVisualRand = false; //miscellaneous visual randomizers (gui, battle leaves, grass)
        public static bool MapRand = false; //maps are randomized (loadmap result is shuffled)
        public static bool MapRand2 = false;
        public static bool MusicRand = true;
        public static bool SoundRand = true;

        //Extra chaos
        public static int MapChaosLevel = 0; //how much map chaos happens (causes maps to move around and rotate)
            //level 1: only translation
            //level 2: only rotation
            //level 3+: both
        public static float MapChaosStrength = 0; //how bad is map chaos (causes faster / stronger movement)
            //1 is low (not that bad), 10 is very high

        //Abilities
        public static bool FreeFly = false; //makes flight less restrictive, allows upward flight (hold A and B), downward flight (hold A and Z), removes flight time limit
        public static bool FlyJump = false;

        //Default settings:
        //makenewseedonstartup, sprite rand, item vis rand, enemy rand        

        //public static bool MusicRand;
        //public static bool DialogueRand;

        public void Awake()
        {
            logSource = Logger;
            logSource.LogInfo("Randomizer awake");

            Harmony harmony = new Harmony("com.bugfables.randomizer");
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
                logSource.LogMessage("Randomizer patched");
            }

            conSeed = Config.Bind("Seed", "Seed", (uint)19686, "Seed used for generating randomization.");
            hash = conSeed.Value;

            //conNew = Config.Bind("Seed", "Make New Seed", false, "Generate a new seed on next startup.");
            //NewSeedOnStartup = conNew.Value;
            //conNew.SetSerializedValue(false);

            conSprite = Config.Bind("Randomizers", "Sprites", true, "Sprite randomization (NPCs and enemies). Some may become invisible due to invalid animations.");
            SpriteRand = conSprite.Value;

            conItem = Config.Bind("Randomizers", "Item Visuals", true, "Changes item and medal icons. Note that there are many empty icons stored in memory, so many items and medals will become invisible.");
            ItemVisualRand = conItem.Value;

            conEnemy = Config.Bind("Randomizers", "Enemies", true, "Changes enemies. Some entities are not randomized (invincible enemies, walls)");
            EnemyRand = conEnemy.Value;

            conMiscVis = Config.Bind("Randomizers", "Miscellaneous Visuals", false, "Miscellaneous visuals. (GUI sprites, battle messages)");
            MiscVisualRand = conMiscVis.Value;

            conMap = Config.Bind("Randomizers", "Maps (one time)", false, "Maps are randomized (though saving and loading will keep you on the same map). Can potentially softlock you if a room's loading zone leads back to itself");
            MapRand = conMap.Value;

            conMap2 = Config.Bind("Randomizers", "Maps (every time)", false, "Every loading zone takes you to a random map.");
            MapRand2 = conMap2.Value;

            conMusic = Config.Bind("Randomizers", "Music", true, "Music. Samira still acts as if the music is not randomized though");
            MusicRand = conMusic.Value;

            conSound = Config.Bind("Randomizers", "Sound", true, "Some sounds. Not all sounds use the same methods and so are unrandomized.");
            SoundRand = conSound.Value;

            conMapChaosLevel = Config.Bind("Chaos", "Map Chaos Level", 0, "Causes maps to slide around chaotically. Don't use this option if you are prone to motion sickness. Not all objects in the map move around. Loading zones appear to stay at their original coordinates, so standing where the loading zone was will eventually activate the loading zone as you pass it. 1 = maps will drift around on the coordinate axes, 2 = maps will rotate around, 3+ = both");
            MapChaosLevel = conMapChaosLevel.Value;

            conMapChaosStrength = Config.Bind("Chaos", "Map Chaos Strength", 0f, "Strength of map chaos effects. Range: 0-10 but values above 10 function as expected");
            MapChaosStrength = conMapChaosStrength.Value;

            conFreeFly = Config.Bind("Abilities", "Free Fly", false, "Allows ability to fly forever, allows ability to fly upward and downward by holding the jump button or switch button.");
            FreeFly = conFreeFly.Value;

            conFlyJump = Config.Bind("Abilities", "Fly Jump", false, "Lets you jump out of fly with the jump button. Kind of glitchy. Overwritten by free fly");
            FlyJump = conFlyJump.Value;
        }

        public static void SetHash(uint p_hash)
        {
            if (p_hash == 0)
            {
                p_hash = 658275685;
            }
            hash = p_hash * 16843009 + 658275685; //16843009 = 2^24 + 2^16 + 2^8 + 2^0
            if (hash == 0)
            {
                hash = 155152;
            }
                                      //pretty arbitrary
        }

        //max length 12
        public static T[] Shuffle<T>(T[] parray)
        {
            //logSource.LogInfo("Shuffling with length = " + parray.Length);
            uint subhash = hash; //set hash
            uint arraySize = (uint)parray.Length;
            while (arraySize > 1)
            {
                uint randnum = subhash % arraySize;
                subhash /= arraySize;
                if (subhash <= arraySize * arraySize * arraySize)
                {
                    //make the subhash bigger
                    subhash *= hash;
                }

                T temp = parray[arraySize-1];
                parray[arraySize - 1] = parray[randnum];
                parray[randnum] = temp;
                arraySize--;
            }

            return parray;
        }

        public static T[] Shuffle<T>(T[] parray, int key)
        {
            //logSource.LogInfo("Shuffling with length = "+parray.Length);
            uint subhash = (uint)(hash + key); //set hash
            uint arraySize = (uint)parray.Length;
            while (arraySize > 1)
            {
                uint randnum = subhash % arraySize;
                subhash /= arraySize;
                //logSource.LogInfo("Random = "+randnum + " Subhash = "+subhash);
                if (subhash <= arraySize * arraySize * arraySize)
                {
                    //make the subhash bigger
                    subhash *= hash;
                    subhash += 1253;
                }

                //logSource.LogInfo("Replaced " + randnum + " with " + (arraySize - 1));

                T temp = parray[arraySize - 1];
                parray[arraySize - 1] = parray[randnum];
                parray[randnum] = temp;
                arraySize--;
            }

            return parray;
        }

        //can't use generic method from above in this case
        public static T[,] Shuffle2dArray<T>(T[,] parray, int key)
        {
            //logSource.LogInfo("Shuffling with length = "+parray.Length);
            uint subhash = (uint)(hash + key); //set hash
            uint arraySize = (uint)parray.GetLength(0);
            while (arraySize > 1)
            {
                uint randnum = subhash % arraySize;
                subhash /= arraySize;
                //logSource.LogInfo("Random = "+randnum + " Subhash = "+subhash);
                if (subhash <= arraySize * arraySize * arraySize)
                {
                    //make the subhash bigger
                    subhash *= hash;
                    subhash += 1253;
                }

                //logSource.LogInfo("Replaced " + randnum + " with " + (arraySize - 1));

                T[] temp = new T[parray.GetLength(1)];
                //logSource.LogInfo("randnum = "+randnum+" replaced with "+(arraySize-1));
                for (int i = 0; i < temp.Length; i++)
                {
                    temp[i] = parray[arraySize - 1, i];
                }
                for (int i = 0; i < temp.Length; i++)
                {
                    parray[arraySize - 1,i] = parray[randnum,i];
                }
                for (int i = 0; i < temp.Length; i++)
                {
                    parray[randnum,i] = temp[i];
                }
                arraySize--;
            }

            return parray;
        }

        //can't use generic method from above in this case
        //This one shuffles while keeping the first number's stuff in order
        public static T[,] Shuffle2dArrayB<T>(T[,] parray, int key)
        {
            //logSource.LogInfo("Shuffling with length = "+parray.Length);
            uint subhash = (uint)(hash + key); //set hash
            uint arraySize = (uint)parray.GetLength(1);
            while (arraySize > 1)
            {
                uint randnum = subhash % arraySize;
                subhash /= arraySize;
                //logSource.LogInfo("Random = "+randnum + " Subhash = "+subhash);
                if (subhash <= arraySize * arraySize * arraySize)
                {
                    //make the subhash bigger
                    subhash *= hash;
                    subhash += 1253;
                }

                //logSource.LogInfo("Replaced " + randnum + " with " + (arraySize - 1));

                T[] temp = new T[parray.GetLength(0)];
                //logSource.LogInfo("randnum = "+randnum+" replaced with "+(arraySize-1));
                for (int i = 0; i < temp.Length; i++)
                {
                    temp[i] = parray[i,arraySize - 1];
                }
                for (int i = 0; i < temp.Length; i++)
                {
                    parray[i,arraySize - 1] = parray[i,randnum];
                }
                for (int i = 0; i < temp.Length; i++)
                {
                    parray[i,randnum] = temp[i];
                }
                arraySize--;
            }

            return parray;
        }

        public static int GetRand(int hash)
        {
            //arbitrary
            return hash * 185012859 + 275157;
        }

        public static float GetRandFloat(int hash)
        {
            return (GetRand(hash) % 65536)/65536;
        }
    }
}