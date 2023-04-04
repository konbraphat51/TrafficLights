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

        [Tooltip("車線変更時の回転速度")]
        [SerializeField] private float angularSpeedChangingLane = 10f;

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

        /// <summary>
        /// 次のRoadJoint。runningRoad開始時に更新
        /// </summary>
        private RoadJoint nextRoadJoint;

        /// <summary>
        /// 次へ向かう道路が平行なとき。trueならrunningJointではなくchangingLaneに移る。
        /// </summary>
        private bool nextIsParallel;

        /// <summary>
        /// 車両の正面ベクトル
        /// </summary>
        public Vector2 front
        {
            get
            {
                return transform.right;
            }
        }

        [Tooltip("道路の角度（度）がこれ以下なら平行とみなす")]
        [SerializeField] private float roadsParallelThreshold = 10f;

        [Tooltip("車線変更時、目的の車線までの回転半径がこれ以下になったら回転移動による調整を開始する。")]
        [SerializeField] private float thresholdRadiusChangingLane = 5f;

        [Tooltip("車線変更時、目的の車線との最大角度")]
        [SerializeField] private float angleMaxChangingLane = 10f;

        [Tooltip("車線変更時、道路との角度がこれ以下になったら道路と平行とみなす")]
        [SerializeField] private float parallelThresholdChangingLane = 3f;

        [Tooltip("同一直線上と判断する外積の閾値")]
        [SerializeField] private float onSameLineThreshold = 0.05f;

        /// <summary>
        /// 車線変更でカーブモードに入った
        /// </summary>
        private bool changingLaneRotating = false;

        /// <summary>
        /// 車線変更終了フラグ
        /// </summary>
        private bool changingLaneFinished = false;

        /// <summary>
        /// 車線変更時の円弧軌道
        /// </summary>
        private CurveRoute curveChangingLane;

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

                case State.changingLane:
                    ChangeLane();
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
            nextRoadJoint = road.GetDiffrentEdge(startingJoint);

            //現在位置を道路に開始位置に調整
            AdjustStartingPositionInRoad(road, laneID, edgeID, first);

            //現在の行き先の座標
            Vector2 destinationPoint;

            if(routes.Count > 0)
            {
                //>>次のJointが存在
                //次のJoint回転を計算
                if (TryGetNextCurveRoute(road.GetDiffrentEdge(startingJoint)))
                {
                    nextIsParallel = false;

                    //次のJoint移動がある場合、回転開始位置までrunningRoad
                    destinationPoint = currentCurveRoute.startingPoint;
                }
                else
                {
                    //>>平行
                    nextIsParallel = true;

                    //Jointまで走る
                    destinationPoint = road.GetDiffrentEdge(startingJoint).transform.position;
                }
            }
            else
            {
                //>>次が終点
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
        /// <returns>平行の場合はfalse</returns>
        private bool TryGetNextCurveRoute(RoadJoint curvingJoint)
        {
            //次のRoad
            Road nextRoad = routes.Peek();

            //次と平行
            if (IsParallel(currentRoad.alongVectors[0], nextRoad.alongVectors[0], roadsParallelThreshold))
            {
                return false;
            }

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
            return nextLaneID;
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
                    if (nextIsParallel)
                    {
                        //車線変更モード
                        StartChangingLane();
                    }
                    else
                    {
                        //Joint回転モードに入る
                        StartRunningJoint();
                    }
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
            currentAngle += GetAngularSpeedInJoint() * coef * Time.deltaTime;

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
            return CheckCircularFinished(currentAngle, currentCurveRoute);
        }

        /// <summary>
        /// 車線変更を開始
        /// </summary>
        private void StartChangingLane()
        {
            //CheckChangingLaneNecessary()より前に呼ぶ必要がある
            GetNextLane();

            //車線変更の必要がないか確認
            if (CheckChangingLaneNecessary())
            {
                StartRunningRoad(routes.Dequeue(), nextLaneID, nextRoadJoint);
                return;
            }

            changingLaneRotating = false;
            curveChangingLane = new CurveRoute();

            state = State.changingLane;
        }

        /// <summary>
        /// 車線変更
        /// </summary>
        private void ChangeLane()
        {
            Road nextRoad = routes.Peek();
            Vector2 nextVector = nextRoad.alongVectors[nextRoad.GetEdgeID(nextRoadJoint)];
            uint nextEdgeID = nextRoad.GetEdgeID(nextRoadJoint);
            Vector2 nextLaneStartingPoint = nextRoad.GetStartingPoint(nextEdgeID, nextLaneID);

            if(!changingLaneRotating)
            {
                TryMakeCurveChangingLane(nextLaneStartingPoint, nextVector);
            }

            if (changingLaneRotating)
            {
                //回転移動
                ChangeLaneRotation(nextLaneStartingPoint, nextVector, curveChangingLane);
            }
            else
            {
                //前進しながら曲がる
                ChangeLaneForward(nextLaneStartingPoint, nextVector);
            }
        }

        /// <summary>
        /// 車線変更が必要か確認する
        /// </summary>
        private bool CheckChangingLaneNecessary()
        {
            Road nextRoad = routes.Peek();
            uint edgeID = nextRoad.GetEdgeID(nextRoadJoint);

            return CheckInLine((Vector2)transform.position,
                (Vector2)nextRoad.GetStartingPoint(edgeID, nextLaneID),
                (Vector2)nextRoad.alongVectors[edgeID]);
        }

        /// <summary>
        /// 車線変更時、円弧移動を仮定して、回転移動モードに入るか判断
        /// </summary>
        private bool TryMakeCurveChangingLane(Vector2 nextLaneStartingPoint, Vector2 nextVector)
        {
            //平行ならfalse
            if(IsParallel(nextVector, front, parallelThresholdChangingLane))
            {
                return false;
            }

            //>>回転移動すると仮定したときの半径・中心を求める
            //現在の進行方向の法線ベクトル
            Vector2 perpendicularFromAhead = GetPerpendicular(front);

            //進行方向と車線方向の角の二等分線ベクトル
            Vector2 bisector = GetBisector(-front, nextVector);

            //回転中心の座標
            Vector2 rotationCenter = GetIntersection(transform.position, perpendicularFromAhead, nextLaneStartingPoint, bisector);

            //回転半径
            float radius = Vector2.Distance(rotationCenter, transform.position);

            if (radius <= thresholdRadiusChangingLane)
            {
                //回転移動を開始
                changingLaneRotating = true;

                //>>回転軌道の具体化
                //回転方向を算出
                float angularDiference = Quaternion.FromToRotation(front, nextVector).eulerAngles.z;
                bool clockwise;
                if (angularDiference < 180f)
                {
                    //反時計回り
                    clockwise = true;
                }
                else
                {
                    //時計回り
                    clockwise = false;
                }

                //カーブの始点・終点を求める
                Vector2 startingPoint = transform.position;
                Vector2 endingPoint = GetFootOfPerpendicular(rotationCenter, nextLaneStartingPoint, nextVector);

                //角度を求める
                float startingAngle = GetAngular(startingPoint - rotationCenter);
                float endingAngle = GetAngular(endingPoint - rotationCenter);

                //軌道の保存
                curveChangingLane.center = rotationCenter;
                curveChangingLane.radius = radius;
                curveChangingLane.clockwise = clockwise;
                curveChangingLane.startingAngle = startingAngle;
                curveChangingLane.endingAngle = endingAngle;

                //現在の角度
                currentAngle = startingAngle;

                return true;
            }

            return false;
        }

        /// <summary>
        /// 車線変更時の、最後の回転移動
        /// </summary>
        private void ChangeLaneRotation(Vector2 targetLanePoint, Vector2 targetLaneVector, CurveRoute curve)
        {
            //角度を移動させる
            if (curveChangingLane.clockwise)
            {
                currentAngle -= GetAngularSpeedInChangingLane() * Time.deltaTime;
            }
            else
            {
                currentAngle += GetAngularSpeedInChangingLane() * Time.deltaTime;
            }

            if(CheckCircularFinished(currentAngle, curve)){
                //>>行き過ぎたので車線方向へ戻す

                //座標
                transform.position = GetPositionFromPolar(curve.center, curve.radius, curve.endingAngle);

                //回転
                transform.rotation = GetRotationInJoint(curve.endingAngle, curve.clockwise);

                //車線変更を終了
                StartRunningRoad(routes.Dequeue(), nextLaneID, nextRoadJoint);
            }
            else
            {
                //座標
                transform.position = GetPositionFromPolar(curve.center, curve.radius, currentAngle);

                //回転
                transform.rotation = GetRotationInJoint(currentAngle, curve.clockwise);
            }
        }

        /// <summary>
        /// 車線変更時、前進しながら曲がる
        /// </summary>
        private void ChangeLaneForward(Vector2 targetLanePoint, Vector2 targetLaneVector)
        {
            //曲がる方向を算出
            bool shouldTurnRight = !IsRightFromVector(transform.position, targetLanePoint, targetLaneVector);

            //進行方向に対する車線の角度
            float angularDifference = Vector2.Angle(front, targetLaneVector);

            //回転角を算出
            float angularMovement = Mathf.Min(angleMaxChangingLane -angularDifference, GetAngularSpeedInChangingLane() * Time.deltaTime);

            if (shouldTurnRight)
            {
                //右に曲がる場合、正負反転
                angularMovement = -angularMovement;
            }

            //回転
            transform.rotation = transform.rotation * Quaternion.Euler(0, 0, angularMovement);

            //回転後に前進
            transform.position += (Vector3)(front.normalized * GetSpeedInRoad() * Time.deltaTime);
        }

        private float GetAngularSpeedInChangingLane()
        {
            return angularSpeedChangingLane;
        }

        /// <summary>
        /// 目的地に到着して、消えてGameManagerに報告
        /// </summary>
        private void OnArrivedDestination()
        {
            //消える
            Destroy(this.gameObject);
        }

        /// <summary>
        /// Joint回転中の角速度
        /// </summary>
        /// <returns></returns>
        private float GetAngularSpeedInJoint()
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
            output.startingAngle = GetAngular(startingPoint - output.center);
            output.endingAngle = GetAngular(endingPoint - output.center);

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
        private bool CheckInLine(Vector3 point, Vector3 linePoint, Vector3 lineVector)
        {
            Vector3 difference = point - linePoint;

            //外積の大きさを求める
            float outer = Vector3.Cross(lineVector, difference).magnitude;

            //0なら直線上
            return IsSame(outer, 0f, onSameLineThreshold);
        }

        /// <summary>
        /// 極座標から平面座標に変換
        /// </summary>
        private static Vector2 GetPositionFromPolar(Vector2 pole, float radius, float angular)
        {
            return pole + (Vector2)(Quaternion.Euler(0f, 0f, angular) * Vector2.right) * radius;
        }

        /// <summary>
        /// 二つのベクトルが平行か（閾値以下）か返す
        /// </summary>
        private bool IsParallel(Vector2 vec0, Vector2 vec1, float threshold)
        {
            
            float angle = Vector2.Angle(vec0, vec1);

            if((angle <= threshold)
                ||(Mathf.Abs(angle -180f) <= threshold)
                ||(Mathf.Abs(angle -360f) <= threshold)){
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 誤差を許して同一値を返す
        /// </summary>
        private bool IsSame(float v0, float v1, float threshold)
        {
            return (Mathf.Abs(v0 - v1) <= threshold);
        }

        /// <summary>
        /// 点と直線の距離を求める
        /// </summary>
        private static float GetDistance(Vector2 point, Vector2 linePoint, Vector2 lineVector)
        {
            //pointの相対座標
            Vector2 pointToLineStart = point - linePoint;

            //pointからの正射影点
            float dotProduct = Vector2.Dot(pointToLineStart, lineVector);
            Vector2 projection = linePoint + lineVector * dotProduct;

            //相対座標と正射影点の距離を求めれば良い
            return Vector2.Distance(point, projection);
        }

        /// <summary>
        /// 法線ベクトルを求める
        /// </summary>
        private static Vector2 GetPerpendicular(Vector2 vec)
        {
            return new Vector2(vec.y, -vec.x);
        }

        private static Vector2 GetBisector(Vector2 vec0, Vector2 vec1)
        {
            Vector2 u0 = vec0.normalized;
            Vector2 u1 = vec1.normalized;

            return (u0 + u1).normalized;
        }

        /// <summary>
        /// 与えられたベクトルに対し、与えられた点が右にあるかを返す
        /// </summary>
        private static bool IsRightFromVector(Vector2 point, Vector2 linePoint, Vector2 lineVector)
        {
            float angularDiference = Quaternion.FromToRotation(lineVector, point-linePoint).eulerAngles.z;

            if (angularDiference < 180f)
            {
                //左にある
                return false;
            }
            else
            {
                //右にある
                return true;
            }
        }

        /// <summary>
        /// 回転運動が終了したか確認
        /// </summary>
        private static bool CheckCircularFinished(float currentAngle, CurveRoute curveRoute)
        {
            if (curveRoute.clockwise)
            {
                //時計回り
                if (curveRoute.startingAngle > curveRoute.endingAngle)
                {
                    //x軸正を経過しない
                    return (currentAngle <= curveRoute.endingAngle);
                }
                else
                {
                    //x軸正を経過
                    return (currentAngle <= curveRoute.endingAngle - 360);
                }
            }
            else
            {
                //反時計回り
                if (curveRoute.startingAngle < curveRoute.endingAngle)
                {
                    //x軸正を経過しない
                    return (currentAngle >= curveRoute.endingAngle);
                }
                else
                {
                    //x軸正を経過
                    return (currentAngle >= curveRoute.endingAngle + 360);
                }
            }
        }

        /// <summary>
        /// ベクトルのｘ軸正からの反時計回りの角度(0-360)を算出
        /// </summary>
        /// <returns></returns>
        private float GetAngular(Vector2 vector)
        {
            Quaternion q = Quaternion.FromToRotation(Vector2.right, vector);

            float z = q.eulerAngles.z;

            if (Mathf.Approximately(z, 0f))
            {
                //180度回転の場合、x, y回転とみなされてzが０になっている可能性がある
                if ((q.eulerAngles.x > 90f) || (q.eulerAngles.y > 90f))
                {
                    z = 180f;
                }
            }

            return z;
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