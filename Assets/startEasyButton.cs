using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class startEasyButton : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        gameObject.GetComponent<Button>().onClick.AddListener(ChangeEasyScene);

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ChangeEasyScene()
    {
        SceneManager.LoadScene("InGame");
    }
}
