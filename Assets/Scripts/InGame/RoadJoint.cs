using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    /// <summary>
    /// Road‚ÆRoad‚ğ‚Â‚È‚®Joint
    /// ‹È‚ª‚èŠp‚É‚Í‚±‚ÌRoadJoint
    /// Œğ·“_AŠOŠE‚Ö‚ÌÚ‘±Œû‚ÍqƒNƒ‰ƒX‚ğg—p
    /// </summary>
    public class RoadJoint : MonoBehaviour
    {
        /// <summary>
        /// ‚±‚ÌJoint‚ÉÚ‘±‚µ‚Ä‚¢‚éRoad
        /// </summary>
        public List<Road> connectedRoads { get; private set; } = new List<Road>();

        /// <summary>
        /// ŠeconnectedRoad‚Ì‚Ç‚¿‚ç‚Ì’[‚ªŒq‚ª‚Á‚Ä‚¢‚é‚©
        /// </summary>
        public Dictionary<Road, int> edges { get; private set; } = new Dictionary<Road, int>();

        /// <summary>
        /// Œq‚ª‚Á‚½“¹‚ğ“o˜^
        /// </summary>
        /// <param name="edge">“¹˜H‚Ì‚Ç‚¿‚ç‚Ì’[‚©iRoad.Edge‚Ì”Ô†‚É‘Î‰j</param>
        public void RegisterRoad(Road road, int edge)
        {
            //“¹‚ğ“o˜^
            connectedRoads.Add(road);

            //’[‚ğ“o˜^
            edges[road] = edge;
        }
    }
}