using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RandomizerMod
{
    //create a hash or random seed
    //can't access the file
    /*
    [HarmonyPatch(typeof(MainManager),"Load", new Type[]{typeof(int),typeof(bool)})]
    public class LoadPatch {
        [HarmonyPostfix]
        public static void Load(MainManager.LoadData? ___value)
        {
            RandomizerClass.SetHash((uint)___value.GetValueOrDefault().filename.GetHashCode());
        }            
    }
    */

    [HarmonyPatch(typeof(MainManager), "GetEnemyData", new Type[] { typeof(int), typeof(bool), typeof(bool)})]
    public class GetEnemyDataPatch
    {
        static bool IsShuffled = false;
        static int[] NewIDs;

        //change the id before it is used
        [HarmonyPrefix]
        public static void GetEnemyData(ref int id)
        {
            if (!IsShuffled)
            {
                RandomizerClass.logSource.LogInfo(Enum.GetNames(typeof(MainManager.Enemies)).Length);
                NewIDs = new int[Enum.GetNames(typeof(MainManager.Enemies)).Length];
                for (int i = 0; i < NewIDs.Length; i++)
                {
                    NewIDs[i] = i;
                }
                NewIDs = RandomizerClass.Shuffle(NewIDs);
                IsShuffled = true;
            }
            if (IsReserved(id))
            {
                return;
            }
            id = NewIDs[id];
            int k = 0;
            while (IsReserved(id) && k < 10)
            {
                id = NewIDs[id];
                k++;
            }
            if (k == 10)
            {
                id = 42; //fail safe
            }
        }

        //is this a reserved ID
        //This prevents you from having to fight glitchy or unkillable enemies
        public static bool IsReserved(int i)
        {
            //list:
            //  invincible spider ?
            //  invincible wasp king
            //  tutorial maki
            //  spider web (from first encounter)
            //  aria vine
            //  tidal wyrm
            //  tidal wyrm tail (because I don't think they work without each other)
            //  walls
            int[] reserved = new int[] { (int)MainManager.Enemies.Spuder, (int)MainManager.Enemies.MakiTutorial, (int)MainManager.Enemies.SandWall, (int)MainManager.Enemies.PisciWall, (int)MainManager.Enemies.IceWall, (int)MainManager.Enemies.SandWyrm, (int)MainManager.Enemies.SandWyrmTail, (int)MainManager.Enemies.WaspKingIntermission, (int)MainManager.Enemies.MothWeb, (int)MainManager.Enemies.AcolyteVine};
            for (int j = 0; j < reserved.Length; j++)
            {
                if (reserved[j] == i)
                {
                    return true;
                }
            }
            return false;
        }
    }

                    
    //this is where most stuff is shuffled
    //actually most of this isn't working
    [HarmonyPatch(typeof(MainManager),"LoadEssentials")]
    public class LoadEssentialsPatch
    {
        static bool itemVRandSuccess = false;
        static bool guiRandSuccess = false;

        [HarmonyPostfix]
        public static IEnumerator LoadEssentialsPostfix(IEnumerator input, AudioClip[] ___dsounds, AudioClip[] ___asounds, AudioClip[] ___msounds, Sprite[] ___guisprites)
        {
            bool success = true;
            while (input.MoveNext())
            {
                if (RandomizerClass.MiscVisualRand)
                {
                    if (MainManager.guisprites == null)
                    {
                        success = false;
                    }
                    else
                    {
                        if (!guiRandSuccess)
                        {
                            guiRandSuccess = true;
                            RandomizerClass.logSource.LogInfo("GUI Sprites");
                            MainManager.guisprites = RandomizerClass.Shuffle(MainManager.guisprites, 525);
                        }
                    }
                }

                if (RandomizerClass.ItemVisualRand)
                {
                    if (MainManager.itemsprites != null)
                    {
                        if (!itemVRandSuccess)
                        {
                            itemVRandSuccess = true;
                            RandomizerClass.logSource.LogInfo("Item sprites");
                            RandomizerClass.Shuffle2dArrayB(MainManager.itemsprites, 1337);
                        }
                    }
                    else
                    {
                        success = false;
                    }
                }

                if (!success)
                {
                    yield return null;
                }
            }
            yield break;
        }
    }

    //shuffle battle messages
    [HarmonyPatch(typeof(MainManager), "SetVariables")]
    public class SetVariablesPatch
    {
        [HarmonyPostfix]
        public static void SetVarPostfix()
        {
            if (RandomizerClass.MiscVisualRand)
            {
                Debug.Log("Battle messages");
                MainManager.battlemessage = RandomizerClass.Shuffle(MainManager.battlemessage, 9142);
            }
        }
    }

    //shuffle around animators
    [HarmonyPatch(typeof(EntityControl),"SetAnimator")]
    public class SetAnimatorPatch
    {
        public static bool IsShuffled;
        //so storing the runtime animators might not be the best idea
        //so I'll only store the file paths
        public static string[] AnimatorList;
        //372 total anim_ids ? (0-371)
        //actually more like -1 - 371?

        //sprite shuffle time
        [HarmonyPostfix]
        public static void SetAnimatorPostfix(EntityControl __instance)
        {
            if (RandomizerClass.SpriteRand)
            {
                if (!IsShuffled)
                {
                    MakeAnimList();
                    RandomizerClass.logSource.LogInfo("Animators");
                }
                if (__instance.animid > AnimatorList.Length - 1)
                {
                    //skip
                    //unity is pretty error tolerant but I don't like seeing a bunch of error messages
                    return;
                }
                //RandomizerClass.logSource.LogInfo("Tried loading from AnimationControllers/" + AnimatorList[__instance.animid] + "/" + AnimatorList[__instance.animid]);
                __instance.anim.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("AnimationControllers/"+AnimatorList[__instance.animid]+"/" + AnimatorList[__instance.animid]);
            }
        }

        public static void MakeAnimList()
        {
            IsShuffled = true;
            //RandomizerClass.logSource.LogInfo("There are " + (Enum.GetNames(typeof(MainManager.AnimIDs)).Length - 1) + " sprites.");
            AnimatorList = new string[Enum.GetNames(typeof(MainManager.AnimIDs)).Length-1]; //there is a "none" id
            for (int i = 0; i < AnimatorList.Length; i++)
            {
                AnimatorList[i] = ((MainManager.AnimIDs)i).ToString(); //weird code
                //RandomizerClass.logSource.LogInfo(((MainManager.AnimIDs)i).ToString());
            }
            //probably redundant but not sure if int arrays are pass by reference
            AnimatorList = RandomizerClass.Shuffle(AnimatorList,616);
        }
    }

    //shuffle maps
    [HarmonyPatch(typeof(MainManager), "LoadMap", new Type[] { typeof(int) })]
    public class LoadMapPatch
    {
        public static bool MapShuffled = false;
        static int[] MapShuffleList;
        public static int times = 0;

        [HarmonyPrefix]
        public static void LoadMap(ref int id)
        {
            if (!RandomizerClass.MapRand && !RandomizerClass.MapRand2)
            {
                return;
            }

            if (!MapShuffled || RandomizerClass.MapRand2)
            {
                MapShuffleList = new int[Enum.GetNames(typeof(MainManager.Maps)).Length];
                for (int i = 0; i < MapShuffleList.Length; i++)
                {
                    MapShuffleList[i] = i;
                }
                MapShuffleList = RandomizerClass.Shuffle(MapShuffleList, times * 5);
                MapShuffled = true;
                times++;
                RandomizerClass.logSource.LogInfo("Maps");
                return; //ignore first map load
            }
            id = MapShuffleList[id];

            //Problem
            //Spawn position does not work
        }
    }

    //the loading zone method
    [HarmonyPatch(typeof(MainManager), "TransferMap", new Type[] { typeof(int), typeof(Vector3), typeof(Vector3) , typeof(Vector3) , typeof(NPCControl) })]
    public class TransferMapPatch
    {
        [HarmonyPostfix]
        public static IEnumerator TransferMap(IEnumerator input, MainManager __instance)
        {
            MapUpdatePatch.time = 0;

            bool checker = (!RandomizerClass.MapRand && !RandomizerClass.MapRand2) || !LoadMapPatch.MapShuffled;
            bool warped = false;
            bool wait = false;

            while (input.MoveNext())
            {
                if (checker)
                {
                    Debug.Log("a");
                    yield return null;
                }

                if (!checker)
                {
                    if (MainManager.instance.minipause) //|| MainManager.player.pausecooldown < 6f || MainManager.instance.globalcooldown > 0f)
                    {
                        wait = true;
                        Debug.Log("Waiting at " + MainManager.player.transform.position);
                        yield return null; //wait until the end of map transfer
                    }

                    if (!wait && MainManager.map.entities.Length > 0 && !warped)
                    {
                        int index = 0;
                        int indexB = -1;
                        while (true)
                        {
                            RandomizerClass.logSource.LogInfo("Attempting to warp");
                            //index out of range, now try with indexB
                            if (index > MainManager.map.entities.Length - 1)
                            {
                                RandomizerClass.logSource.LogInfo("Try indexB ("+indexB+")");
                                if (indexB == -1)
                                {
                                    RandomizerClass.logSource.LogInfo("Warp fail");
                                    yield break;
                                }
                                else
                                {
                                    if (MainManager.map.entities[indexB] != null)
                                    {
                                        RandomizerClass.logSource.LogInfo("Warped to (first entrance) " + MainManager.map.entities[indexB].transform.position);
                                        MainManager.player.transform.position = MainManager.map.entities[indexB].transform.position;
                                        Vector3 tppos = MainManager.player.transform.position + Vector3.up * 0.05f;
                                        for (int k = 0; k < MainManager.instance.playerdata.Length; k++)
                                        {
                                            MainManager.instance.playerdata[k].entity.transform.position = tppos + MainManager.MainCamera.transform.forward * ((float)k / 10f);
                                        }
                                        warped = true;
                                    }
                                }
                                break;
                            }

                            if (index < MainManager.map.entities.Length && MainManager.map.entities[index] != null && MainManager.map.entities[index].npcdata.objecttype == NPCControl.ObjectTypes.DoorOtherMap)
                            {
                                //find second entrance
                                if (indexB == -1)
                                {
                                    indexB = index;
                                    index++;
                                    continue;
                                }
                                break;
                            }
                            index++;
                            Debug.Log(index);
                        }
                        if (!warped && index < MainManager.map.entities.Length && MainManager.map.entities[index] != null)
                        {
                            RandomizerClass.logSource.LogInfo("Warped to " + MainManager.map.entities[index].transform.position);
                            MainManager.player.transform.position = MainManager.map.entities[index].transform.position;
                            Vector3 tppos = MainManager.player.transform.position;
                            for (int k = 0; k < MainManager.instance.playerdata.Length; k++)
                            {
                                MainManager.instance.playerdata[k].entity.transform.position = tppos + MainManager.MainCamera.transform.forward * ((float)k / 10f);
                            }
                            warped = true;
                        }
                    }
                    yield return null;
                }
            }
        }
    }

    //freefly patch
    [HarmonyPatch(typeof(PlayerControl),"LateUpdate")]
    public class LateUpdatePatch
    {
        [HarmonyPostfix]
        public static void LateUpdatePostfix(PlayerControl __instance, ref float ___startheight)
        {
            if (__instance.flying && RandomizerClass.FreeFly)
            {
                if (MainManager.GetKey(4, true))
                { //a
                    ___startheight += 0.1f * MainManager.framestep;
                }
                if (MainManager.GetKey(6, true))
                { //z
                    ___startheight -= 0.1f * MainManager.framestep;
                    __instance.transform.position = Vector3.Lerp(__instance.transform.position, new Vector3(__instance.transform.position.x, ___startheight + 1f, __instance.transform.position.z), 0.05f);
                }
                __instance.flycooldown = 5; //basically remove fly time limit
            }

            //fly jump
            if (__instance.flying && RandomizerClass.FlyJump && !RandomizerClass.FreeFly)
            {
                if (MainManager.GetKey(4, true))
                { //a
                    //stop you from flying again
                    __instance.canfly = false;
                    __instance.flycooldown = 0f; //stop flying?
                    __instance.entity.Jump(__instance.entity.jumpheight * 1.8f);
                }
            }

            /*
            if (MainManager.GetKey(4,true) && MainManager.GetKey(5, true) && MainManager.GetKey(6, true))
            {
                Time.timeScale = 3;
                Debug.Log("Gotta go fast!");
            }
            */
        }
    }


    //music randomization
    [HarmonyPatch(typeof(MainManager), "ChangeMusic", new Type[] { typeof(AudioClip), typeof(float), typeof(int), typeof(bool)})]
    public class ChangeMusicPatch
    {
        static bool IsShuffled = false;
        static int[] MusicList;

        [HarmonyPrefix]
        public static void ChangeMusicPrefix(ref AudioClip musicclip)
        {
            if (!RandomizerClass.MusicRand)
            {
                return;
            }
            if (!IsShuffled)
            {
                IsShuffled = true;
                MusicList = new int[Enum.GetNames(typeof(MainManager.Musics)).Length];
                for (int i = 0; i < MusicList.Length; i++)
                {
                    MusicList[i] = i;
                }
                MusicList = RandomizerClass.Shuffle(MusicList,608);
                RandomizerClass.logSource.LogInfo("Music");
            }

            //MainManager.Musics result;
            int trueid = 0;
            if (musicclip != null)
            {
                trueid = (int)Enum.Parse(typeof(MainManager.Musics),musicclip.name);
                trueid = MusicList[trueid];
            }
            AudioClip audio = Resources.Load<AudioClip>("Audio/Music/" + ((MainManager.Musics)trueid).ToString());
            if (audio == null)
            {
                RandomizerClass.logSource.LogInfo("Some music failed to load: "+ ((MainManager.Musics)trueid).ToString());
            }
            musicclip = audio;
        }
    }

    //sound patch (does not always work)
    [HarmonyPatch(typeof(MainManager), "PlaySound", new Type[] { typeof(AudioClip), typeof(int), typeof(float), typeof(float), typeof(bool) })]
    public class PlaySoundPatch
    {
        public static bool IsShuffled = false;
        public static int[] ShuffleList;

        [HarmonyPrefix]
        public static void PlaySoundPrefix(ref AudioClip soundclip, AudioClip[] ___asounds)
        {
            if (!RandomizerClass.SoundRand)
            {
                return;
            }
            if (!IsShuffled)
            {
                ShuffleList = new int[___asounds.Length];
                for (int i = 0; i < ___asounds.Length; i++)
                {
                    ShuffleList[i] = i;
                }
                ShuffleList = RandomizerClass.Shuffle(ShuffleList);
                IsShuffled = true;
                RandomizerClass.logSource.LogInfo("Sounds");
            }
            int index = -1;
            for (int i = 0; i < ___asounds.Length; i++)
            {
                if (soundclip.name == ___asounds[i].name)
                {
                    index = i;
                }
            }
            if (index == -1)
            {
                RandomizerClass.logSource.LogInfo("Failed to find sound " + soundclip.name);
                return;
            }
            else
            {
                soundclip = ___asounds[ShuffleList[index]];
            }
        }
    }

    //can you patch map methods that aren't explicitly defined?
    //yes
    [HarmonyPatch(typeof(MapControl),"FixedUpdate")]
    public class MapUpdatePatch
    {
        //add some bonus chaos
        //chaos effect 1: XYZ wiggle
        static float xFrequency;
        static float xAmplitude;
        static float yFrequency;
        static float yAmplitude;
        static float zFrequency;
        static float zAmplitude;

        //chaos effect 2: yaw rotation
        static float yawFrequency; //in Hz
        static float yawAmplitude; //in degrees

        //chaos effect 3: pitch + roll rotation
        static float pitchFrequency; //in Hz
        static float pitchAmplitude; //in degrees
        static float rollFrequency; //in Hz
        static float rollAmplitude; //in degrees

        public static float time = 0;

        //have to make sure map does not become untraversable?
        [HarmonyPrefix]
        public static void UpdatePatch(MapControl __instance)
        {
            if (RandomizerClass.MapChaosLevel > 0 && RandomizerClass.MapChaosStrength > 0)
            {
                time += Time.fixedDeltaTime;

                //yaw formula: 5 degrees per second at strength 1
                //frequency * degrees = constant = 1/5  d/s

                if (RandomizerClass.MapChaosLevel >= 2)
                {
                    yawFrequency = 0.2f + 0.05f * RandomizerClass.MapChaosStrength + (0.8f + 0.3f * RandomizerClass.MapChaosStrength) * RandomizerClass.GetRandFloat(1472 + (int)__instance.mapid); //0.2 to 1 (period goes from 1-5, skewed towards 1)
                    yawAmplitude = RandomizerClass.MapChaosStrength / (200f * yawFrequency);
                    __instance.mainmesh.eulerAngles = __instance.mainmesh.eulerAngles + Vector3.up * yawAmplitude * Mathf.Sin(time * yawFrequency);

                    pitchFrequency = 0.2f + 0.05f * RandomizerClass.MapChaosStrength + (0.8f + 0.3f * RandomizerClass.MapChaosStrength) * RandomizerClass.GetRandFloat(1402 + (int)__instance.mapid); //0.2 to 1 (period goes from 1-5, skewed towards 1)
                    pitchAmplitude = RandomizerClass.MapChaosStrength / (200f * pitchFrequency);
                    __instance.mainmesh.eulerAngles = __instance.mainmesh.eulerAngles + Vector3.forward * pitchAmplitude * Mathf.Sin(time * pitchFrequency);

                    rollFrequency = 0.2f + 0.05f * RandomizerClass.MapChaosStrength + (0.8f + 0.3f * RandomizerClass.MapChaosStrength) * RandomizerClass.GetRandFloat(1512 + (int)__instance.mapid); //0.2 to 1 (period goes from 1-5, skewed towards 1)
                    rollAmplitude = RandomizerClass.MapChaosStrength / (200f * rollFrequency);
                    __instance.mainmesh.eulerAngles = __instance.mainmesh.eulerAngles + Vector3.right * rollAmplitude * Mathf.Sin(time * rollFrequency);
                }

                if (RandomizerClass.MapChaosLevel == 1 || RandomizerClass.MapChaosLevel >= 3)
                {
                    xFrequency = 1.0f + 0.25f * RandomizerClass.MapChaosStrength + (1.0f + 0.1f * RandomizerClass.MapChaosStrength) * RandomizerClass.GetRandFloat(52612 + (int)__instance.mapid); //0.2 to 1 (period goes from 1-5, skewed towards 1)
                    xAmplitude = RandomizerClass.MapChaosStrength / (150f * xFrequency);

                    yFrequency = 1.0f + 0.2f * RandomizerClass.MapChaosStrength + (1.0f + 0.2f * RandomizerClass.MapChaosStrength) * RandomizerClass.GetRandFloat(417762 + (int)__instance.mapid); //0.2 to 1 (period goes from 1-5, skewed towards 1)
                    yAmplitude = RandomizerClass.MapChaosStrength / (150f * yFrequency);

                    zFrequency = 1.0f + 0.25f * RandomizerClass.MapChaosStrength + (1.0f + 0.1f * RandomizerClass.MapChaosStrength) * RandomizerClass.GetRandFloat(7291232 + (int)__instance.mapid); //0.2 to 1 (period goes from 1-5, skewed towards 1)
                    zAmplitude = RandomizerClass.MapChaosStrength / (150f * zFrequency);
                    __instance.mainmesh.position += Vector3.right * xAmplitude * Mathf.Sin(time * xFrequency)
                                                  + Vector3.up * yAmplitude * Mathf.Sin(Time.time * yFrequency)
                                                  + Vector3.forward * zAmplitude * Mathf.Sin(Time.time * zFrequency);
                }
            }
        }
    }
}
