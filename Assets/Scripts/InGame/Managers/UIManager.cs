using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InGame.UI;

namespace InGame
{
    /// <summary>
    /// UIの統括を行う
    /// </summary>
    public class UIManager : SingletonMonoBehaviour<UIManager>
    {
        [Header("オブジェクト指定")]
        
        [Tooltip("得点表示UI")]
        [SerializeField] private ScoreUI scoreUI;

        /// <summary>
        /// 得点更新
        /// </summary>
        public void OnPointsChanged(int changement)
        {
            //更新後の得点を取得
            int currentPoints = GameManager.Instance.score;

            //更新させる
            scoreUI.UpdateScore(currentPoints);
        }
    }
}