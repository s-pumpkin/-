using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManger : MonoBehaviour
{
    public static GameManger Instance;
    public GameObject StartPos;

    public ResourceLoad<Sprite> SpritePool = new ResourceLoad<Sprite>();
    public ResourceLoad<AudioClip> AudioClipPool = new ResourceLoad<AudioClip>();


    public GameObject EndUI;

    [SerializeField]
    private Text m_visitorCountText;
    [SerializeField]
    private Text m_finisherCountText;

    [SerializeField]
    private Text m_rankOfScoreText;



    [SerializeField]
    private InputField m_rankInputField;

    [SerializeField]
    private Text m_scoreOfRankText;



    [SerializeField]
    private Text m_leaderboardText;

    public Text m_timerText;
    public float timer_f;
    public bool EndGame = false;

    public Text testText;
    private void Awake()
    {
        Instance = this;

        SpritePool.Info("Sprite");
        AudioClipPool.Info("Audio");

        GainNewVisitor();
    }

    public void Start()
    {
        PlayerController.Instance.transform.position = StartPos.transform.position;
    }

    void Update()
    {
        if (!EndGame)
            timer_f += Time.deltaTime;
    }

    public string SetTimerText(float time)
    {
        int s = (int)(time % 60);
        int m = (int)(time / 60 % 60);
        int h = (int)(time / 3600);
        return h + "時 " + m + "分 " + s + "秒";
    }

    public void OpenEndUI()
    {
        EndGame = true;

        m_timerText.text = SetTimerText(timer_f);

        SendScoreToLeaderboard();
    }

    #region 第幾位遊玩者
    private void GainNewVisitor()
    {
        PlayerStatsManager.Instance.GainNewVisitor((x) =>
        {
            ShowVisitorCount(x);
        });
    }

    private void ShowVisitorCount(string visitorNum)
    {
        m_visitorCountText.text = string.Format("第 {0} 位遊玩者！", visitorNum);
    }
    #endregion

    #region 第幾位通關者
    public void GainNewFinisher()
    {
        PlayerStatsManager.Instance.GainNewFinisher((x) =>
        {
            ShowFinisherCount(x);
        });
    }

    private void ShowFinisherCount(string finisherNum)
    {
        m_finisherCountText.text = string.Format("你是第 {0} 位通關者！", finisherNum);
    }
    #endregion

    #region 將成績登錄到排行榜，取得名次
    public void SendScoreToLeaderboard()
    {
        var score = ScoreCompute((int)timer_f);

        PlayerStatsManager.Instance.AddNewScore(score, (x) =>
     {
         ShowTheRankOfScore(score, x);
     });
    }

    private void ShowTheRankOfScore(int score, string rank)
    {
        m_rankOfScoreText.text = string.Format("排行第 {0} 名", rank);

        EndUI.SetActive(true);
        GetScoreOfTop10();
    }
    #endregion

    #region 查詢排行榜上第 n 名的成績 
    public void SearchScoreByRank()
    {
        var rank = 1;
        var isNumber = int.TryParse(m_rankInputField.text, out rank);

        if (!isNumber || rank <= 0)
            return;

        PlayerStatsManager.Instance.GetScoreByRank(rank, (x) =>
        {
            ShowTheScoreOfTheRank(rank, x);
        });
    }

    private void ShowTheScoreOfTheRank(int rank, string score)
    {
        m_scoreOfRankText.text = string.Format("Rank   : No.{0}\nScore  : {1}", rank, score);
    }
    #endregion

    #region 列出排行榜上的前 n 筆成績
    public void GetScoreOfTop10()
    {
        PlayerStatsManager.Instance.GetScoreOfTopN(10, (x) =>
        {
            RefreshLeaderboard(x);
        });
    }

    private void RefreshLeaderboard(string str)
    {
        var data = str.Split(',');

        m_leaderboardText.text = "";

        for (int i = 0; i < 10; i++)
        {
            if (data[i].Contains("?"))
                continue;
            int score = ScoreCompute(int.Parse(data[i]));
            string time = SetTimerText(score);
            m_leaderboardText.text += string.Format("第 {0} 名時間: {1}\n", i + 1, time);
        }

    }
    #endregion

    public int ScoreCompute(int score)
    {
        score = Mathf.Abs(score - 10000000);
        return score;
    }

}
