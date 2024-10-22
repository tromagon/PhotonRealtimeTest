using TMPro;
using UnityEngine;

namespace Test
{
    public class LogPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI logText;
        
        void OnEnable()
        {
            Application.logMessageReceived += LogCallback;
        }

        void OnDisable()
        {
            Application.logMessageReceived -= LogCallback;
        }

        void LogCallback(string logString, string stackTrace, LogType type)
        {
            logText.text += logString + "\r\n";
        }
    }
}