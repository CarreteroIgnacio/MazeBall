using UnityEngine;


public class GameManager : MonoBehaviour
{

    public static bool IsPause;
    public static GameManager Instance;
    public float CurrentPoints { get; private set; }
    public float pointsGainSpeed;

    public delegate void LevelDelegate();
    public event LevelDelegate LevelChanging;
    public virtual void OnLevelChanging() => LevelChanging?.Invoke();

    public delegate void LevelResetDelegate();
    public event LevelResetDelegate LevelReset;
    public virtual void OnLevelReset() => LevelReset?.Invoke();

    
    public delegate void GameOverDelegate();
    public event GameOverDelegate GameOver;
    public virtual void OnGameOver() => GameOver?.Invoke();
    
    public delegate void TogglePauseDelegate(bool active);
    public event TogglePauseDelegate TogglePauseEvent;
    public virtual void OnTogglePauseGame(bool active) => TogglePauseEvent?.Invoke(active);
    private void Awake() => Instance = this;
  
    
    private void Start()
    {
        IsPause = true;
        TogglePauseGame();

        GameOver += TogglePauseGame;
        //StartCoroutine(LevelChanger());
    }

    

    public void TogglePauseGame()
    {
        IsPause = !IsPause;
        //Debug.Log(IsPause);
        OnTogglePauseGame(IsPause);

        CanvasManager.Instance.SetActiveMenu(IsPause);
        SetCursorState(IsPause);
        
        Time.timeScale = IsPause ? 0 : 1;
        FrequencyBandAnalyser.Instance.PauseMusic(IsPause);
    }

    private void SetCursorState(bool nya)
    {
        Cursor.visible = nya;
        Cursor.lockState = nya?CursorLockMode.None: CursorLockMode.Locked;
    }
 
    public void AddPoints(float points) => Instance.CurrentPoints += points * pointsGainSpeed;


    

    public void Reload()
    {
        OnLevelReset();
        TogglePauseGame();
        Instance.CurrentPoints = 0;
    }

    public void Exit() => Application.Quit();
    public void OpenURL(string link) => Application.OpenURL(link);




}
