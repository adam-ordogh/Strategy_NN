using UnityEngine;

public class VisualsManager : MonoBehaviour
{
    public static VisualsManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void StartAnimation(System.Collections.IEnumerator routine)
    {
        StartCoroutine(routine);
    }
}
