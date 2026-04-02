using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    [Header("UI References")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;
    public Toggle vsyncToggle;

    [Header("Buttons")]
    public Button applyButton;
    public Button defaultButton;

    private Resolution[] resolutions;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeResolutions();
    }

    private void Start()
    {
        LoadAndApplySettings();

        if (applyButton != null) applyButton.onClick.AddListener(ApplySettings);
        if (defaultButton != null) defaultButton.onClick.AddListener(ResetToDefault);
    }

    private void InitializeResolutions()
    {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height + " @" + (int)resolutions[i].refreshRateRatio.value + "Hz";
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.RefreshShownValue();
    }

    // --- BUTTON ACTIONS ---

    public void ApplySettings()
    {
        int resIndex = resolutionDropdown.value;
        bool isFullscreen = fullscreenToggle.isOn;
        bool isVsync = vsyncToggle.isOn;

        Resolution res = resolutions[resIndex];
        Screen.SetResolution(res.width, res.height, isFullscreen);
        QualitySettings.vSyncCount = isVsync ? 1 : 0;

        PlayerPrefs.SetInt("ResIndex", resIndex);
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.SetInt("VSync", isVsync ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log("Settings Applied and Saved.");
    }

    public void ResetToDefault()
    {
        resolutionDropdown.value = resolutions.Length - 1;
        fullscreenToggle.isOn = true;
        vsyncToggle.isOn = true;

        ApplySettings();
    }

    private void LoadAndApplySettings()
    {
        int resIndex = PlayerPrefs.GetInt("ResIndex", resolutions.Length - 1);
        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        bool isVsync = PlayerPrefs.GetInt("VSync", 1) == 1;

        resolutionDropdown.value = resIndex;
        fullscreenToggle.isOn = isFullscreen;
        vsyncToggle.isOn = isVsync;

        Resolution res = resolutions[resIndex];
        Screen.SetResolution(res.width, res.height, isFullscreen);
        QualitySettings.vSyncCount = isVsync ? 1 : 0;
    }
}