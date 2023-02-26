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

        public enum InitiallyGreen
        {
            odd,
            even
        }

        [Tooltip("初期状態で緑信号になるもの")]
        [SerializeField] private InitiallyGreen initiallyGreen = InitiallyGreen.even;

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

            //初期色をセット
            SetInitialLight();
        }

        /// <summary>
        /// TrafficLightの初期色をセット
        /// 偶数あるいは奇数のTrafficLightを緑にし、残りを赤にする
        /// </summary>
        private void SetInitialLight()
        {
            for (int cnt = 0; cnt < trafficLights.Length; cnt++)
            {
                switch (initiallyGreen)
                {
                    case InitiallyGreen.even:
                        if (cnt % 2 == 0)
                        {
                            trafficLights[cnt].SetLight(TrafficLight.Color.green);
                        }
                        else
                        {
                            trafficLights[cnt].SetLight(TrafficLight.Color.red);
                        }
                        break;

                    case InitiallyGreen.odd:
                        if (cnt % 2 == 0)
                        {
                            trafficLights[cnt].SetLight(TrafficLight.Color.green);
                        }
                        else
                        {
                            trafficLights[cnt].SetLight(TrafficLight.Color.red);
                        }
                        break;
                }
            }
        }
    }
}