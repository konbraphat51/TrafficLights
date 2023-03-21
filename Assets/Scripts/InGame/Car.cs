using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class Car : MonoBehaviour
    {
        [Tooltip("スピード")]
        [SerializeField] private float speed = 5f;

        [SerializeField] private float zPos = -2f;

        private enum State
        {
            runningRoad,
            runningJoint,
            changingLane
        }

        private State state;

        /// <summary>
        /// 生成されたRoadJoint
        /// </summary>
        public RoadJoint spawnPoint { get; private set; }

        /// <summary>
        /// 目的地RoadJoint
        /// </summary>
        public RoadJoint destination { get; private set; }

        /// <summary>
        /// この配列の順に沿って走行
        /// </summary>
        public Queue<Road> routes { get; private set; }

        ///<summary>
        ///現在走っている道路
        ///</summary>
        private Road currentRoad;

        /// <summary>
        /// 現在走っている道路の車線番号
        /// </summary>
        private uint currentLane = 0;

        /// <summary> 
        /// 現在使ってる道沿いベクトル
        /// </summary>
        private Vector2 currentAlongRoad;

        /// <summary>
        /// 現在走ってる道で走行した距離
        /// </summary>
        private float currentDistanceInRoad = 0f;

        private void Update()
        {
            Run();
        }

        /// <summary>
        /// 走行。前進も曲がりも兼ねる
        /// </summary>
        private void Run()
        {
            switch (state)
            {
                case State.runningRoad:
                    RunRoad();
                    break;
            }
        }

        /// <summary>
        /// 生成時の初期化処理
        /// </summary>
        public void Initialize(RoadJoint spawnPoint, Road spawnRoad, uint spawnLane, RoadJoint destination = null)
        {
            //生成ポイント
            this.spawnPoint = spawnPoint;

            //目的地
            if (destination == null)
            {
                //こちらで目的地を設定
                this.destination = ChooseDestinationRadomly(this.spawnPoint);
            }
            else
            {
                //目的地が指定されている
                this.destination = destination;
            }

            //初期道路は確定しているので、その次のjointからのルートを得る
            this.routes = GetRoute(spawnRoad.GetDiffrentEdge(spawnPoint), destination);

            //走り始める
            StartRunningRoad(spawnRoad, spawnLane, spawnPoint);
        }

        /// <summary>
        /// ランダムに目的地を決める
        /// </summary>
        private RoadJoint ChooseDestinationRadomly(RoadJoint spawnPoint)
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

        /// <summary>
        /// ルートを取得
        /// </summary>
        /// <param name="startingPoint">開始位置</param>
        /// <param name="target">目的地</param>
        /// <returns></returns>
        private Queue<Road> GetRoute(RoadJoint startingPoint, RoadJoint target)
        {
            //TODO
            Queue<Road> output = new Queue<Road>();

            Road nextRoad = startingPoint.connectedRoads[Random.Range(0, startingPoint.connectedRoads.Count)];

            output.Enqueue(nextRoad);

            return output;
        }

        /// <summary>
        /// 道路を走り始める
        /// </summary>
        private void StartRunningRoad(Road road, uint laneID, RoadJoint startingJoint)
        {
            uint edgeID = road.GetEdgeNumber(startingJoint);

            //記憶
            currentRoad = road;
            currentLane = laneID;
            currentDistanceInRoad = 0f;
            currentAlongRoad = road.alongVectors[edgeID];

            //>>現在位置を道路に開始位置に調整
            //位置
            Vector2 startingPointPosition = road.GetStartingPoint(edgeID, laneID);
            transform.position = (Vector3)startingPointPosition + new Vector3(0, 0, zPos);
            //角度
            transform.rotation = GetRotatoinInRoad(currentAlongRoad);

            //ステートを変更
            state = State.runningRoad;
        }

        /// <summary>
        /// runningJointステートに入る
        /// </summary>
        private void StartRunningJoint()
        {
            //ステートを変更
            state = State.runningJoint;
        }

        /// <summary>
        /// runningRoad時の走行処理
        /// </summary>
        private void RunRoad()
        {
            //前進
            AdvanceRoad();
            
            //終端を通り過ぎたか確認
            if (currentDistanceInRoad >= currentAlongRoad.magnitude)
            {
                //Joint回転モードに入る
                StartRunningJoint();
            }
        }

        /// <summary>
        /// runningRoad時の走行処理
        /// </summary>
        private void AdvanceRoad()
        {
            float advancedDistance = GetSpeedInRoad() * Time.deltaTime;
            //外部（ワールド）的
            transform.position += (Vector3)(currentAlongRoad.normalized * advancedDistance);
            //内部的
            currentDistanceInRoad += advancedDistance;
        }

        /// <summary>
        /// runningRoad時の状況に対応する速度を求める
        /// </summary>
        private float GetSpeedInRoad()
        {
            return speed;
        }

        /// <summary>
        /// RunRoad時の回転を返す
        /// </summary>
        private Quaternion GetRotatoinInRoad(Vector2 alongVector)
        {
            return Quaternion.FromToRotation(Vector3.right, alongVector);
        }
    }
}