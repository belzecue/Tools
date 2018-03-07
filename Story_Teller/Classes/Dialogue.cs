﻿using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using PlayerAndEditorGUI;



namespace StoryTriggerData
{

    public static class Dialogue {

        public static InteractionBranch root { get {return browsedObj.iGroup; } }

        public static string singleText { get { return _optText.Count > 0 ? _optText[0] : null; } set { _optText.Clear(); _optText.Add(value); } }

        public static STD_Values browsedObj;

        public static List<string> _optText = new List<string>();
        static List<Interaction> possibleInteractions = new List<Interaction>();
        static List<DialogueChoice> possibleOptions = new List<DialogueChoice>();



        public static bool ScrollOptsDirty;

        static bool checkOptions(Interaction ia) {
            clearTexts();
            Debug.Log("Adding options ");
            int cnt = 0;
            //for (int i = 0; i < tmp.Count; i++)
            foreach (DialogueChoice dio in ia.options)  
            if (dio.conditions.TestConditions(browsedObj)) {
                    _optText.Add(dio.text.ToString());
                    possibleOptions.Add(dio);
                    cnt++;
                    Debug.Log("Adding options " + cnt);
            }

            ScrollOptsDirty = true;

            QuestVersion = STD_Values.questVersion;

            if (cnt > 0)
                return true;
            else
                return false;
        }


        static void updatePassiveLogic(InteractionBranch gr) {

            foreach (Interaction si in gr.interactions){
                if (browsedObj.type == QOoptionType.PassiveLogic)
                    if (si.conditions.TestConditions(browsedObj)) {

                        for (int j = 0; j < si.options.Count; j++)
                            if (si.options[j].conditions.TestConditions(browsedObj))
                                si.options[j].results.apply(browsedObj);
                        si.FinalResults.apply(browsedObj);
                    }
            }

            foreach (InteractionBranch sgr in gr.interactionBranches)
                updatePassiveLogic(sgr);
        }

        static void CollectInteractions(InteractionBranch gr) {
            //for (int i = 0; i < root.Interactions.Count; i++) {
            foreach (Interaction si in gr.interactions) {
              //  StoryInteraction tmp = root.Interactions[i];
                if (browsedObj.type == QOoptionType.Dialogue) {
                    if (si.conditions.TestConditions(browsedObj)) {
                        _optText.Add(si.Texts[0].ToString());
                        possibleInteractions.Add(si); textCount++;
                    }
                }
            }
        }


        static int textCount;

       

       

        public static void BackToInitials() {
            STD_Values.AddQuestVersion();
            clearTexts();

            updatePassiveLogic(root);

            textCount = 0;
            CollectInteractions(root);

            if (textCount == 0)
                CloseInteractions();
            else {

                QuestVersion = STD_Values.questVersion;
                ScrollOptsDirty = true;

                InteractionStage = 0;
                textNo = 0;

                if (continuationReference != null) {
                    foreach (var ie in possibleInteractions)
                        if (ie.reference == continuationReference) {
                            //Debug.Log("Found continuation! Skipping: " + ie.Texts[0]);
                            interaction = ie;
                            InteractionStage++;
                            SelectOption(0);
                            break;
                        }
                }

              
            }
        }

        public static void StartInteractions(STD_Values so) {
            so.OnEnterResults.apply(so);
            browsedObj = so;
            BackToInitials();
           
        }

        public static void CloseInteractions() {
            if (browsedObj != null)
                browsedObj.OnExitResults.apply(browsedObj);
            
            browsedObj = null;
        }

        public static int textNo;
        public static int InteractionStage;

        static Interaction interaction;
        static DialogueChoice option;

        static int QuestVersion;
        public static void DistantUpdate() {
            if (root != null) {
                if (QuestVersion != STD_Values.questVersion) {

                    switch (InteractionStage) {
                        case 0: BackToInitials(); break;
                        case 1: gotBigText(); break;
                        case 3: checkOptions(interaction); break;
                        case 5:
                            List<Sentance> tmp = option.texts2;
                            if (tmp.Count > textNo) 
                                singleText = tmp[textNo].ToString();
                            break;
                    }
                    QuestVersion = STD_Values.questVersion;
                }
            }
        }

        static void clearTexts(){
            _optText.Clear();
            ScrollOptsDirty = true;
            possibleInteractions.Clear();
        }

        static bool gotBigText()
        {
            if (textNo < interaction.Texts.Count) {
                singleText=interaction.Texts[textNo].ToString(); 
                return true;
            }
            return false;
        }


        static string continuationReference;
        public static void SelectOption(int no) {
            //Debug.Log("Selecting: " + no);
            STD_Values.AddQuestVersion();
            //int actual = possibleInteractions.Count > 0 ? possibleInteractions[no] : 0;
            switch (InteractionStage)
            {
                case 0: //Debug.Log("case 0");
                    InteractionStage++; interaction = possibleInteractions[no]; goto case 1;
                case 1:
                    continuationReference = null;
                    //Debug.Log("case 1"); 
                    textNo++;
                    if (gotBigText()) break;
                    InteractionStage++;
                    goto case 2;
                case 2:
                    //Debug.Log("case 2"); 
                    InteractionStage++;
                    if (!checkOptions(interaction)) goto case 4; break; // if no options available
                case 3:
                    //Debug.Log("case 3");
                    option = possibleOptions[no];
                    option.results.apply( browsedObj);
                    continuationReference = option.goToReference;
                    interaction.FinalResults.apply(browsedObj);
                    textNo = -1;
                    goto case 5;

                case 4: //Debug.Log("case 4"); 
                    interaction.FinalResults.apply( browsedObj); BackToInitials(); break;

                case 5:
                    //Debug.Log("In case 5");
                    List<Sentance> txts = option.texts2;
                    if ((txts.Count > textNo + 1)) {
                        textNo++;
                        singleText = txts[textNo].ToString();
                        InteractionStage = 5;
                        break;
                    }
                    goto case 6;
                case 6:

                    BackToInitials();
                    break;
            }
        }


        public static bool PEGI(STD_Values trg) {
            bool changed = false;

            if (browsedObj != trg) {

                pegi.ClickToEditScript();

                if (icon.Play.Click("Play dialogue",20)) StartInteractions(trg);
                    
            } else {
                if (pegi.Click(icon.Close, "Close dialogue", 20))
                    CloseInteractions();
                else {
                    pegi.newLine();
                        for (int i = 0; i < _optText.Count; i++) {
                        if (pegi.Click(_optText[i])) {
                            SelectOption(i);
                            DistantUpdate();
                        }

                            pegi.newLine();
                        }
                }

            }

            return changed;
        }

    }
}