using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class GameManager : SingletonMonoBehaviour<GameManager>
    {
        //全TrafficLightsSystemが初期化済みか
        private bool intersectionIsInitialized = false;

        private void Update()
        {
            //TrafficLightsSystemを初期化
            //Start()に置くとRoad接続に先回りしてしまうためここに配置
            if (!intersectionIsInitialized)
            {
                InitilizeTrafficLightsSystems();
            }
        }

        /// <summary>
        /// 全道路が接続済みか確認して、接続済みなら全TrafficLightsSystemを初期化させる
        /// </summary>
        private void InitilizeTrafficLightsSystems()
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

            //全RoadJointsの時計回りソートを済ませる
            //+全TrafficLightsSystemを初期化させる
            RoadJoint[] allJoints = FindObjectsOfType<RoadJoint>();
            foreach(RoadJoint roadJoint in allJoints) 
            {
                roadJoint.ArrangeRoadsClockwise();
            }

            //初期化済みに
            intersectionIsInitialized = true;
        }
    }
}