using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class Car : MonoBehaviour
    {
        /// <summary>
        /// 生成されたRoadJoint
        /// </summary>
        public RoadJoint spawnPoint { get; private set; }

        /// <summary>
        /// 目的地RoadJoint
        /// </summary>
        public RoadJoint destination { get; private set; }

        /// <summary>
        /// 生成時の初期化処理
        /// </summary>
        public void Initialize(RoadJoint spawnPoint, RoadJoint destination = null)
        {
            //生成ポイント
            this.spawnPoint = spawnPoint;

            //目的地
            if (destination == null)
            {
                //こちらで目的地を設定
                this.destination = GetDestination(this.spawnPoint);
            }
            else
            {
                //目的地が指定されている
                this.destination = destination;
            }
            
        }

        /// <summary>
        /// ランダムに目的地を決める
        /// </summary>
        private RoadJoint GetDestination(RoadJoint spawnPoint)
        {
            //spawnPointとの重複を避けるループ
            RoadJoint destination;
            OutsideConnection[] outsideConnectionsAll = FindObjectsOfType<OutsideConnection>();
            while (true)
            {
                //ランダムに目的地を決める
                destination = outsideConnectionsAll[Random.Range(0, outsideConnectionsAll.Length - 1)];

                //重複チェック
                if (destination != spawnPoint)
                {
                    //>>生成ポイントと目的地が重複していない
                    break;
                }
            }

            return destination;
        }


    }
}