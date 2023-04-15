using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InGame
{
    public class GameManager : SingletonMonoBehaviour<GameManager>
    {
        public enum Sequence
        {
            countDown,
            playing,
            gameFinished
        }

        public Sequence sequence { get; private set; } = Sequence.countDown;

        /// <summary>
        /// 得点
        /// </summary>
        public static int score { get; private set; } = 0;

        [Header("スコア関係")]

        [Tooltip("最高速度との差に何乗するか")]
        [SerializeField] private float scoreExponent = 2f;

        [Tooltip("得点を単純拡大する")]
        [SerializeField] private float scoreCoef = 100f;

        [Header("タイム")]

        [Tooltip("ゲーム時間（秒）")]
        [SerializeField] private float gameTime = 61f;

        [Header("ゲーム終了")]
        
        [Tooltip("ゲーム終了してからリザルト画面に移るまでの時間")]
        [SerializeField] private float gameFinishedWait = 3f;

        [Tooltip("リザルト画面のシーン名")]
        [SerializeField] private string resultSceneName = "Result";

        public float countDownTimeLeft { get; private set; } = 3f;

        public float gameTimeLeft { get; private set; } = 61f;

        //全TrafficLightsSystemが初期化済みか
        private bool afterConnectionInitialized = false;

        private void Start()
        {
            //変数初期化
            gameTimeLeft = gameTime;
            score = 0;
        }

        private void Update()
        {
            //TrafficLightsSystemを初期化
            //Start()に置くとRoad接続に先回りしてしまうためここに配置
            if (!afterConnectionInitialized)
            {
                InitilizeAfterRoadConnection();
            }

            ManageTime();
        }

        /// <summary>
        /// 全道路が接続済みか確認して、その後に初期化が必要なオブジェクトを初期化させる
        /// </summary>
        private void InitilizeAfterRoadConnection()
        {
            //未接続の道路が存在していればキャンセル
            Road[] allRoads = FindObjectsOfType<Road>();
            foreach(Road road in allRoads)
            {
                if (!road.isInitialized)
                {
                    //>>未接続
                    return;
                }
            }

            //>>全道路が接続済み
            
            //交差点・信号機を初期化
            InitializeIntersections();

            //CarGeneratorを初期化
            CarGenerator.Initialize();

            //Navigatorを初期化
            Navigator.Instance.SetUp();
        }

        /// <summary>
        /// 全TrafficLightsSystemを初期化させる
        /// </summary>
        private void InitializeIntersections()
        {
            //全RoadJointsの時計回りソートを済ませる
            //+全TrafficLightsSystemを初期化させる
            RoadJoint[] allJoints = FindObjectsOfType<RoadJoint>();
            foreach (RoadJoint roadJoint in allJoints)
            {
                roadJoint.ArrangeRoadsAnticlockwise();
            }

            //初期化済みに
            afterConnectionInitialized = true;
        }

        /// <summary>
        /// 残り時間の管理
        /// </summary>
        private void ManageTime()
        {
            switch (sequence)
            {
                case Sequence.countDown:
                    countDownTimeLeft -= Time.deltaTime;

                    //時間が来たらplayingシーケンスに切り替え
                    if(countDownTimeLeft <= 0f)
                    {
                        TransferToPlaying();
                    }
                    break;

                case Sequence.playing:
                    gameTimeLeft -= Time.deltaTime;

                    if(gameTimeLeft <= 0f)
                    {
                        TransferToFinished();
                    }
                    break;
            }
        }

        /// <summary>
        /// Playingシーケンスに移行
        /// </summary>
        private void TransferToPlaying()
        {
            sequence = Sequence.playing;

            UIManager.Instance.OnCountDownFinished();
        }

        /// <summary>
        /// Finishedシーケンスに移行
        /// </summary>
        private void TransferToFinished()
        {
            sequence = Sequence.gameFinished;

            Invoke(nameof(GotoResult), gameFinishedWait);

            UIManager.Instance.OnGameFinished();
        }

        /// <summary>
        /// 車到達時に加点
        /// </summary>
        public void OnCarArrived(float speedAverage, float speedMax)
        {
            //Playingシーケンスのみ
            if(sequence != Sequence.playing)
            {
                return;
            }

            //いくら加点するか計算
            int scoreAddition = CalculatePoint(speedAverage, speedMax);

            //加点
            AddPoint(scoreAddition);
        }

        /// <summary>
        /// 平均スピードを得点に変換する
        /// </summary>
        private int CalculatePoint(float speedAverage, float speedMax)
        {
            float difference = speedAverage　/ speedMax;
            float powered = Mathf.Pow(difference, scoreExponent);

            return (int)(powered * scoreCoef);
        }

        /// <summary>
        /// 加点
        /// </summary>
        private void AddPoint(int addition)
        {
            //加点
            score += addition;

            //UI更新
            UIManager.Instance.OnPointsChanged(addition);
        }

        /// <summary>
        /// Result画面へ遷移
        /// </summary>
        private void GotoResult()
        {
            SceneManager.LoadScene(resultSceneName);
        }
    }
}