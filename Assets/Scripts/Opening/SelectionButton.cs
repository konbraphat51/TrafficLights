using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Opening
{
    public class SelectionButton : MonoBehaviour
    {
        [SerializeField] private string sceneName;

        /// <summary>
        /// ƒNƒŠƒbƒN‚³‚ê‚½Û‚Ìˆ—
        /// </summary>
        public void OnClick()
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}