using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class Car : MonoBehaviour
    {
        [Tooltip("スピード")]
        [SerializeField] private float speed = 5f;

        [Tooltip("RoadJointを回る回転速度")]
        [SerializeField] private float angularSpeed = 30f;

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
        /// この配列の順に沿って走行。Joint曲がり終えた時点で消費
        /// </summary>
        public Queue<Road> routes { get; private set; }

        ///<summary>
        ///現在走っている道路
        ///</summary>
        public Road currentRoad { get; private set; }

        /// <summary>
        /// 現在走っている道路の車線番号
        /// </summary>
        public uint currentLane { get; private set; } = 0;

        /// <summary> 
        /// 現在使ってる道沿いベクトル
        /// </summary>
        private Vector2 currentAlongRoad;

        /// <summary>
        /// 現在走ってる道で走行した距離
        /// </summary>
        private float currentDistanceInRoad = 0f;

        /// <summary>
        /// この距離まで到達すればrunningRoad終了
        /// </summary>
        private float targetDistanceInRoad = 0f;

        /// <summary>
        /// 現在のJoint移動軌道。Jointを移動し終えた時点で更新し、次のJointの軌道を代入
        /// </summary>
        private CurveRoute currentCurveRoute;

        /// <summary>
        /// RunningJoint中の回転角
        /// </summary>
        private float currentAngle;

        /// <summary>
        /// 次のRunningRoadのlaneID
        /// </summary>
        private uint nextLaneID;

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

                case State.runningJoint:
                    RunJoint();
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
            this.routes = GetRoute(spawnRoad.GetDiffrentEdge(spawnPoint), this.destination);

            //走り始める
            StartRunningRoad(spawnRoad, spawnLane, spawnPoint, true);
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
            //Navigatorよりルートを取得
            Road[] routeArray = Navigator.Instance.GetRoute(startingPoint, target);

            //キューに直す
            Queue<Road> routeQueue = new Queue<Road>();
            foreach(Road road in routeArray)
            {
                routeQueue.Enqueue(road);
            }

            return routeQueue;
        }

        /// <summary>
        /// 道路を走り始める
        /// </summary>
        private void StartRunningRoad(Road road, uint laneID, RoadJoint startingJoint, bool first = false)
        {
            uint edgeID = road.GetEdgeID(startingJoint);

            //記憶
            currentRoad = road;
            currentLane = laneID;
            currentDistanceInRoad = 0f;
            currentAlongRoad = road.alongVectors[edgeID];

            //現在位置を道路に開始位置に調整
            AdjustStartingPositionInRoad(road, laneID, edgeID, first);

            //現在の行き先の座標
            Vector2 destinationPoint;

            //次のJoint回転を計算
            if (TryGetNextCurveRoute(road.GetDiffrentEdge(startingJoint))){
                //次のJoint移動がある場合、回転開始位置までrunningRoad
                destinationPoint = currentCurveRoute.startingPoint;
            }
            else
            {
                //次のJoint移動がない場合（終点の場合）、Jointまで走る
                destinationPoint = road.GetDiffrentEdge(startingJoint).transform.position;
            }

            //目標走行距離
            targetDistanceInRoad = Vector2.Distance(road.GetStartingPoint(edgeID, laneID), destinationPoint);

            //開始位置時点での走行距離
            currentDistanceInRoad = targetDistanceInRoad - Vector2.Distance(transform.position, destinationPoint);

            //ステートを変更
            state = State.runningRoad;
        }

        /// <summary>
        /// RunningRoad開始時の位置調整
        /// </summary>
        private void AdjustStartingPositionInRoad(Road road, uint laneID, uint edgeID, bool first)
        {
            //座標
            if (!CheckInLine(transform.position, road.GetStartingPoint(edgeID, laneID), road.alongVectors[edgeID]))
            {
                //直線状無い場合は垂線の足へ調整
                transform.position = GetFootOfPerpendicular(transform.position, road.GetStartingPoint(edgeID, laneID), road.alongVectors[edgeID]);
            }

            //初回の場合、座標を合わせる
            if (first)
            {
                transform.position = road.GetStartingPoint(edgeID, laneID);
            }

            //回転
            transform.rotation = GetRotatoinInRoad(road.alongVectors[edgeID]);
        }

        /// <summary>
        /// 次のcurveRouteを取得
        /// </summary>
        /// <returns>次が終点などでJoint移動が無い場合はfalse</returns>
        private bool TryGetNextCurveRoute(RoadJoint curvingJoint)
        {
            //次が終点
            if (routes.Count == 0)
            {
                return false;
            }

            //次のRoad
            Road nextRoad = routes.Peek();

            //取得
            currentCurveRoute = GetCurveRoute(
                curvingJoint,
                currentRoad,
                currentLane,
                nextRoad,
                GetNextLane()
                );

            return true;
        }

        /// <summary>
        /// 次のRoadで走る車線を選ぶ
        /// </summary>
        private uint GetNextLane()
        {
            //TODO
            nextLaneID = 0;
            return 0;
        }

        /// <summary>
        /// runningJointステートに入る
        /// </summary>
        private void StartRunningJoint()
        {
            //位置を調整
            AdjustStartingPositionInJoint();

            //角度情報の初期化
            currentAngle = currentCurveRoute.startingAngle;

            //ステートを変更
            state = State.runningJoint;
        }

        /// <summary>
        /// RunningJoint開始時の位置調整
        /// </summary>
        private void AdjustStartingPositionInJoint()
        {
            //座標
            transform.position = currentCurveRoute.startingPoint;

            //回転
            transform.rotation = GetRotationInJoint(currentCurveRoute.startingAngle, currentCurveRoute.clockwise);
        }

        /// <summary>
        /// runningRoad時の走行処理
        /// </summary>
        private void RunRoad()
        {
            //前進
            AdvanceRoad();
            
            //終端を通り過ぎたか確認
            if (currentDistanceInRoad >= targetDistanceInRoad)
            {
                if(routes.Count > 0)
                {
                    //>>まだ終点まで来ていない
                    //Joint回転モードに入る
                    StartRunningJoint();
                }
                else
                {
                    //>>終点まで来た
                    //到着時処理
                    OnArrivedDestination();
                }
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
        /// Jointを回る
        /// </summary>
        private void RunJoint()
        {
            //移動
            TurnJoint();

            //終端を通り過ぎたか確認
            if (CheckPassedJoint())
            {
                //通り過ぎた
                StartRunningRoad(routes.Dequeue(), nextLaneID, currentCurveRoute.curvingJoint);
            }
        }

        /// <summary>
        /// RoadJointを回る
        /// </summary>
        private void TurnJoint()
        {
            //時計・反時計回りで正負反転する
            int coef;
            if (currentCurveRoute.clockwise)
            {
                coef = -1;
            }
            else
            {
                coef = 1;
            }

            //回転
            currentAngle += GetSpeedInJoint() * coef * Time.deltaTime;

            //座標
            transform.position = GetPositionFromPolar(currentCurveRoute.center, currentCurveRoute.radius, currentAngle);

            //回転
            transform.rotation = GetRotationInJoint(currentAngle, currentCurveRoute.clockwise);
        }

        /// <summary>
        /// RunningJoint中に終端を通り過ぎたか確認
        /// </summary>
        private bool CheckPassedJoint()
        {
            if (currentCurveRoute.clockwise)
            {
                //時計回り
                if (currentCurveRoute.startingAngle > currentCurveRoute.endingAngle)
                {
                    //x軸正を経過しない
                    return (currentAngle <= currentCurveRoute.endingAngle);
                }
                else
                {
                    //x軸正を経過
                    return (currentAngle <= currentCurveRoute.endingAngle - 360);
                }
            }
            else
            {
                //反時計回り
                if (currentCurveRoute.startingAngle < currentCurveRoute.endingAngle)
                {
                    //x軸正を経過しない
                    return (currentAngle >= currentCurveRoute.endingAngle);
                }
                else
                {
                    //x軸正を経過
                    return (currentAngle >= currentCurveRoute.endingAngle + 360);
                }
            }
        }

        /// <summary>
        /// 目的地に到着して、消えてGameManagerに報告
        /// </summary>
        private void OnArrivedDestination()
        {
            //消える
            Destroy(this.gameObject);
        }

        private float GetSpeedInJoint()
        {
            return angularSpeed;
        }

        /// <summary>
        /// RunRoad時の回転を返す
        /// </summary>
        private static Quaternion GetRotatoinInRoad(Vector2 alongVector)
        {
            return Quaternion.FromToRotation(Vector3.right, alongVector);
        }

        /// <summary>
        /// 二つの道からカーブ軌道を取得
        /// </summary>
        /// <param name="startingEdgeID">交差点側のedgeID</param>
        /// <param name="endingRoad">交差点側のedgeID</param>
        private CurveRoute GetCurveRoute(
            RoadJoint curvingJoint,
            Road startingRoad, 
            uint startingLaneID,
            Road endingRoad, 
            uint endingLaneID
            )
        {
            CurveRoute output = new CurveRoute();

            output.curvingJoint = curvingJoint;

            //EdgeIDを取得
            uint startingEdgeID = startingRoad.GetEdgeID(curvingJoint);
            uint endingEdgeID = endingRoad.GetEdgeID(curvingJoint);

            //>>時計回りかを取得
            Vector2 startingAlongVector = startingRoad.alongVectors[Road.GetDifferentEdgeID(startingEdgeID)];
            Vector2 endingAlongVector = endingRoad.alongVectors[endingEdgeID];
            float angularDiference = Quaternion.FromToRotation(startingAlongVector, endingAlongVector).eulerAngles.z;
            if (angularDiference < 180f)
            {
                //反時計回り
                output.clockwise = false;
            }
            else
            {
                //時計回り
                output.clockwise = true;
            }

            //>>中心を取得
            //車線の側面の交点が中心になる

            //考慮する線分
            Vector2 startingRoadSideLinePoint;
            Vector2 startingRoadSideLineVector;
            Vector2 endingRoadSideLinePoint;
            Vector2 endingRoadSideLineVector;

            if (output.clockwise)
            {
                //>>時計回り

                //starting: 車線の右側
                startingRoadSideLinePoint = startingRoad.GetRightPoint(Road.GetDifferentEdgeID(startingEdgeID), startingLaneID);
                startingRoadSideLineVector = startingRoad.alongVectors[Road.GetDifferentEdgeID(startingEdgeID)];

                //ending: 車線の右側
                endingRoadSideLinePoint = endingRoad.GetRightPoint(endingEdgeID, endingLaneID);
                endingRoadSideLineVector = endingRoad.alongVectors[endingEdgeID];
            }
            else
            {
                //>>反時計回り
                
                //starting: 車線の左側
                startingRoadSideLinePoint = startingRoad.GetLeftPoint(Road.GetDifferentEdgeID(startingEdgeID), startingLaneID);
                startingRoadSideLineVector = startingRoad.alongVectors[Road.GetDifferentEdgeID(startingEdgeID)];

                //ending: 車線の左側
                endingRoadSideLinePoint = endingRoad.GetLeftPoint(endingEdgeID, endingLaneID);
                endingRoadSideLineVector = endingRoad.alongVectors[endingEdgeID];
            }

            //交点を求める
            output.center = GetIntersection(startingRoadSideLinePoint, startingRoadSideLineVector, endingRoadSideLinePoint, endingRoadSideLineVector);

            //カーブの始点・終点を求める
            Vector2 startingPoint = GetFootOfPerpendicular(output.center, startingRoad.GetStartingPoint(Road.GetDifferentEdgeID(startingEdgeID), startingLaneID), startingRoad.alongVectors[Road.GetDifferentEdgeID(startingEdgeID)]);
            Vector2 endingPoint = GetFootOfPerpendicular(output.center, endingRoad.GetStartingPoint(endingEdgeID, endingLaneID), endingRoad.alongVectors[endingEdgeID]);

            //半径を求める
            output.radius = Vector2.Distance(startingPoint, output.center);

            //角度を求める
            output.startingAngle = Quaternion.FromToRotation(Vector2.right, startingPoint - output.center).eulerAngles.z;
            output.endingAngle = Quaternion.FromToRotation(Vector2.right, endingPoint - output.center).eulerAngles.z;

            return output;
        }

        /// <summary>
        /// 二つの線分の交点を求める
        /// </summary>
        private static Vector2 GetIntersection(Vector2 line0Point, Vector2 line0Vector, Vector2 line1Point, Vector2 line1Vector)
        {
            // 外積を求める
            float cross = line0Vector.x * line1Vector.y - line0Vector.y * line1Vector.x;

            // 線分が平行である場合
            if (Mathf.Approximately(cross, 0f))
            {
                Debug.LogError("平行");
                return Vector2.zero;
            }

            // 交点を求める
            float t = ((line1Point.x - line0Point.x) * line1Vector.y - (line1Point.y - line0Point.y) * line1Vector.x) / cross;
            Vector2 intersectionPoint = line0Point + line0Vector * t;

            return intersectionPoint;
        }

        /// <summary>
        /// RunningJoint中の回転を取得
        /// </summary>
        private Quaternion GetRotationInJoint(float angleInCurve, bool clockwise)
        {
            float addition;

            //90度足し引きすることで進行方向を向く
            if (clockwise)
            {
                addition = -90f;
            }
            else
            {
                addition = 90f;
            }

            return Quaternion.Euler(0f, 0f, angleInCurve + addition);
        }

        /// <summary>
        /// 垂線の足を求める
        /// </summary>
        private static Vector2 GetFootOfPerpendicular(Vector2 point, Vector2 linePoint, Vector2 lineVector)
        {
            Vector2 v = point - linePoint;
            float t = Vector2.Dot(v, lineVector) / lineVector.sqrMagnitude;
            Vector2 foot = linePoint + lineVector * t;

            return foot;
        }

        /// <summary>
        /// 点が直線上にあるか判定する
        /// </summary>
        private static bool CheckInLine(Vector3 point, Vector3 linePoint, Vector3 lineVector)
        {
            Vector3 difference = point - linePoint;

            //外積の大きさを求める
            float outer = Vector3.Cross(lineVector, difference).magnitude;

            //0なら直線上
            return Mathf.Approximately(outer, 0f);
        }

        /// <summary>
        /// 極座標から平面座標に変換
        /// </summary>
        private static Vector2 GetPositionFromPolar(Vector2 pole, float radius, float angular)
        {
            return pole + (Vector2)(Quaternion.Euler(0f, 0f, angular) * Vector2.right) * radius;
        }

        /// <summary>
        /// Jointを曲がるとき、円弧を描く。その円弧の始点、終点、中心。
        /// </summary>
        private struct CurveRoute
        {
            /// <summary>
            /// 回っているRoadJoint
            /// </summary>
            public RoadJoint curvingJoint;

            /// <summary>
            /// 円弧の中心
            /// </summary>
            public Vector2 center;

            /// <summary>
            /// 円弧半径
            /// </summary>
            public float radius;

            /// <summary>
            /// 始点の角度（ｘ軸から反時計回りに）
            /// </summary>
            public float startingAngle;

            /// <summary>
            /// 終点の角度（ｘ軸から反時計回りに）
            /// </summary>
            public float endingAngle;

            /// <summary>
            /// 時計回りか
            /// </summary>
            public bool clockwise;

            public Vector2 startingPoint
            {
                get
                {
                    return GetPositionFromPolar(center, radius, startingAngle);
                }
            }

            public Vector2 endingPoint
            {
                get
                {
                    return GetPositionFromPolar(center, radius, endingAngle);
                }
            }
        }
    }
}