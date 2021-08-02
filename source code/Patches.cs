using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ErrorBlameMod
{
    //probably won't need this very often
    //intercept text before it is actually displayed
    //in case prefixes or postfixes won't work
    [HarmonyPatch(typeof(MainManager), "SetText", new Type[]
    {
    typeof(string),
    typeof(bool),
    typeof(Vector3),
    typeof(Transform),
    typeof(NPCControl)
    })]
    public class SetTextPatch
    {
        public static string insertText = null;

        [HarmonyPrefix]
        public static bool SetTextIntercept(ref string text)
        {
            //ErrorBlameMod.logSource.LogInfo("SetText");
            bool flag = !string.IsNullOrEmpty(insertText);
            if (flag)
            {
                text = insertText;
                insertText = null;
            }
            return true;
        }
    }

    //Start screen
    //Replace old title with new one
    
    [HarmonyPatch(typeof(StartMenu), "Intro", 0)]
    public class StartIntroPatch
    {
        [HarmonyPostfix]
        private static IEnumerator StartIntroPostfix(IEnumerator input, SpriteRenderer[] ___sprites)
        {
            int i = 0;
            while (input.MoveNext())
            {
                bool flag = i == 3;
                if (flag)
                {
                    Sprite[] newTitle = ErrorBlameMod.bundle.LoadAllAssets<Sprite>();
                    bool flag2 = newTitle == null || newTitle.Length < 1;
                    if (!flag2)
                    {
                        //title0 loads last
                        ___sprites[1].sprite = newTitle[newTitle.Length - 1];
                    }
                }
                yield return input.Current;
                bool flag3 = !MainManager.basicload || MainManager.languageid == -1;
                if (!flag3)
                {
                    int num = i;
                    i = num + 1;
                }
            }
            yield break;
        }
    }


    //notes:
    //a lot of dialogue is just loaded in and kept in memory
    //makes it easy to replace :)

    //Event 71: fortune teller lines?
    //text here is not stored long term
    //have to replicate functionality of the fortune teller and intercept the text as it is passed to SetText
    [HarmonyPatch(typeof(EventControl), "Event71")]
    public class Event71Patch
    {
        [HarmonyPostfix]
        static IEnumerator Event71Postfix(IEnumerator input, NPCControl ___call)
        {
            int count = -1;
            string[] text = null;
            int[] res = null;

            //flags used in fortune teller
            int[][] Event71Flags = new int[][]
            {
                        new int[]
                        {
                            71,
                            87,
                            142,
                            271,
                            331,
                            380,
                            381,
                            388,
                            392,
                            455,
                            460,
                            463,
                            200,
                            243,
                            55,
                            469,
                            471,
                            486,
                            468,
                            488,
                            489,
                            498,
                            499,
                            578,
                            604,
                            603
                        },
                        new int[]
                        {
                            338,
                            121,
                            230,
                            137,
                            611,
                            462,
                            220,
                            149,
                            415,
                            285,
                            355,
                            534,
                            575
                        }
            };

            while (input.MoveNext())
            {
                bool flag = count > 0;
                if (flag)
                {
                    int num = count;
                    count = num - 1;
                    bool flag2 = count == 0 && res != null && res.Length != 0;
                    if (flag2)
                    {
                        //choose random avaliable thing
                        SetTextPatch.insertText = text[res[UnityEngine.Random.Range(0, res.Length)]];
                    }
                }
                yield return input.Current;
                bool flag3 = count < 0 && ___call.insideid == 1 && MainManager.instance.insideid == 1;
                if (flag3)
                {
                    count = 1;
                    text = ErrorBlameMod.GetDialogue("FortuneTeller" + MainManager.instance.option).Replace("\r\n", "\n").Split(new char[]
                    {
                '\n'
                    });
                    List<int> possible = new List<int>();
                    int num;
                    for (int i = 0; i < text.Length; i = num + 1)
                    {
                        bool flag4 = MainManager.instance.option == 0;
                        if (flag4)
                        {
                            bool flag5 = !MainManager.instance.crystalbflags[i];
                            if (flag5)
                            {
                                possible.Add(i);
                            }
                        }
                        else
                        {
                            bool flag6 = !MainManager.instance.flags[Event71Flags[MainManager.instance.option - 1][i]];
                            if (flag6)
                            {
                                possible.Add(i);
                            }
                        }
                        num = i;
                    }

                    //result
                    res = possible.ToArray();
                    possible = null;
                }
            }
            yield break;
        }
    }

    [HarmonyPatch(typeof(PauseMenu), "SetMapLines")]
    public class SetMapLinesPatch
    {
        [HarmonyPrefix]
        static bool SetMapLinesPrefix(ref string[] ___enemydata)
        {
            ___enemydata = ErrorBlameMod.GetDialogue("AreaDesc").Split(new char[]
            {
                '\n'
            });
            return true;
        }
    }

    [HarmonyPatch(typeof(MapControl), "Start")]
    public class MapStartPatch
    {
        [HarmonyPostfix]
        private static void MapStartPostfix(MapControl __instance)
        {
            try
            {
                string text = ErrorBlameMod.GetMapDialogue((__instance.readdatafromothermap != MainManager.Maps.TestRoom) ? __instance.readdatafromothermap.ToString() : __instance.mapid.ToString());
                __instance.dialogues = text.Replace("\r\n", "\n").Split(new char[]
                {
                        '\n'
                });
            }
            catch
            {
                ErrorBlameMod.logSource.LogError("MapFoundNoText for " + __instance.mapid);
            }
        }
    }

    [HarmonyPatch(typeof(MainManager), "SetVariables")]
    public class SetVariablesPatch
    {
        [HarmonyPostfix]
        private static void SetVarPostfix(MainManager __instance)
        {
            ErrorBlameMod.logSource.LogInfo("SetVariables (where most new text is loaded)");

            //loading sprites is not fun
            //Good news
            //It loads in alphabetical order :)
            Sprite[] sarray = new Sprite[7];
            Sprite[] helper = ErrorBlameMod.bundle.LoadAllAssets<Sprite>();
            //int[] needed = new int[] { 0, 1, 2, 3, 4, 5, 6 };
            for (int i = 0; i < sarray.Length; i++)
            {
                //battlem0_(0-6)
                sarray[i] = helper[i];
            }

            MainManager.battlemessage = sarray;

            MainManager.menutext = ErrorBlameMod.GetDialogue("MenuText").Replace("\r\n", "\n").Split(new char[]
            {
                '\n'
            });
            MainManager.commondialogue = ErrorBlameMod.GetDialogue("CommonDialogue").Split(new char[]
            {
                '\n'
            });
            MainManager.musicnames = ErrorBlameMod.GetDialogue("MusicList").Split(new char[]
            {
                '\n'
            });
            MainManager.commandhelptext = ErrorBlameMod.GetDialogue("ActionCommands").Split(new char[]
            {
                '\n'
            });
            MainManager.areanames = ErrorBlameMod.GetDialogue("AreaNames").Split(new char[]
            {
                '\n'
            });
            MainManager.itemdata = new string[1, 256, 7];
            string[] array = ErrorBlameMod.GetDialogue("Items").Replace("\r\n", "\n").Split(new char[]
            {
                '\n'
            });
            string[] array2 = Resources.Load<TextAsset>("Data/ItemData").ToString().Split(new char[]
            {
                '\n'
            });
            for (int i = 0; i < array2.Length - 1; i++)
            {
                //item text uses "@" as a separator                    
                string[] array3 = array[i].Split(new char[]
                {
                    '@'
                });
                bool flag2 = array3.Length > 1;
                if (flag2)
                {
                    MainManager.itemdata[0, i, 0] = array3[0];
                    MainManager.itemdata[0, i, 1] = array3[1];
                    MainManager.itemdata[0, i, 2] = array3[2];
                    bool flag3 = array3.Length > 3;
                    if (flag3)
                    {
                        MainManager.itemdata[0, i, 3] = array3[3];
                    }
                    else
                    {
                        MainManager.itemdata[0, i, 3] = string.Empty;
                    }
                    array3 = array2[i].Split(new char[]
                    {
                        '@'
                    });
                    MainManager.itemdata[0, i, 4] = array3[0];
                    MainManager.itemdata[0, i, 5] = array3[1];
                    MainManager.itemdata[0, i, 6] = array3[2];
                }
            }
            array = ErrorBlameMod.GetDialogue("Skills").Split(new char[]
            {
                '\n'
            });
            array2 = Resources.Load<TextAsset>("Data/SkillData").ToString().Split(new char[]
            {
                '\n'
            });
            MainManager.skilldata = new string[array2.Length - 1, 13];
            for (int j = 0; j < MainManager.skilldata.GetLength(0); j++)
            {
                string[] array4 = array[j].Split(new char[]
                {
                    '@'
                });
                MainManager.skilldata[j, 0] = array4[0];
                MainManager.skilldata[j, 1] = array4[1];
                array4 = array2[j].Split(new char[]
                {
                    '@'
                });
                for (int k = 2; k < MainManager.skilldata.GetLength(1); k++)
                {
                    MainManager.skilldata[j, k] = array4[k - 2];
                }
            }
            array = ErrorBlameMod.GetDialogue("BadgeName").Split(new char[]
            {
                '\n'
            });
            array2 = Resources.Load<TextAsset>("Data/BadgeData").ToString().Split(new char[]
            {
                '\n'
            });
            MainManager.badgedata = new string[array2.Length - 1, 8];
            for (int l = 0; l < MainManager.badgedata.GetLength(0); l++)
            {
                string[] array5 = array[l].Split(new char[]
                {
                    '@'
                });
                MainManager.badgedata[l, 0] = array5[0];
                MainManager.badgedata[l, 1] = array5[1];
                MainManager.badgedata[l, 6] = ((string.IsNullOrEmpty(array5[2]) || array5[2] == " ") ? string.Empty : array5[2]);
                array5 = array2[l].Split(new char[]
                {
                    '@'
                });
                MainManager.badgedata[l, 2] = array5[0];
                MainManager.badgedata[l, 3] = array5[1];
                MainManager.badgedata[l, 4] = array5[2];
                MainManager.badgedata[l, 5] = array5[3];
                MainManager.badgedata[l, 7] = array5[4];
            }
            array = ErrorBlameMod.GetDialogue("BoardQuests").Split(new char[]
            {
                '\n'
            });
            array2 = Resources.Load<TextAsset>("Data/BoardData").ToString().Split(new char[]
            {
                '\n'
            });
            MainManager.boardquestdata = new string[array.Length, array[0].Split(new char[]
            {
                '@'
            }).Length + array2[0].Split(new char[]
            {
                '@'
            }).Length];
            for (int m = 0; m < array.Length; m++)
            {
                string[] array6 = array[m].Split(new char[]
                {
                    '@'
                });
                int num = 0;
                for (int n = 0; n < array6.Length; n++)
                {
                    MainManager.boardquestdata[m, n] = array6[n];
                    num++;
                }
                array6 = array2[m].Split(new char[]
                {
                    '@'
                });
                for (int num2 = 0; num2 < array6.Length; num2++)
                {
                    MainManager.boardquestdata[m, num2 + num] = array6[num2];
                }
            }
            MainManager.librarydata = new string[5, 256, 10];
            MainManager.libraryorder = new int[MainManager.librarydata.GetLength(0), MainManager.librarydata.GetLength(1)];
            for (int num3 = 0; num3 < MainManager.librarydata.GetLength(0); num3++)
            {
                string[] array7 = new string[]
                {
                    string.Empty
                };
                switch (num3)
                {
                    case 0:
                        {
                            array7 = Resources.Load<TextAsset>("Data/DiscoveryOrder").ToString().Split(new char[]
                            {
                        '\n'
                            });
                            array = ErrorBlameMod.GetDialogue("Discoveries").Split(new char[]
                            {
                        '\n'
                            });
                            List<string> list = new List<string>();
                            MainManager.discoveryicons = new int[MainManager.librarylimit[0]];
                            for (int num4 = 0; num4 < MainManager.librarylimit[0]; num4++)
                            {
                                string[] array8 = array7[num4].Split(new char[]
                                {
                            ','
                                });
                                MainManager.discoveryicons[num4] = Convert.ToInt32(array8[1]);
                                list.Add(array8[0]);
                            }
                            array7 = list.ToArray();
                            break;
                        }
                    case 1:
                        array7 = Resources.Load<TextAsset>("Data/TattleList").ToString().Split(new char[]
                        {
                        '\n'
                        });
                        array = ErrorBlameMod.GetDialogue("EnemyTattle").Split(new char[]
                        {
                        '\n'
                        });
                        break;
                    case 2:
                        array7 = Resources.Load<TextAsset>("Data/CookOrder").ToString().Split(new char[]
                        {
                        '\n'
                        });
                        array = Resources.Load<TextAsset>("Data/CookLibrary").ToString().Split(new char[]
                        {
                        '\n'
                        });
                        break;
                    case 3:
                        {
                            array7 = Resources.Load<TextAsset>("Data/SynopsisOrder").ToString().Split(new char[]
                            {
                        '\n'
                            });
                            array = ErrorBlameMod.GetDialogue("Synopsis").Split(new char[]
                            {
                        '\n'
                            });
                            List<string> list2 = new List<string>();
                            MainManager.achiveicons = new int[MainManager.librarylimit[3]];
                            for (int num5 = 0; num5 < MainManager.librarylimit[3]; num5++)
                            {
                                string[] array9 = array7[num5].Split(new char[]
                                {
                            ','
                                });
                                MainManager.achiveicons[num5] = Convert.ToInt32(array9[1]);
                                list2.Add(array9[0]);
                            }
                            array7 = list2.ToArray();
                            break;
                        }
                    case 4:
                        array7 = Resources.Load<TextAsset>("Data/MapOrder").ToString().Split(new char[]
                        {
                        '\n'
                        });
                        array = Resources.Load<TextAsset>("Data/Dialogues0/LibraryMap").ToString().Split(new char[]
                        {
                        '\n'
                        });
                        break;
                }
                for (int num6 = 0; num6 < array.Length; num6++)
                {
                    array2 = array[num6].Split(new char[]
                    {
                        '@'
                    });
                    for (int num7 = 0; num7 < array2.Length; num7++)
                    {
                        MainManager.librarydata[num3, num6, num7] = array2[num7];
                    }
                }
                for (int num8 = 0; num8 < array7.Length; num8++)
                {
                    MainManager.libraryorder[num3, num8] = Convert.ToInt32(array7[num8]);
                }
            }
            __instance.boardquests = new List<int>[3];
            for (int num9 = 0; num9 < __instance.boardquests.Length; num9++)
            {
                __instance.boardquests[num9] = new List<int>
                {
                    0
                };
            }
            array = ErrorBlameMod.GetDialogue("EnemyTattle").Split(new char[]
            {
                '\n'
            });
            MainManager.enemynames = new string[array.Length];
            for (int num10 = 0; num10 < array.Length; num10++)
            {
                MainManager.enemynames[num10] = array[num10].Split(new char[]
                {
                    '@'
                })[0];
            }
        }
    }

    //lore text 1
    [HarmonyPatch(typeof(MainManager), "GetDialogueText")]
    public class GetDialogueTextPatch
    {
        [HarmonyPrefix]
        private static bool GetDialogueTextPrefix(int id, ref string __result)
        {
            bool flag = id != -66;
            bool result;
            if (flag)
            {
                result = true;
            }
            else
            {
                __result = "|tail,null||boxstyle,3||bleep,2,1,1|";
                MainManager.instance.flagstring[0] = ErrorBlameMod.GetDialogue("LoreText").Split(new char[]
                {
                    '\n'
                })[MainManager.instance.flagvar[0]].Split(new char[]
                {
                    '@'
                })[1] + "|break||hide||fwait,0.05||boxstyle,3||fwait,0.05||hide||goto,1|";
                __result += MainManager.instance.flagstring[0];
                result = false;
            }
            return result;
        }
    }

    //lore text titles?
    [HarmonyPatch(typeof(MainManager), "ShowItemList", 0)]
    public class ShowItemListPatch
    {
        [HarmonyPostfix]
        private static void ShowItemListPostfix(int type, Vector2 position)
        {
            bool flag = type == 22;
            if (flag)
            {
                float num = 0.25f;
                for (int i = 0; i < MainManager.instance.itemlist.childCount; i++)
                {
                    MainManager.DestroyText(MainManager.instance.itemlist.GetChild(i), false);
                }
                UnityEngine.Object.Destroy(MainManager.instance.itemlist.gameObject);
                MainManager.instance.itemlist = new GameObject("ItemList").transform;
                MainManager.instance.itemlist.parent = MainManager.GUICamera.transform;
                MainManager.instance.itemlist.localPosition = new Vector3(position.x, position.y, 10f);
                bool flag2 = MainManager.listlow > 0 && MainManager.instance.maxoptions > MainManager.listammount;
                if (flag2)
                {
                    SpriteRenderer spriteRenderer = new GameObject("UpArrow").AddComponent<SpriteRenderer>();
                    spriteRenderer.gameObject.layer = 5;
                    spriteRenderer.transform.parent = MainManager.instance.itemlist;
                    spriteRenderer.sprite = MainManager.guisprites[3];
                    spriteRenderer.transform.localPosition = new Vector2(1f, num + 0.7f);
                    spriteRenderer.transform.localEulerAngles = new Vector3(0f, 0f, 180f);
                    spriteRenderer.sortingOrder = 3;
                }
                for (int j = MainManager.listlow; j < MainManager.listmax; j++)
                {
                    bool flag3 = j >= MainManager.instance.maxoptions;
                    if (flag3)
                    {
                        break;
                    }
                    SpriteRenderer spriteRenderer2 = new GameObject("Bar" + j.ToString()).AddComponent<SpriteRenderer>();
                    bool flag4 = !MainManager.instance.pause;
                    if (flag4)
                    {
                        spriteRenderer2.sprite = MainManager.guisprites[0];
                        spriteRenderer2.transform.localScale = new Vector3(1.15f, 1f, 1f);
                    }
                    spriteRenderer2.gameObject.layer = 5;
                    spriteRenderer2.transform.parent = MainManager.instance.itemlist;
                    spriteRenderer2.sortingOrder = -1;
                    spriteRenderer2.transform.localPosition = new Vector2(1.4f, num);
                    spriteRenderer2.color = new Color(1f, 1f, 1f, 0.75f);
                    num -= 0.7f;
                    string[] array = ErrorBlameMod.GetDialogue("LoreText").Split(new char[]
                    {
                        '\n'
                    })[j].Split(new char[]
                    {
                        '@'
                    });
                    bool flag5 = j == MainManager.instance.option;
                    if (flag5)
                    {
                        MainManager.instance.flagstring[0] = array[1];
                    }
                    MainManager.instance.StartCoroutine(MainManager.SetText(((!MainManager.instance.pause) ? "|size,0.6,0.8|" : string.Empty) + array[0], 0, null, false, false, new Vector3(-2.65f, -0.15f), Vector3.zero, new Vector2(0.75f, 0.75f), spriteRenderer2.transform, null));
                }
                bool flag6 = MainManager.instance.maxoptions > MainManager.listammount && MainManager.instance.option < MainManager.instance.maxoptions - 1 && MainManager.listlow < MainManager.instance.maxoptions - MainManager.listammount;
                if (flag6)
                {
                    SpriteRenderer spriteRenderer3 = new GameObject("DownArrow").AddComponent<SpriteRenderer>();
                    spriteRenderer3.gameObject.layer = 5;
                    spriteRenderer3.transform.parent = MainManager.instance.itemlist;
                    spriteRenderer3.transform.localPosition = new Vector2(1f, num);
                    spriteRenderer3.sprite = MainManager.guisprites[3];
                    spriteRenderer3.sortingOrder = 3;
                }
                MainManager.instance.itemlist.localEulerAngles = Vector3.zero;
            }
        }
    }

    [HarmonyPatch(typeof(CardGame), "LoadCardData")]
    public class LoadCardDataPatch
    {
        [HarmonyPrefix]
        private static bool LoadCardDataPrefix(CardGame __instance, ref int[] ___boss, ref int[] ___miniboss)
        {
            string[] array = ErrorBlameMod.GetDialogue("CardText").Split(new char[]
            {
        '\n'
            });
            string[] array2 = Resources.Load<TextAsset>("Data/CardData").ToString().Split(new char[]
            {
        '\n'
            });
            __instance.carddata = new CardGame.CardData[array2.Length];
            List<int> list = new List<int>();
            List<int> list2 = new List<int>();
            for (int i = 0; i < array2.Length; i++)
            {
                string[] array3 = array2[i].Split(new char[]
                {
            ','
                });
                __instance.carddata[i].noid = i;
                __instance.carddata[i].tp = Convert.ToInt32(array3[0]);
                __instance.carddata[i].attack = Convert.ToInt32(array3[1]);
                __instance.carddata[i].enemyid = Convert.ToInt32(array3[2]);
                __instance.carddata[i].namesizeX = (float)Convert.ToDouble(array[i].Split(new char[]
                {
                    '@'
                })[1]);
                __instance.carddata[i].type = (CardGame.Type)Convert.ToInt32(array3[4]);
                bool flag3 = array3[5].Length > 0;
                if (flag3)
                {
                    string[] array4 = array3[5].Split(new char[]
                    {
                        '@'
                    });
                    __instance.carddata[i].effects = new int[array4.Length, 3];
                    for (int j = 0; j < array4.Length; j++)
                    {
                        string[] array5 = array4[j].Split(new char[]
                        {
                    '#'
                        });
                        for (int k = 0; k < array5.Length; k++)
                        {
                            __instance.carddata[i].effects[j, k] = Convert.ToInt32(array5[k]);
                        }
                    }
                }
                else
                {
                    __instance.carddata[i].effects = null;
                }
                string[] array6 = array3[6].Split(new char[]
                {
            '@'
                });
                __instance.carddata[i].tribe = new CardGame.Tribe[array6.Length];
                for (int l = 0; l < array6.Length; l++)
                {
                    __instance.carddata[i].tribe[l] = (CardGame.Tribe)Convert.ToInt32(array6[l]);
                }
                bool flag4 = __instance.carddata[i].type == CardGame.Type.Boss;
                if (flag4)
                {
                    list.Add(i);
                }
                else
                {
                    bool flag5 = __instance.carddata[i].type == CardGame.Type.Miniboss;
                    if (flag5)
                    {
                        list2.Add(i);
                    }
                }
                __instance.carddata[i].desc = array[i].Split(new char[]
                {
            '@'
                })[0];
            }
            ___boss = list.ToArray();
            ___miniboss = list2.ToArray();
            return false;
        }
    }


    [HarmonyPatch(typeof(CardGame), "StartCard")]
    public class StartCardPatch
    {
        [HarmonyPrefix]
        private static bool StartCardPrefix()
        {
            BuildWindowPatch.cardLoad = true;
            return true;
        }
    }

    /*
    [HarmonyPatch(typeof(CardGame), "GetInput")]
    [HarmonyPrefix]
    private static void GetInputPrefix()
    {
        cardLoad = false;
    }
    */

    [HarmonyPatch(typeof(CardGame), "BuildWindow")]
    public class BuildWindowPatch
    {
        public static bool cardLoad;

        [HarmonyPrefix]
        private static bool BuildWindowPrefix(CardGame __instance)
        {
            //don't load if you don't need to
            if (cardLoad)
            {
                cardLoad = false;
                __instance.carddiag = ErrorBlameMod.GetDialogue("CardDialogue").Split(new char[]
                {
                    '\n'
                });
            }
            return true;
        }
    }


    //Rank up text
    //They are all sprites
    //AddExperience

    //ok, AddExperience is a bit too big to rewrite
    //time for some shady programming techniques

    [HarmonyPatch(typeof(BattleControl), "AddExperience")]
    public class AddExperiencePatch
    {
        //public static bool addExp = false;
        public static Sprite[] sprites;

        [HarmonyPrefix]
        public static bool AddExperiencePrefix()
        {
            if (sprites == null)
            {
                Sprite[] spriteholder = ErrorBlameMod.bundle.LoadAllAssets<Sprite>();
                //5 sprites
                //after the 6 battlem sprites
                sprites = new Sprite[6];

                //ErrorBlameMod.logSource.LogInfo("Loaded letters.");

                for (int i = 0; i < sprites.Length; i++)
                {
                    sprites[i] = spriteholder[i + 6];
                }
            }

            //addExp = true;
            return true;
        }

        /*
        [HarmonyPostfix]
        public static void AddExperiencePostfix()
        {
            addExp = false;
        }
        */
    }

    [HarmonyPatch(typeof(MainManager), "NewUIObject", new Type[]
        {
    typeof(string),
    typeof(Transform),
    typeof(Vector3),
    typeof(Vector3),
    typeof(Sprite),
    typeof(int)
    })]
    public class NewUIObjectPatch
    {
        [HarmonyPrefix]
        public static bool NewUIObjectPrefix(string objname, ref Sprite sprite, ref Vector3 size)
        {
            if (objname.Contains("letter"))// && AddExperiencePatch.addExp)
            {
                //replace the sprite
                int number = int.Parse(objname.Substring(6, 1));
                //old rank up text has too many letters
                //ErrorBlameMod.logSource.LogInfo(number);
                if (number > 5)
                {
                    //make it microscopic
                    size = Vector3.one * 0.001f;

                    //nope
                    //return false;
                    return true;
                }
                sprite = AddExperiencePatch.sprites[number];        
            }
            return true;
        }

    }
}
