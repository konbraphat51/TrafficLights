using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace InGame
{
    /// <summary>
    /// 交差点。
    /// ３つ以上の道路をつなぐRoadJoint
    /// </summary>
    public class Intersection : RoadJoint
    {
        [Tooltip("対応する信号機システム。nullなら信号機無しの交差点")]
        [SerializeField] private TrafficLightsSystem? trafficLightSystem;

        /// <summary>
        /// TrafficLightsSystemに道路、信号機を登録する
        /// ここでTrafficLightのActivateも行われる
        /// Roadが登録される前のStartに登録すると不具合が発生するので、その後に呼ぶ必要がある
        /// </summary>
        public void InitializeTrafficLightSystem()
        {
            //信号機が無い場合はキャンセル
            if(trafficLightSystem == null)
            {
                return;
            }

            //時計回りにRoadを並べる
            Road[] orderedRoads = ArrangeRoadsClockwise(connectedRoads.ToArray());

            //TrafficLightsSystemに登録
            trafficLightSystem.RegisterTrafficLights(orderedRoads, edges);
        }

        /// <summary>
        /// Intersectionから見てroadを時計回りに並べる
        /// </summary>
        private Road[] ArrangeRoadsClockwise(Road[] roads)
        {
            //各Roadとのx軸正方向からの時計回りの角度を求める
            Dictionary<Road, float> angles = new Dictionary<Road, float>();
            foreach(Road road in roads)
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
            foreach(KeyValuePair<Road, float> roadAngle in angles.OrderBy(c => c.Value))
            {
                orderedRoad.Add(roadAngle.Key);
            }

            return orderedRoad.ToArray();
        }
    }
}