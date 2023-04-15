using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using InGame;

namespace Result
{
    /// <summary>
    /// 最終的な得点を表示
    /// </summary>
    public class ResultPoint : MonoBehaviour
    {
        void Start()
        {
            GetComponent<TextMeshProUGUI>().text = GameManager.score.ToString();
        }
    }
}