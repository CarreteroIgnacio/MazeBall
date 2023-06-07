using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public partial class CanvasManager : MonoBehaviour
{

    public static CanvasManager Instance;
    public Material playerMat;
      
    public TextMeshProUGUI pointText;
    
    public Image[] healthBarSlider;
    public Image[] integrityBarSlider;
    public Image[] energyBarSlider;

    public Image[] dashTimer;
    public Image[] jumpTimer;
    
    public Image wasdKeyHelp;
    public TextMeshProUGUI[] textKeyHelp;

    public TextMeshProUGUI TotalPointsText;

    public float speedLerp = 1;
    private static readonly int OverallInflate = Shader.PropertyToID("_Overall_Inflate");

    public GameObject menuGameObject;
    public GameObject playerUIGameObject;

    private void Awake()
    {
        Instance = this;
        SetActiveVolume(.4f);

        StartCoroutine(VanishHelp());
        
    }

    private void Start()
    {
        GameManager.GameOver += ShowTotalPoints;
        GameManager.LevelReset += HideotalPoints;
    }

    private void Update() => pointText.text = GameManager.Instance.CurrentPoints.ToString("F0");

    [Header("Volume")] public Image RectVolumen;
    public Sprite[] IconsVolumen;
    
    //private void LateUpdate() => SetGUI(SysPlayer.playerComponentInstance);

    void ShowTotalPoints()
    {
        TotalPointsText.text = "You get " + GameManager.Instance.CurrentPoints.ToString("F0") + " Pts" +
            "\n Try Again! :)";
        TotalPointsText.gameObject.SetActive(true);
    }

    void HideotalPoints() => TotalPointsText.gameObject.SetActive(false);

    private IEnumerator VanishHelp()
    {
        var timer = 0f;
        while (true)
        {
            timer += Time.deltaTime;

            var lerp = Mathf.InverseLerp(10,0, timer);
            var imageColor = new Color(1,1,1,lerp);

            wasdKeyHelp.color = imageColor;
            foreach( var textMesh in textKeyHelp) 
                textMesh.color = imageColor;



            if(timer > 10f) break;
            yield return 0;
        }
        wasdKeyHelp.transform.parent.gameObject.SetActive(false);

    }
    
    
    public void SetActiveMenu(bool nya)
    {
        menuGameObject.SetActive(nya);
        playerUIGameObject.SetActive(!nya);
    }

    public void SetActiveVolume(float vol)
    {
        RectVolumen.sprite = vol switch
        {
            > .66f => IconsVolumen[3],
            > .33f => IconsVolumen[2],
            > 0f => IconsVolumen[1],
            _ => IconsVolumen[0]
        };

        FrequencyBandAnalyser.Instance.SetCurrentVolume(vol);
    }

    public void SetGUI(PlayerComponent playerComponent)
    {
        var currentHealth = playerComponent.Health / playerComponent.MaxHealth;
        var hue = Mathf.Lerp(0, 140, currentHealth) / 360;
        foreach (var slider in healthBarSlider)
        {
            slider.fillAmount = Mathf.Lerp(slider.fillAmount, currentHealth, Time.deltaTime * speedLerp);;
            slider.color = Color.HSVToRGB(hue, 1, 1);
        }
        
        playerMat.SetFloat(OverallInflate, currentHealth);
        
        

        var currentEnergy = playerComponent.Energy / playerComponent.MaxEnergy;
        var vibrance = Mathf.Lerp(.25f, .6f, currentEnergy);

        if (currentEnergy >= 1) vibrance += .4f;
        else if (currentEnergy > .5) vibrance += .2f;
        
        foreach (var slider in energyBarSlider)
        {
            slider.fillAmount = Mathf.Lerp(slider.fillAmount, currentEnergy, Time.deltaTime * speedLerp);
            slider.color = Color.HSVToRGB(.5f , 1, vibrance);
        }


        var cofIntegrity = playerComponent.Integrity / playerComponent.MaxIntegrity;
        foreach (var slider in integrityBarSlider)
            slider.fillAmount = Mathf.Lerp(slider.fillAmount, cofIntegrity, Time.deltaTime * speedLerp);



        UpdateButtonsPowers(dashTimer,
            playerComponent.DashCurrentCd / playerComponent.DashCooldown,
            playerComponent.Energy,
            playerComponent.DashCost
        );

        UpdateButtonsPowers(jumpTimer,
            playerComponent.JumpCurrentCd / playerComponent.JumpCooldown,
            playerComponent.Energy,
            playerComponent.JumpCost
        );
    

        jumpTimer[3].fillAmount =  Mathf.Clamp01( 
            playerComponent.JumpCurrentCd / playerComponent.JumpCooldown );
            
            
        
    }


    public void UpdateButtonsPowers(Image[] images, float cdCof, float energy, float cost)
    {
        images[3].fillAmount = cdCof;

        if (cdCof <= 0)
        {
            if (energy > cost)
            {
                images[0].color = new Color(1, 1, 1, .8f);
                images[1].color = new Color(0, 0, 0, .5f);
                images[2].color = new Color(0, 0, 0, 1f);
            }
            else
            {
                images[0].color = new Color(0, 0, 0, .5f);
                images[1].color = new Color(1, 1, 1, .8f);
                images[2].color = new Color(1, 1, 1, .8f);
            }
        }
        else
        {
            images[0].color = new Color(0, 0, 0, .5f);
            images[1].color = new Color(1, 1, 1, .8f);
            images[2].color = new Color(1, 1, 1, .8f);
        }
    }
    
    public void Pause() => GameManager.Instance.TogglePauseGame();
    
    public void Exit() => GameManager.Instance.Exit();
    public void Reload() => GameManager.Instance.Reload();
    public void OpenURL(string link) => GameManager.Instance.OpenURL(link);

}
