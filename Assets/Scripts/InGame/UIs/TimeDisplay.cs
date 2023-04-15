using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace InGame.UI
{
    /// <summary>
    /// 残り時間を表示
    /// </summary>
    public class TimeDisplay : MonoBehaviour
    {
        void Update()
        {
            GetComponent<TextMeshProUGUI>().text = MakeText();
        }

        private string MakeText()
        {
            string output = "";

            int currentTime = (int)GameManager.Instance.gameTimeLeft;

            int minute = currentTime / 60;
            output += minute.ToString();

            output += " : ";

            int second = currentTime % 60;
            if(second < 10)
            {
                //「08」のように表示させる
                output += "0";
            }
            output += second;

            return output;
        }
    }
}
