using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace InGame
{
    /// <summary>
    /// Intersectionに一対一対応
    /// 複数のTrafficLightを取りまとめる
    /// プレイヤーの入力は本クラスが受け取る
    /// </summary>
    public class TrafficLightsSystem : MonoBehaviour
    {
        [Tooltip("対応する信号機")]
        [SerializeField] private TrafficLight[] trafficLights;

        private Dictionary<Road, TrafficLight> correspondingTrafficLight = new Dictionary<Road, TrafficLight>();

        /// <summary>
        /// 緑色になっている信号機の組み合わせ
        /// </summary>
        public enum GreenPattern
        {
            odd,
            even
        }

        [Tooltip("初期状態で緑信号になるもの")]
        [SerializeField] private GreenPattern initiallyGreen = GreenPattern.even;

        //現在緑になっているパターン
        private GreenPattern currentPattern;

        //黄色信号

        /// <summary>
        /// 起動済みのTrafficLightを登録。各TrafficLightの初期化処理もする
        /// </summary>
        /// <param name="roads">時計回りに登録すること</param>
        public void RegisterTrafficLights(Road[] roads, Dictionary<Road, int> edges)
        {
            //5個以上の信号機には対応できない
            Debug.Assert(roads.Length < 5);

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
                    case GreenPattern.even:
                        if (cnt % 2 == 0)
                        {
                            trafficLights[cnt].SetLight(TrafficLight.Color.green);
                        }
                        else
                        {
                            trafficLights[cnt].SetLight(TrafficLight.Color.red);
                        }
                        break;

                    case GreenPattern.odd:
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

            //パターンを記憶
            currentPattern = initiallyGreen;
        }

        /// <summary>
        /// 信号機切り替え
        /// </summary>
        public void ToggleLights()
        {

        }

        /// <summary>
        /// 指定されたパターンの次のパターンを返す
        /// 指定されたのが最後ならば最初のパターンを返す
        /// </summary>
        private GreenPattern GetNextPattern(GreenPattern pattern)
        {
            //引数が何番目かを取得する
            GreenPattern[] allPatterns = Enum.GetValues(typeof(GreenPattern)).Cast<GreenPattern>().ToArray();
            int thisIndex = Array.IndexOf(allPatterns, pattern);

            if (thisIndex < allPatterns.Length - 1)
            {
                return allPatterns[thisIndex + 1];
            }
            else
            {
                return allPatterns[0];
            }
        }
    }
}