using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Play,
    Pause,
    End
}

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    private Board _board;
    public GameState state;
    
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TextMeshProUGUI blackQuantityText;
    [SerializeField] private TextMeshProUGUI whiteQuantityText;
    [SerializeField] private TextMeshProUGUI winText;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    void Start()
    {
        state = GameState.Pause;
        _board = FindObjectOfType<Board>();
    }
    

    public void GameStart()
    {
        state = GameState.Play;
    }
    
    public void GamePause()
    {
        state = GameState.Pause;
    }
    
    public void GameReset()
    {
        state = GameState.Pause;
        endGamePanel.SetActive(false);
        _board.Setup();
        UpdateQuantityText();
    }

    public void ToMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void PlayerSingleMode()
    {
        _board.mode = GameMode.Solo;
    }
    
    public void PlayerMultiMode()
    {
        _board.mode = GameMode.Multi;
    }

    public void EndGame()
    {
        if (_board.Points[Team.Black] > _board.Points[Team.White])
        {
            winText.text = "Black win!";
        }
        else if (_board.Points[Team.Black] < _board.Points[Team.White])
        {
            winText.text = "White win!";
        }
        else
        {
            winText.text = "Draw!";
        }
        endGamePanel.SetActive(true);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
    
    public void UpdateQuantityText()
    {
        blackQuantityText.text = _board.Points[Team.Black].ToString();
        whiteQuantityText.text = _board.Points[Team.White].ToString();
    }
}
