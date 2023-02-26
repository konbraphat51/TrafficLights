using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class TrafficLightsSystem : MonoBehaviour
    {
        [Tooltip("‘Î‰‚·‚éM†‹@")]
        [SerializeField] private TrafficLight[] trafficLights;

        private Dictionary<Road, TrafficLight> correspondingTrafficLight = new Dictionary<Road, TrafficLight>();


        /// <summary>
        /// ‹N“®Ï‚İ‚ÌTrafficLight‚ğ“o˜^BŠeTrafficLight‚Ì‰Šú‰»ˆ—‚à‚·‚é
        /// </summary>
        /// <param name="roads">Œv‰ñ‚è‚É“o˜^‚·‚é‚±‚Æ</param>
        public void RegisterTrafficLights(Road[] roads, Dictionary<Road, int> edges)
        {
            //Œv‰ñ‚è‡‚ÉTrafficLight‚ğ‹N“®E“o˜^
            trafficLights = new TrafficLight[roads.Length];
            for(int cnt = 0; cnt < roads.Length; cnt++)
            {
                //TrafficLight‚ğ“o˜^
                trafficLights[cnt] = roads[cnt].ActivateTrafficLight(edges[roads[cnt]]);

                //Road‚à“o˜^
                correspondingTrafficLight[roads[cnt]] = trafficLights[cnt];
            }
        }
    }
}