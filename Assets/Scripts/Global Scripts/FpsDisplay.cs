using TMPro;
using UnityEngine;

public class FpsDisplay : MonoBehaviour
{
    public TextMeshProUGUI displayText;
 
    [SerializeField] private float hudRefreshRate = .25f;
 
    private float _timer;
    public void Update ()
    {
        if (!(Time.unscaledTime > _timer)) return;
        
        _timer = Time.unscaledTime + hudRefreshRate;

        displayText.text = (1f / Time.unscaledDeltaTime).ToString("F0");
    }
}
