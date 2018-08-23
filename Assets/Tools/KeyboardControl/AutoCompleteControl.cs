﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
/*
 * Responsible for inserting new Words in the Dictionary, but also for applying autocomplete-Suggestion selected by the user.
 * It's uses an Interface (IEnteredText) to get the saved text by the user in order to insert new words.
 * The Keyboard is the Caller.
 */
public class AutoCompleteControl : MonoBehaviour{
	//When insert a new word?
	/* Wort: Ende/Anfangsmarker: "Leerzeichen"; ","; "."; "!"; "?"; ";" 
	 * 
	 */
	//Seperation signs for marking words
	private string[] seperator = {" ",",",".","!","?",";"};
	//Inputfield of the keyboard
	public InputField input;
	public KeyboardControl keyboardControl;
	//Contains the possible suggestions
	public Text suggestionTop;
	public Text suggestionMiddle;
	public Text suggestionBottom;

	//Dictionary for Autocomplete
	DictEntryMultyWord autoCompleteDic = new DictEntryMultyWord ();
	// Use this for initialization 
	void Start () {}
	
	// Update is called once per frame
	void Update () {}


	//Searching the text for words in order to find likely matches
	public void suggestWords(){
		List<DictEntrySingleWord> tempLikelyWords = new List<DictEntrySingleWord>();
		/*<DictEntrySingleWord> stringlist = autoCompleteDic.getSortedLikelyWordsAfterRate("");
		foreach (DictEntrySingleWord s in stringlist)
			Debug.Log( "Words: " + s.getWord() );
		*/
		int lastSymbolIndex = this.input.text.Length;
		//Debug.Log ("lastSymbolIndex:" + lastSymbolIndex);
		//Debug.Log("lastSysmbol: "+this.input.text.Substring(lastSymbolIndex-1));
		bool isLastSymbolSeperator = false;
		for (int i = 0; i < this.seperator.Length; i++) {
			//Debug.Log (this.input.text.Substring(lastSymbolIndex-1).CompareTo(this.seperator[i])==0);
			if (lastSymbolIndex>0 && this.input.text.Substring(lastSymbolIndex-1).CompareTo(this.seperator[i])==0) {
				isLastSymbolSeperator = true;
				break;
			}
		}
		//show only word suggestions if the last symbol/symbols is not a seperator
		if (!isLastSymbolSeperator) {			
			string[] words = this.getWordsFromInput (this.input.text);
			if (words != null & words.Length > 0)
				tempLikelyWords = autoCompleteDic.getSortedLikelyWordsAfterRate (words [words.Length - 1]);
			DictEntrySingleWord[] suggestArray = tempLikelyWords.ToArray ();
			/*
			 * show only button's with word-suggestions; if it has not a word-suggestion deatcivate it
			 */
			Button[] suggestionButtons = this.GetComponentsInChildren<Button> (true);
			if (suggestArray.Length > 0) {				
				for (int i = 0; i < suggestionButtons.Length; i++) {
					//Debug.Log ("length:" + (i) + ":" + suggestArray.Length + " ");
					if (i < suggestArray.Length && suggestArray [i] != null) {
						suggestionButtons [i].GetComponentInChildren<Text> ().text = suggestArray [i].getWord ();
						suggestionButtons [i].gameObject.SetActive (true);
						this.adaptTextToButtonSize (suggestionButtons [i]);
					} else {
						suggestionButtons [i].gameObject.SetActive (false);
					}
				}
				this.gameObject.SetActive (true);
			} else {
				this.gameObject.SetActive (false);
			}
		} else {
			//Debug.Log ("name:" + this.gameObject.name);
			suggestionTop.text = "";
			suggestionMiddle.text = "";
			suggestionBottom.text = "";
			this.gameObject.SetActive (false);
		}
	}
	//show's not the whole word, if it's too big for the button
	private void adaptTextToButtonSize(Button button){
		float widthButton = Mathf.Abs(button.GetComponent<RectTransform> ().rect.width);
		Text textButton = button.GetComponentInChildren<Text> ();
		float widthText = Mathf.Abs(textButton.preferredWidth);
		int startIndexOfWordFromLeft = textButton.text.Length-1;
		//Debug.Log ("text:" + textButton.text);
		Debug.Log ("widthButton: " + widthButton);
		Debug.Log ("widthText: " + widthText);
		string tempText = textButton.text;
		do {
			textButton.text = tempText;
			//Debug.Log("widthTextOriginal:"+textButton.preferredWidth);
			textButton.text = "..." + textButton.text.Substring (startIndexOfWordFromLeft);
			//Debug.Log ("text:" + textButton.text);
			startIndexOfWordFromLeft--;
			widthText = Mathf.Abs (textButton.preferredWidth);
			//Debug.Log ("widthText: " + widthText);
		} while (widthText <= (widthButton-20) & startIndexOfWordFromLeft > 0);
	}

	//called by the keyboard to add eventually new words to the dictionary
	public void enteredText(string text){
		string[] words = this.getWordsFromInput (text);
		if (words != null) {
			for (int i = 0; i < words.Length; i++) {
				autoCompleteDic.insert (words [i]);
			}		
		}
		//TestDictionary t = new TestDictionary ();
		/*List<DictEntrySingleWord> stringlist = autoCompleteDic.getSortedLikelyWordsAfterRate("");
		foreach (DictEntrySingleWord s in stringlist)
			Debug.Log( "Words: " + s.getWord() );
		*/
	}

	//Applie's the suggestion-word in the inputfield of the keyboard, selected by the user
	public void applySuggestion(){
		GameObject selected = EventSystem.current.currentSelectedGameObject;
		if (selected != null) {
			//selected suggested-word
			string suggestionText = selected.GetComponentInChildren<Text> ().text;
			int lastIndex = 0;
			for (int i = 0; i < seperator.Length; i++) {
				lastIndex = Mathf.Max(lastIndex,input.text.LastIndexOf(seperator[i]));
			}
			if (lastIndex > 0)
				lastIndex++;
			//Debug.Log("LastIndexOF:"+lastIndex);
			this.keyboardControl.deleteText (lastIndex);
			this.keyboardControl.enterTextEvent (suggestionText);
			InputDeviceManager.instance.shakeLeftController( 0.5f, 0.15f );
		}
	}
	//Extract all words from a given text
	private string[] getWordsFromInput(string text){
		if (text != null) {			
			string[] words = text.Split (seperator, System.StringSplitOptions.RemoveEmptyEntries);
			//for (int i = 0; i < words.Length; i++) {
				//Debug.Log ("word: " + words [i]);
			//}
			return words;
		}
		return null;
	}
}