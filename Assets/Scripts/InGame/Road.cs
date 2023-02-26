using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class Road : MonoBehaviour
    {
        [Tooltip("道路の両端の空オブジェクト")]
        [SerializeField] private GameObject[] edgeObjects;

        public RoadJoint[] connectedJoints { get; private set; } = new RoadJoint[2];

        private void Start()
        {
            //配置最適化
            OptimizeArrangement();
        }

        /// <summary>
        /// 両端に最近傍のRoadJointを探索し、配置を最適化
        /// </summary>
        private void OptimizeArrangement()
        {
            //接続RoadJointを取得
            GetConnectedJoints();

            //最適化前の両端を結ぶベクトル
            Vector3 originalPath = edgeObjects[0].transform.position - edgeObjects[1].transform.position;
            //最適化後
            Vector3 optimizedPath = connectedJoints[0].transform.position - connectedJoints[1].transform.position;

            //移動
            transform.position = (connectedJoints[0].transform.position + connectedJoints[1].transform.position) / 2;

            //>>拡大あるいは縮小
            //倍率
            float extensionCoef = optimizedPath.magnitude / originalPath.magnitude;
            //横向きに拡大・縮小
            transform.localScale = new Vector3(extensionCoef * transform.localScale.x, transform.localScale.y, transform.localScale.z);

            //回転
            Quaternion rotation = Quaternion.FromToRotation(originalPath, optimizedPath);
            transform.rotation *= rotation;
        }

        /// <summary>
        /// 両端に最近傍のRoadJointを取得
        /// </summary>
        private void GetConnectedJoints()
        {
            //両端について
            for (int cnt = 0; cnt < 2; cnt++)
            {
                Vector3 edgePosition = edgeObjects[cnt].transform.position;

                //最近傍を探して登録
                connectedJoints[cnt] = GetNearestJoint(edgePosition);
            }
        }

        /// <summary>
        /// 最も近いRoadJointを探す
        /// </summary>
        private RoadJoint GetNearestJoint(Vector3 searchingPoint)
        {
            //全RoadJointを取得
            RoadJoint[] allJoints = FindObjectsOfType<RoadJoint>();

            RoadJoint nearestJoint = null;

            //最近傍を線形探索
            float minDistance = float.MaxValue;
            foreach (RoadJoint joint in allJoints)
            {
                Vector3 difference = searchingPoint - joint.transform.position;
                float distance = difference.magnitude;

                if (distance < minDistance)
                {
                    nearestJoint = joint;
                    minDistance = distance;
                }
            }

            //nullだとおかしい（RoadJointが検知されていない）
            Debug.Assert(nearestJoint != null);

            return nearestJoint;
        }
    }
}