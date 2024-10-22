using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Test
{
    public class ConnectionScreen : MonoBehaviour
    {
        [SerializeField] private NetworkConnection networkConnection;
        [SerializeField] private TMP_InputField usernameTextInput;
        [SerializeField] private Button createButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button copyCodeButton;
        [SerializeField] private GameObject sessionCodeGroup;
        [SerializeField] private TextMeshProUGUI sessionCodeText;
        [SerializeField] private TMP_InputField inputSessionCodeText;
        
        private void Awake()
        {
            createButton.onClick.AddListener(async () =>
            {
                var result = await networkConnection.CreateRoom(new NetworkConnection.ConnectionArgument
                {
                    Username = usernameTextInput.text
                });

                if (result.Success)
                {
                    sessionCodeGroup.SetActive(true);
                    sessionCodeText.text = result.SessionCode;
                }
            });
            
            joinButton.onClick.AddListener(async () =>
            {
                var result = await networkConnection.JoinRoom(new NetworkConnection.ConnectionArgument
                {
                    Session = inputSessionCodeText.text
                });

                if (result.Success)
                {
                    Debug.Log("Room joined");
                }
            });
            
            sessionCodeGroup.SetActive(false);
            
            copyCodeButton.onClick.AddListener(() =>
            {
                GUIUtility.systemCopyBuffer = sessionCodeText.text;
            });
        }
    }
}