using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace InGame.UI
{
    /// <summary>
    /// カウントダウンを表示
    /// </summary>
    public class CountDownDisplay : MonoBehaviour
    {
        private void Update()
        {
            GetComponent<TextMeshProUGUI>().text = ((int)GameManager.Instance.countDownTimeLeft + 1).ToString();
        }
    }
}

