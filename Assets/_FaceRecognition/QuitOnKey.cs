using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitOnKey : MonoBehaviour
{
    public KeyCode QuitHotKey = KeyCode.Escape;

    void Start() { }

    void Update()
    {
        if (Input.GetKeyDown(QuitHotKey))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
