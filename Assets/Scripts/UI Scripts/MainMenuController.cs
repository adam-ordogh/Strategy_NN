using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public string gameSceneName = "GameScene";

    public GameObject menuPanel;
    public GameObject newGamePanel;
    public GameObject loadGamePanel;
    public GameObject settingsPanel;

    public SaveLoadUI saveLoadUI;

    [Header("Game Settings")]
    public TMP_Dropdown mapSeedDropdown;
    public TMP_Dropdown opponentDropdown;

    [Header("Map Preview")]
    public Image mapPreviewImage; 
    public int mapWidth = 50;     
    public int mapHeight = 50;

    private int currentPreviewSeed;

    private Dictionary<string, int> presetSeeds = new Dictionary<string, int>()
    {
        { "Hegyi Átkelő", 1423 },
        { "Erdei Mezők", 9982 },
        { "Sűrű Rengeteg", 0 },
        { "Random", -1 } 
    };

    private void Start()
    {
        if (mapSeedDropdown != null)
        {
            mapSeedDropdown.onValueChanged.AddListener(delegate { UpdateMapPreview(); });
        }
    }

    // --- NEW GAME PANEL---
    public void ToggleNewGamePanel()
    {
        bool isOpening = newGamePanel.activeSelf;

        newGamePanel.SetActive(!isOpening);
        menuPanel.SetActive(isOpening);

        if (!isOpening)
        {
            UpdateMapPreview();
        }
    }

    // --- CONTINUE GAME ---
    public void OnContinueClicked()
    {
        string path = Path.Combine(Application.persistentDataPath, "Saves");
        if (Directory.Exists(path))
        {
            var directory = new DirectoryInfo(path);
            var newestFile = directory.GetFiles("*.json").OrderByDescending(f => f.LastWriteTime).FirstOrDefault();

            if (newestFile != null)
            {
                LevelLoadBridge.SaveFileToLoad = Path.GetFileNameWithoutExtension(newestFile.Name);
                SceneManager.LoadScene(gameSceneName);
                return;
            }
        }
        Debug.Log("No saves found to continue.");
    }

    // --- LOAD GAME ---

    public void ToggleLoadGamePanel()
    {
        bool isOpening = !loadGamePanel.activeSelf;

        loadGamePanel.SetActive(isOpening);
        menuPanel.SetActive(!isOpening); 

        if (isOpening)
        {
            saveLoadUI.OpenPanel(SaveLoadUI.UIMode.Load);
        }
    }

    // --- NEW GAME ---
    public void OnStartNewGameClicked()
    {
        LevelLoadBridge.SaveFileToLoad = "";

        string selectedName = mapSeedDropdown.options[mapSeedDropdown.value].text;

        if (presetSeeds.ContainsKey(selectedName))
        {
            int seed = presetSeeds[selectedName];

            if (seed == -1) seed = Random.Range(0, 100000);

            LevelLoadBridge.MapSeed = seed;
        }

        if (opponentDropdown != null)
        {
            string aiChoice = opponentDropdown.options[opponentDropdown.value].text;
            if (aiChoice.Contains("Tanított") || aiChoice.Contains("Machine Learning"))
            {
                LevelLoadBridge.OpponentType = AIFactory.AIType.MLBasic;
            }
            else
            {
                LevelLoadBridge.OpponentType = AIFactory.AIType.Deterministic;
            }
        }

        SceneManager.LoadScene(gameSceneName);
    }

    // --- SETTINGS ---
    public void ToggleSettingsPanel()
    {
        bool isOpening = !settingsPanel.activeSelf;
        settingsPanel.SetActive(isOpening);
        menuPanel.SetActive(!isOpening);
    }

    // --- EXIT GAME ---
    public void OnExitClicked()
    {
        Debug.Log("Exiting Game...");
        Application.Quit();
    }


    public void UpdateMapPreview()
    {
        if (mapPreviewImage == null) return;

        string selectedName = mapSeedDropdown.options[mapSeedDropdown.value].text;

        if (presetSeeds.ContainsKey(selectedName))
        {
            currentPreviewSeed = presetSeeds[selectedName];

            if (currentPreviewSeed == -1)
            {
                currentPreviewSeed = Random.Range(0, 100000);
            }
        }

        Texture2D tex = GeneratePreviewTexture(currentPreviewSeed);
        Debug.Log($"Selected preset: {selectedName} with seed {currentPreviewSeed}");

        mapPreviewImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    private Texture2D GeneratePreviewTexture(int seed)
    {
        Texture2D tex = new Texture2D(mapWidth, mapHeight);
        tex.filterMode = FilterMode.Point; 

        Random.InitState(seed);
        float offsetX = Random.Range(-100000f, 100000f);
        float offsetY = Random.Range(-100000f, 100000f);

        Color[] pixels = new Color[mapWidth * mapHeight];

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y <= x && y < mapHeight; y++)
            {
                float noise = Mathf.PerlinNoise((x + offsetX) * 0.1f, (y + offsetY) * 0.15f);

                Color pixelColor;
                if (noise < 0.3f) pixelColor = new Color(0.1f, 0.4f, 0.1f);      // Forest
                else if (noise < 0.7f) pixelColor = new Color(0.2f, 0.6f, 0.2f); // Grass
                else pixelColor = Color.gray;                                    // Mountain

                pixels[y * mapWidth + x] = pixelColor;

                int mirrorX = mapWidth - 1 - x;
                int mirrorY = mapHeight - 1 - y;

                if (mirrorX >= 0 && mirrorY >= 0 && mirrorX < mapWidth && mirrorY < mapHeight)
                {
                    pixels[mirrorY * mapWidth + mirrorX] = pixelColor;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}