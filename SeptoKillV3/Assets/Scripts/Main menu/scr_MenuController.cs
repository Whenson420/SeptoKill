using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using static Models;

public class scr_MenuController : MonoBehaviour
{
    public PlayerSettingsModel playerSettings;
    [Header("Level To Load")]
    public string NewGameLevel = null;
    public bool AreYouSure = false;
    public Button EndlessModeButton = null;

    [Header("Sound settings")]
    [SerializeField] private TMP_Text MasterVolumeTextValue = null;
    [SerializeField] private Slider MasterVolumeSlider = null;
    [SerializeField] private GameObject confirmationPrompt = null;
    [SerializeField] private float defaultMasterVolume = 1.0f;

    [Header("Gameplay")]
    [SerializeField] private TMP_InputField Sensitivity = null;
    [SerializeField] private Toggle invertY = null;
    [SerializeField] private Toggle invertX = null;
    private float defaultSens = 4.0f;

    #region Main
    public void EndlessGame()
    {
        if (AreYouSure)
        {
            playerSettings.ViewXSensitivity  = PlayerPrefs.GetFloat("Sensitivity");
            playerSettings.ViewYSensitivity = playerSettings.ViewXSensitivity;
            if (PlayerPrefs.GetInt("invertX") == 1)
            {
                playerSettings.ViewXInverted = true;
            }
            else
            {
                playerSettings.ViewXInverted = false;

            }
            if (PlayerPrefs.GetInt("invertY") == 1)
            {
                playerSettings.ViewYInverted = true;
            }
            else
            {
                playerSettings.ViewYInverted = false;

            }
            SceneManager.LoadScene(NewGameLevel);
        }
        else
        {
            AreYouSure = true;
        }
    }
    public void ExitButton()
    {
        Application.Quit();
    }
    #endregion
    #region Sound
    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = volume;
        MasterVolumeTextValue.text = volume.ToString("0.0");
    }
    public void VolumeApply()
    {
        PlayerPrefs.SetFloat("masterVolume", AudioListener.volume);
        StartCoroutine(ConfirmationBox());
    }
    #endregion
    #region Gameplay
    public void ApplyGameplay()
    {
        PlayerPrefs.SetFloat("Sensitivity", float.Parse(Sensitivity.text));
        PlayerPrefs.SetInt("invertX", invertX ? 1 : 0);
        PlayerPrefs.SetInt("invertY", invertY ? 1 : 0);
        StartCoroutine(ConfirmationBox());
    }
    #endregion
    public void ResetButton(string MenuType)
    {
        if (MenuType == "Audio")
        {
            AudioListener.volume = defaultMasterVolume;
            MasterVolumeSlider.value = defaultMasterVolume;
            MasterVolumeTextValue.text = defaultMasterVolume.ToString("0.0");
            VolumeApply();
        }
        if (MenuType == "Gameplay")
        {
            Sensitivity.text = defaultSens.ToString();
            invertX.isOn = false;
            invertY.isOn = false;
            ApplyGameplay();
        }
    }
    public IEnumerator ConfirmationBox()
    {
        confirmationPrompt.SetActive(true);
        yield return new WaitForSeconds(2);
        confirmationPrompt.SetActive(false);
    }

}
