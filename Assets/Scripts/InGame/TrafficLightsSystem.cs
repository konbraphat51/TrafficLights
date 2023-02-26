using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class TrafficLightsSystem : MonoBehaviour
    {
        [Tooltip("対応する信号機")]
        [SerializeField] private TrafficLight[] trafficLights;

        private Dictionary<Road, TrafficLight> correspondingTrafficLight = new Dictionary<Road, TrafficLight>();


        /// <summary>
        /// 起動済みのTrafficLightを登録。各TrafficLightの初期化処理もする
        /// </summary>
        /// <param name="roads">時計回りに登録すること</param>
        public void RegisterTrafficLights(Road[] roads, Dictionary<Road, int> edges)
        {
            //時計回り順にTrafficLightを起動・登録
            trafficLights = new TrafficLight[roads.Length];
            for(int cnt = 0; cnt < roads.Length; cnt++)
            {
                //TrafficLightを登録
                trafficLights[cnt] = roads[cnt].ActivateTrafficLight(edges[roads[cnt]]);

                //Roadも登録
                correspondingTrafficLight[roads[cnt]] = trafficLights[cnt];
            }
        }
    }
}