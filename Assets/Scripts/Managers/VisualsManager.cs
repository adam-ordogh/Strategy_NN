using UnityEngine;

public class VisualsManager : MonoBehaviour
{
    // This is a "Singleton-ish" access point, or you can pass it via injection
    public static VisualsManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    // This is the only function UnitVisualizer needs to know about
    public void StartAnimation(System.Collections.IEnumerator routine)
    {
        StartCoroutine(routine);
    }
}
