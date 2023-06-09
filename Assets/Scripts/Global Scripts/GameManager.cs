using UnityEngine;


public class GameManager : MonoBehaviour
{

    public static bool IsPause;
    public static GameManager Instance;
    public float CurrentPoints { get; private set; }
    public float pointsGainSpeed;

    public delegate void LevelDelegate();
    public static event LevelDelegate LevelChanging;
    public static void OnLevelChanging() => LevelChanging?.Invoke();

    public delegate void LevelResetDelegate();
    public static event LevelResetDelegate LevelReset;
    private static void OnLevelReset() => LevelReset?.Invoke();

    
    public delegate void GameOverDelegate();
    public static event GameOverDelegate GameOver;
    public static void OnGameOver() => GameOver?.Invoke();
    
    public delegate void TogglePauseDelegate(bool active);
    public static event TogglePauseDelegate TogglePauseEvent;
    private static void OnTogglePauseGame(bool active) => TogglePauseEvent?.Invoke(active);
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
