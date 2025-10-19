using TMPro;
using UnityEngine;
using Gley.Localization;


public class LanguageManager : MonoBehaviour
{
    /// <summary>
    ///You need to add all the texts in the scipts to change the language
    /// </summary>
    /// 

    [Header("Language Selection")]
    public GameObject langSelPanel;
    public AdvancedDropdown languageDropdown;
    public TMP_Text dropDownHeader;

    [Header("Main Menu Texts For Translation")]
    public TMP_Text currentLanguageTxt;
    public TMP_Text settingsTxt;
    public TMP_Text quitTxt;
    public TMP_Text shopTxt;
    public TMP_Text giftTxt;
    public TMP_Text messageTxt;
    public TMP_Text missionTxt;
    public TMP_Text selectLangTxt;



    private void Start()
    {
        RefreshText();

        if (PlayerPrefs.GetInt("DefaultSet", 0) != 1)
        {
            OnLanguageChanged(1);
            PlayerPrefs.SetInt("DefaultSet", 1);
        }
    }

    private void RefreshText()
    {
        SetCurrentLocalLanguageName();
        settingsTxt.text = API.GetText(WordIDs.Settings_Id);
        quitTxt.text = API.GetText(WordIDs.Quit_Id);
        shopTxt.text = API.GetText(WordIDs.Store_Id);
        giftTxt.text = API.GetText(WordIDs.GIFTS_Id);
        messageTxt.text = API.GetText(WordIDs.MESSAGES_Id);
        missionTxt.text = API.GetText(WordIDs.MISSION_Id);
        selectLangTxt.text = API.GetText(WordIDs.SelectLang_Id);
    }

    void SetCurrentLocalLanguageName()
    {
        string name = "";
        switch (API.GetCurrentLanguage().ToString())
        {
            case "English":
                name = "English";
                break;
            case "German":
                name = "Deutsch";
                break;
            case "Spanish":
                name = "Español";
                break;
            case "Italian":
                name = "Italiano";
                break;
            case "French":
                name = "Français";
                break;
        }

        currentLanguageTxt.text = name;
        dropDownHeader.text = name;

    }

    string[] languages = { "English", "German", "Italian", "French", "Spanish" };
    public void EnableLanguagePanel()
    {
        langSelPanel.SetActive(true);
        languageDropdown.onChangedValue += OnLanguageChanged;
    }

    void OnLanguageChanged(int index)
    {
        // Here you can call your localization system or apply changes
        switch (index)
        {
            case 0:
                API.SetCurrentLanguage(SupportedLanguages.English);
                break;
            case 1:
                API.SetCurrentLanguage(SupportedLanguages.German);
                break;
            case 2:
                API.SetCurrentLanguage(SupportedLanguages.Italian);
                break;
            case 3:
                API.SetCurrentLanguage(SupportedLanguages.French);
                break;
            case 4:
                API.SetCurrentLanguage(SupportedLanguages.Spanish);
                break;
        }

        RefreshText();
        SaveLanguage();
    }

    public void NextLanguage()
    {
        API.NextLanguage();
        RefreshText();
        SaveLanguage();
    }


    private void SaveLanguage()
    {
        API.SetCurrentLanguage(API.GetCurrentLanguage());
    }
}
