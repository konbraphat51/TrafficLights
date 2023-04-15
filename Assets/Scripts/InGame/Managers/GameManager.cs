using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class GameManager : SingletonMonoBehaviour<GameManager>
    {
        /// <summary>
        /// 得点
        /// </summary>
        public int score { get; private set; } = 0;

        [Header("スコア関係")]

        [Tooltip("最高速度との差に何乗するか")]
        [SerializeField] private float scoreExponent = 2f;

        //全TrafficLightsSystemが初期化済みか
        private bool afterConnectionInitialized = false;

        private void Update()
        {
            //TrafficLightsSystemを初期化
            //Start()に置くとRoad接続に先回りしてしまうためここに配置
            if (!afterConnectionInitialized)
            {
                InitilizeAfterRoadConnection();
            }
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
        /// 車到達時に加点
        /// </summary>
        public void OnCarArrived(float speedAverage, float speedMax)
        {
            //いくら加点するか計算
            int scoreAddition = (int)Mathf.Pow(speedMax - speedAverage, scoreExponent);

            //加点
            AddPoint(scoreAddition);
        }

        /// <summary>
        /// 加点
        /// </summary>
        private void AddPoint(int addition)
        {
            score += addition;
        }
    }
}