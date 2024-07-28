using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameMode
{
    Solo,
    Multi
}

public class Board : MonoBehaviour
{
    [Header("Prefab")] 
    public GameObject chessPiecePrefab;
    public GameObject hintPrefab;
        
    [Header("Art Stuff")]
    private Camera _currentCamera;
    [SerializeField] private Vector3 centerOffset;
    [SerializeField] private AudioSource soundManager;
    [SerializeField] private AudioClip moveSound;
    
    [Header("Property")]
    public GameMode mode;
    public Team soloTeam;
    private const int Width = 8;
    private ChessPiece[,] _tiles;
    private List<GameObject> _hints;
    public Dictionary<Team, int> Points;
    
    [Header("Game State Manger")]
    public Team currentTurn;
    private GameManager _gameManager;
    
    void Start()
    {
        _currentCamera = FindObjectOfType<Camera>();
        _gameManager = FindObjectOfType<GameManager>();
        Setup();
        _gameManager.UpdateQuantityText();
    }

    public void Setup()
    {
        // delete all game object from board for game reset
        if (_tiles != null)
        {
            for (int column = 0; column < Width; column++)
            {
                for (int row = 0; row < Width; row++)
                {
                    if (_tiles[column, row] != null)
                    {
                        Destroy(_tiles[column, row].gameObject);
                        _tiles[column, row] = null;
                    }
                }
            }
        }

        if (_hints != null)
        {
            foreach (var hint in _hints)
            {
                Destroy(hint.gameObject);
            }
        }
        _tiles = new ChessPiece[Width, Width];
        _hints = new List<GameObject>();
        for (int column = Width/2-1; column <= Width/2; column++)
        {
            for (int row = Width/2-1; row <= Width/2; row++)
            {
                Vector3 newPos = new Vector3(column, row) + centerOffset;
                if (Instantiate(chessPiecePrefab, newPos, Quaternion.identity, transform)
                    .TryGetComponent(out ChessPiece piece))
                {
                    piece.team = column == row ? Team.Black : Team.White;
                    piece.name = piece.team + "(" + column + ", " + row + ")";
                    _tiles[column, row] = piece;
                }
            }
        }
        Points = new Dictionary<Team, int>()
        {
            { Team.White, 2 }, 
            { Team.Black, 2 }
        };
        _gameManager.state = GameState.Play;
        GetHints();
    }
    
    void Update()
    {
        if (_hints.Count == 0 && _gameManager.state != GameState.End)
        {
            _gameManager.state = GameState.End;
            _gameManager.EndGame();
        }
        else if (_gameManager.state == GameState.Play && Input.GetMouseButtonDown(0) && (mode == GameMode.Solo && currentTurn == soloTeam || mode == GameMode.Multi))
        {
            Vector3 mousePosition = _currentCamera.ScreenToWorldPoint(Input.mousePosition);
            int boardColumn = (int)(mousePosition.x - Math.Round(centerOffset.x));
            int boardRow = (int)(mousePosition.y - Math.Round(centerOffset.y));
            if (boardColumn is >= 0 and < Width && boardRow is >= 0 and < Width && !_tiles[boardColumn, boardRow])
            {
                List<Vector2Int> updatePiecesPosition = IsValidMove(boardColumn, boardRow);
                if (updatePiecesPosition.Count > 0)
                {
                    Vector3 newPos = new Vector3(boardColumn, boardRow) + centerOffset;
                    if (Instantiate(chessPiecePrefab, newPos, Quaternion.identity, transform)
                        .TryGetComponent(out ChessPiece piece))
                    {
                        piece.team = currentTurn;
                        piece.name = piece.team + "(" + boardColumn + ", " + boardRow + ")";
                        _tiles[boardColumn, boardRow] = piece;
                    }
                    foreach (var piecePosition in updatePiecesPosition)
                    {
                        _tiles[piecePosition.x, piecePosition.y].SwitchTeam(currentTurn);
                    }
                    // update point
                    Points[currentTurn] += 1 + updatePiecesPosition.Count;
                    Points[GetNextTurn()] += - updatePiecesPosition.Count;
                    _gameManager.UpdateQuantityText();

                    PlayMoveSound();
                    ChangeTurn();
                    GetHints();
                    StartCoroutine(GenerateBotMove());
                }
            }
        }
    }

    #region Helper
    private void ChangeTurn()
    {
        currentTurn = currentTurn == Team.White ? Team.Black : Team.White;
    }

    private Team GetNextTurn()
    {
        return currentTurn == Team.White ? Team.Black : Team.White;
    }
    #endregion

    private List<Vector2Int> IsValidMove(int column, int row)
    {
        List<Vector2Int> updatePieces = new List<Vector2Int>();
        ValidTop(ref updatePieces, column, row);
        ValidTopRight(ref updatePieces, column, row);
        ValidRight(ref updatePieces, column, row);
        ValidBottomRight(ref updatePieces, column, row);
        ValidBottom(ref updatePieces, column, row);
        ValidBottomLeft(ref updatePieces, column, row);
        ValidLeft(ref updatePieces, column, row);
        ValidTopLeft(ref updatePieces, column, row);
        return updatePieces;
    }

    private void GetHints()
    {
        foreach (var hint in _hints)
        {
            Destroy(hint.gameObject);
        }
        _hints.Clear();
        int maxIterator = 4;
        while (_hints.Count == 0 && maxIterator > 0)
        {
            // refactor :(
            for (int column = 0; column < Width; column++)
            {
                for (int row = 0; row < Width; row++)
                {
                    if (_tiles[column, row])
                    {
                        continue;
                    }
                    List<Vector2Int> simulateChecks = new List<Vector2Int>();
                    // consider check after each validation
                    ValidTop(ref simulateChecks, column, row);
                    ValidTopRight(ref simulateChecks, column, row);
                    ValidRight(ref simulateChecks, column, row);
                    ValidBottomRight(ref simulateChecks, column, row);
                    ValidBottom(ref simulateChecks, column, row);
                    ValidBottomLeft(ref simulateChecks, column, row);
                    ValidLeft(ref simulateChecks, column, row);
                    ValidTopLeft(ref simulateChecks, column, row);
                    if (simulateChecks.Count > 0)
                    {
                        // todo skip gen hint when bot turn
                        GameObject hint = Instantiate(hintPrefab, new Vector2(column + centerOffset.x, row + centerOffset.y), Quaternion.identity, transform);
                        hint.name = "Hint (" + column + ", " + row + ")";
                        if (mode == GameMode.Solo && currentTurn != soloTeam)
                        {
                            hint.SetActive(false);
                        }
                        _hints.Add(hint);
                    }
                }
            }
            maxIterator--;
            if (_hints.Count == 0)
            {
                ChangeTurn();
            }
        }
    }

    IEnumerator GenerateBotMove()
    {
        yield return new WaitForSeconds(1);
        if (mode == GameMode.Solo && currentTurn != soloTeam)
        {
            GetHints();
            if (_hints.Count != 0)
            {
                int randomMove = UnityEngine.Random.Range(0, _hints.Count);
                GameObject move = _hints[randomMove];
                Vector3 onBoardPosition = move.transform.position - centerOffset;
                int onBoardColumn = (int)onBoardPosition.x;
                int onBoardRow = (int)onBoardPosition.y;
                List<Vector2Int> updatePiecesPosition = IsValidMove(onBoardColumn, onBoardRow);
                if (updatePiecesPosition.Count > 0)
                {
                    Vector3 newPos = new Vector3(onBoardPosition.x, onBoardPosition.y) + centerOffset;
                    if (Instantiate(chessPiecePrefab, newPos, Quaternion.identity, transform)
                        .TryGetComponent(out ChessPiece piece))
                    {
                        piece.team = currentTurn;
                        piece.name = piece.team + "(" + onBoardPosition.x + ", " + onBoardPosition.y + ")";
                        _tiles[onBoardColumn, onBoardRow] = piece;
                    }
                    foreach (var piecePosition in updatePiecesPosition)
                    {
                        _tiles[piecePosition.x, piecePosition.y].SwitchTeam(currentTurn);
                    }
                    // update quantity
                    Points[currentTurn] += 1 + updatePiecesPosition.Count;
                    Points[GetNextTurn()] += - updatePiecesPosition.Count;
                    _gameManager.UpdateQuantityText();
                    PlayMoveSound();
                    ChangeTurn();
                    GetHints();
                }
            }
        }
    }
    
    private void ValidTop(ref List<Vector2Int> updatePieces, int column, int row)
    {
        bool metTeamMate = false;
        List<Vector2Int> simulateUpdatePosition = new List<Vector2Int>();
        if (row < Width - 1)
        {
            for (int i = row + 1; i < Width; i++)
            {
                if (_tiles[column, i])
                {
                    if (_tiles[column, i].team != currentTurn)
                    {
                        simulateUpdatePosition.Add(new Vector2Int(column, i));
                    }
                    else
                    {
                        metTeamMate = true;
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        if (metTeamMate)
        {
            updatePieces.AddRange(simulateUpdatePosition);
        }
    }
    
    private void ValidTopRight(ref List<Vector2Int> updatePieces, int column, int row)
    {
        bool metTeamMate = false;
        List<Vector2Int> simulateUpdatePosition = new List<Vector2Int>();
        int columnIndex = column + 1;
        int rowIndex = row + 1;
        
        while (columnIndex < Width && rowIndex < Width)
        {
            if (_tiles[columnIndex, rowIndex])
            {
                if (_tiles[columnIndex, rowIndex].team != currentTurn)
                {
                    simulateUpdatePosition.Add(new Vector2Int(columnIndex, rowIndex));
                    columnIndex++;
                    rowIndex++;
                }
                else
                {
                    metTeamMate = true;
                    break;
                }
            }
            else
            {
                break;
            }
        }
        if (metTeamMate)
        {
            updatePieces.AddRange(simulateUpdatePosition);
        }
    }
    
    private void ValidRight(ref List<Vector2Int> updatePieces, int column, int row)
    {
        bool metTeamMate = false;
        List<Vector2Int> simulateUpdatePosition = new List<Vector2Int>();
        if (column < Width - 1)
        {
            for (int i = column + 1; i < Width; i++)
            {
                if (_tiles[i, row])
                {
                    if (_tiles[i, row].team != currentTurn)
                    {
                        simulateUpdatePosition.Add(new Vector2Int(i, row));
                    }
                    else
                    {
                        metTeamMate = true;
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }
        if (metTeamMate)
        {
            updatePieces.AddRange(simulateUpdatePosition);
        }
    }
    
    private void ValidBottomRight(ref List<Vector2Int> updatePieces, int column, int row)
    {
        bool metTeamMate = false;
        List<Vector2Int> simulateUpdatePosition = new List<Vector2Int>();
        int columnIndex = column + 1;
        int rowIndex = row - 1;
        
        while (columnIndex < Width && rowIndex >= 0)
        {
            if (_tiles[columnIndex, rowIndex])
            {
                if (_tiles[columnIndex, rowIndex].team != currentTurn)
                {
                    simulateUpdatePosition.Add(new Vector2Int(columnIndex, rowIndex));
                    columnIndex++;
                    rowIndex--;
                }
                else
                {
                    metTeamMate = true;
                    break;
                }
            }
            else
            {
                break;
            }
        }
        if (metTeamMate)
        {
            updatePieces.AddRange(simulateUpdatePosition);
        }
    }
    
    private void ValidBottom(ref List<Vector2Int> updatePieces, int column, int row)
    {
        bool metTeamMate = false;
        List<Vector2Int> simulateUpdatePosition = new List<Vector2Int>();
        if (row > 0)
        {
            for (int i = row - 1; i >= 0; i--)
            {
                if (_tiles[column, i])
                {
                    if (_tiles[column, i].team != currentTurn)
                    {
                        simulateUpdatePosition.Add(new Vector2Int(column, i));
                    }
                    else
                    {
                        metTeamMate = true;
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }
        if (metTeamMate)
        {
            updatePieces.AddRange(simulateUpdatePosition);
        }
    }
    
    private void ValidBottomLeft(ref List<Vector2Int> updatePieces, int column, int row)
    {
        bool metTeamMate = false;
        List<Vector2Int> simulateUpdatePosition = new List<Vector2Int>();
        int columnIndex = column - 1;
        int rowIndex = row - 1;
        
        while (columnIndex >= 0 && rowIndex >= 0)
        {
            if (_tiles[columnIndex, rowIndex])
            {
                if (_tiles[columnIndex, rowIndex].team != currentTurn)
                {
                    simulateUpdatePosition.Add(new Vector2Int(columnIndex, rowIndex));
                    columnIndex--;
                    rowIndex--;
                }
                else
                {
                    metTeamMate = true;
                    break;
                }
            }
            else
            {
                break;
            }
        }
        if (metTeamMate)
        {
            updatePieces.AddRange(simulateUpdatePosition);
        }
    }
    
    private void ValidLeft(ref List<Vector2Int> updatePieces, int column, int row)
    {
        bool metTeamMate = false;
        List<Vector2Int> simulateUpdatePosition = new List<Vector2Int>();
        if (column > 0)
        {
            for (int i = column - 1; i >= 0; i--)
            {
                if (_tiles[i, row])
                {
                    if (_tiles[i, row].team != currentTurn)
                    {
                        simulateUpdatePosition.Add(new Vector2Int(i, row));
                    }
                    else
                    {
                        metTeamMate = true;
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }
        if (metTeamMate)
        {
            updatePieces.AddRange(simulateUpdatePosition);
        }
    }
    
    private void ValidTopLeft(ref List<Vector2Int> updatePieces, int column, int row)
    {
        bool metTeamMate = false;
        List<Vector2Int> simulateUpdatePosition = new List<Vector2Int>();
        int columnIndex = column - 1;
        int rowIndex = row + 1;
        
        while (columnIndex >= 0 && rowIndex < Width)
        {
            if (_tiles[columnIndex, rowIndex])
            {
                if (_tiles[columnIndex, rowIndex].team != currentTurn)
                {
                    simulateUpdatePosition.Add(new Vector2Int(columnIndex, rowIndex));
                    columnIndex--;
                    rowIndex++;
                }
                else
                {
                    metTeamMate = true;
                    break;
                }
            }
            else
            {
                break;
            }
        }
        if (metTeamMate)
        {
            updatePieces.AddRange(simulateUpdatePosition);
        }
    }

    private void PlayMoveSound()
    {
        soundManager.clip = moveSound;
        soundManager.Play();
    }
}
