using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace InGame
{
    /// <summary>
    /// RoadとRoadをつなぐJoint
    /// 曲がり角にはこのRoadJoint
    /// 交差点、外界への接続口は子クラスを使用
    /// </summary>
    public class RoadJoint : MonoBehaviour
    {
        /// <summary>
        /// このJointに接続しているRoad
        /// </summary>
        public List<Road> connectedRoads { get; private set; } = new List<Road>();

        /// <summary>
        /// connectedRoadsが時計回りに並べ替え済みか
        /// </summary>
        public bool sortedClockwise { get; private set; } = false;

        /// <summary>
        /// 各connectedRoadのどちらの端が繋がっているか
        /// </summary>
        public Dictionary<Road, int> edges { get; private set; } = new Dictionary<Road, int>();

        /// <summary>
        /// 繋がった道を登録
        /// </summary>
        /// <param name="edge">道路のどちらの端か（Road.Edgeの番号に対応）</param>
        public void RegisterRoad(Road road, int edge)
        {
            //道を登録
            connectedRoads.Add(road);

            //端を登録
            edges[road] = edge;
        }

        /// <summary>
        /// Intersectionから見てroadを時計回りに並べる
        /// </summary>
        public virtual void ArrangeRoadsClockwise()
        {
            //各Roadとのx軸正方向からの時計回りの角度を求める
            Dictionary<Road, float> angles = new Dictionary<Road, float>();
            foreach (Road road in connectedRoads)
            {
                //RoadとIntersectionの差ベクトル
                Vector3 dif = road.transform.position - transform.position;

                //x軸正方向との符号がついていない角度（ベクトルのなす角）
                float unsigned = Vector2.Angle(new Vector2(1, 0), dif);

                if (dif.y >= 0)
                {
                    //RoadがIntersectionより上側にあるとき、０～１８０度になっているところに180度足す
                    angles[road] = 360f - unsigned;
                }
                else
                {
                    //下にある場合はそのまま
                    angles[road] = unsigned;
                }
            }

            //角度が小さい順に並び替え
            List<Road> orderedRoad = new List<Road>();
            foreach (KeyValuePair<Road, float> roadAngle in angles.OrderBy(c => c.Value))
            {
                orderedRoad.Add(roadAngle.Key);
            }

            //時計回りに登録しなおす
            connectedRoads = orderedRoad;

            //時計回り並べ替え済みに
            sortedClockwise = true;
        }
    }
}