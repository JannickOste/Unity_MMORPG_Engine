using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

class StartMenu : UIBase
{
    public GameObject startMenu;
    Dictionary<string, Action> buttonListeners;

    /* Login Field Variables */
    public GameObject loginWindow;
    public UnityEngine.UI.InputField logNameInput;
    public UnityEngine.UI.InputField logPassInput;

    /* Registration Field Variables */
    public GameObject registerWindow;
    public UnityEngine.UI.InputField regNameInput;
    public UnityEngine.UI.InputField regPassInput;
    public UnityEngine.UI.InputField regConfPassInput;

    /* Errorbox Variables */
    public GameObject errorWindow;
    public TMPro.TMP_Text errorLbl;
    public UnityEngine.UI.Button errorBtn;

    private Tuple<string, string>[] GetPostFields(string target) => new Tuple<string, string>[]
    {
        new Tuple<string, string>("target",   target),
        new Tuple<string, string>("username", logNameInput.text),
        new Tuple<string, string>("password", logPassInput.text),
        new Tuple<string, string>("mac",      Misc.MACAddr)
    };

    public override void Load()
    {
        base.Load();

        buttonListeners = new Dictionary<string, Action>
        {
            { "EXIT_GAME_BUTTON",       new Action(() => Application.Quit()) },
            { "LOGIN_BUTTON",           new Action(() => Misc.MakeWebRequest(
                                                            this,
                                                            new Action<object, object>((webout, isError) =>
                                                            {
                                                                if(!(bool)isError)
                                                                {
                                                                    Debug.Log(webout);
                                                                    if (webout.ToString().StartsWith("0"))
                                                                        ConnectToServer(webout.ToString().Split(':').Last());
                                                                }
                                                            }), GetPostFields("login")))},
            { "REGISTER_SUBMIT_BUTTON", new Action(() =>  Register() ) },

            { "LOAD_LOGIN_BUTTON",      new Action(() => { this.startMenu.SetActive(false);      this.loginWindow.SetActive(true); })},
            { "LOAD_REGISTER_BUTTON",   new Action(() => { this.startMenu.SetActive(false);      this.registerWindow.SetActive(true); })},
            { "LOGIN_BACK_BUTTON",      new Action(() => { this.loginWindow.SetActive(false);    this.startMenu.SetActive(true); })},
            { "REGISTER_CANCEL_BUTTON", new Action(() => { this.registerWindow.SetActive(false); this.startMenu.SetActive(true); })}
        };

        this.LoadComponents();


    }

    private void LoadComponents()
    {
        /* Assign variables */
        foreach (var componentSet in new List<dynamic>()
        {
            this.GetInputFields(),
            GameObject.FindGameObjectsWithTag("UIComponent"),
            this.GetTextMeshProUGUIs().Where(i => i.name == "ERROR_LABEL")
        })
        {
            foreach(dynamic rawComponent in componentSet)
            {
                dynamic component = new[] { typeof(GameObject), typeof(TMPro.TextMeshProUGUI) }.ToList().Contains(rawComponent.GetType())
                                        ? rawComponent
                                        : Convert.ChangeType(rawComponent, rawComponent.GetType());


                switch (component.name)
                {
                    // Inputfields
                    case "LogUsernameInput": logNameInput = component; break;
                    case "LogPasswordInput": logPassInput = component; break;
                    case "REGISTER_USERNAME_INPUT": regNameInput = component; break;
                    case "REGISTER_PASSWORD_INPUT": regPassInput = component; break;
                    case "REGISTER_PASSWORD_CONFIRM_INPUT": regConfPassInput = component; break;

                    // Windows
                    case "START_MENU": startMenu = component; break;
                    case "LOGIN_MENU": loginWindow = component; break;
                    case "ERROR_WINDOW": errorWindow = component; break;
                    case "REGISTRATION_WINDOW": registerWindow = component; break;
                }
            }
        }



        foreach (UnityEngine.UI.Button btn in _template.GetComponentsInChildren<UnityEngine.UI.Button>())
        {
            btn.onClick.RemoveAllListeners();
            Action listener;
            if (buttonListeners.TryGetValue(btn.name, out listener))
            {   
                if (btn.name == "REGISTER_SUBMIT_BUTTON") Debug.Log(btn.name);
                btn.onClick.AddListener(delegate () { listener(); });
            }

        }

        foreach (GameObject window in new[] { registerWindow, loginWindow, errorWindow })
            window.SetActive(false);
    }



    private void Register()
    {
        Debug.Log("Attempting to register");    
        if (regNameInput.text.Length <= 5) DisplayMessage("Username must be atleast 6 karakters.", registerWindow);
        else if (regPassInput.text.Length <= 5) DisplayMessage("Password must be atleast 6 karakters.", registerWindow);
        else if (regPassInput.text != regConfPassInput.text) DisplayMessage("Password does not match confirmed password", registerWindow);
        else
        {
            Misc.MakeWebRequest(
                this,
                new Action<object, object>((webout, isError) =>
                {
                    if (webout.ToString().StartsWith("0"))
                    {
                        registerWindow.SetActive(false);
                        DisplayMessage("Registration succesfull..", loginWindow);
                    }
                    else Debug.Log(webout);
                }), GetPostFields("register"));
        }
    }

    private void DisplayMessage(string errorText, GameObject returnWindow)
    {
        // Toggle error window
        returnWindow.SetActive(false);
        errorWindow.SetActive(true);

        // Assign  window switch & error text.
        errorLbl.SetText(errorText);
        errorBtn.onClick.RemoveAllListeners();
    
        errorBtn.onClick.AddListener(delegate()
        {
            errorWindow.SetActive(false);
            returnWindow.SetActive(true);
        });
    }

    private void ConnectToServer(string client_secret)
    {
        Misc.PrintDebugLine("StartMenu", "Login", "Login accepted, attempting to login on game server.");
        DontDestroyOnLoad(GameObject.Find("Game Manager"));
        Client.instance.loginHash = client_secret;
        Client.instance.ConnectToServer(client_secret);

        if(Client.instance.tcp.socket.Connected)
        {
            SceneManager.LoadScene(1);
            GameManager.instance.gameObject.AddComponent<EntityHandler>();

            EntityHandler.Load();
        }
    }
}

