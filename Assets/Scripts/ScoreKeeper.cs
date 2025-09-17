using UnityEngine;
using UnityEngine.UI;

public class ScoreKeeper : MonoBehaviour
{
    private GameBoardManager gameBoardManager;
    private int score;
    private PlayerMovement player;
    [SerializeField] private Text scoreText; // Optional text display

    private Vector3 startingPos;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = FindAnyObjectByType<PlayerMovement>();
        gameBoardManager = FindAnyObjectByType<GameBoardManager>();
        score = 0;
        startingPos = player.transform.position;
        scoreText.text = "Score: 0";
    }

    // Update is called once per frame
    void Update()
    {
        updateScore();
    }

    void updateScore()
    {
        Vector3Int start = gameBoardManager.curBoard.WorldToCell(startingPos);
        Vector3Int cur = gameBoardManager.curBoard.WorldToCell(player.transform.position);
        score = (int)Mathf.Max(score, start.y - cur.y);
        scoreText.text = $"Score: {score}";
    }
}
