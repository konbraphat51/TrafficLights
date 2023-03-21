using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class GameManager : SingletonMonoBehaviour<GameManager>
    {
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
                roadJoint.ArrangeRoadsClockwise();
            }

            //初期化済みに
            afterConnectionInitialized = true;
        }
    }
}