using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

class Chatbox : UIBase
{
    private TMPro.TextMeshProUGUI textLabel;
    private UnityEngine.UI.Button[] chatButtons;
    private List<string> dialogs;

    private bool buttonsEnabled => chatButtons.Select(btn => btn.gameObject.activeSelf).Count(cBool => cBool) > 0;

    public void FixedUpdate()
    {
        if (dialogs != null)
        {
            StartCoroutine("CheckActions");
            if (buttonsEnabled) StartCoroutine("CheckInput");
        }
    }

    public override void Load()
    {
        base.Load();

        textLabel = this.GetTextMeshProUGUIs().FirstOrDefault();
        chatButtons = this.GetButtons();

        foreach (var button in chatButtons)
            button.gameObject.SetActive(false);
    }

    public void SetDialog(string dialog)
    {
        dialogs = dialog.Split('$').ToList();
        textLabel.SetText(dialogs.FirstOrDefault());

        nextAction = System.DateTime.Now.AddMilliseconds(Constants.INTERACTION_DELAY_IN_MS);
    }

    private IEnumerator CheckActions()
    {
        yield return new WaitForFixedUpdate();

        if (InputHandler.keyPresed(KEY.INTERACT) && System.DateTime.Now > nextAction)
        {
            int current_index = dialogs.IndexOf(textLabel.text);
            if (current_index == dialogs.Count()-1) this.Destroy();
            else
            {
                textLabel.SetText(dialogs[current_index+1]);

                // Button string.
                if(textLabel.text.Contains(":") && textLabel.text.Contains("_"))
                {
                    List<string> buttonConfigurations = textLabel.text.Split('_').ToList();
                    foreach (string buttonConfig in buttonConfigurations)
                    {
                        int button_index = buttonConfigurations.IndexOf(buttonConfig);
                        int buttonAction;
                        if(int.TryParse(buttonConfig.Split(':').First(), out buttonAction))
                        {
                            UnityEngine.UI.Button target = this.chatButtons[button_index];

                            target.gameObject.SetActive(true);
                            target.GetComponentInChildren<TMPro.TextMeshProUGUI>().SetText($"[{button_index+1}]: {buttonConfig.Split(':').Last()}");

                            if (buttonAction != -1)
                            {
                                target.onClick.AddListener(delegate ()
                                {
                                    PacketHandler.SendPacket(ClientPacket.ACTION_REQUEST, new Dictionary<string, object>()
                                    {
                                        { "action_id", buttonAction }
                                    });
                                    
                                    this.Destroy();
                                });
                            }
                            else target.onClick.AddListener(delegate () { this.Destroy(); });
                        }
                    }

                    textLabel.gameObject.SetActive(false);
                }
            }

            nextAction = System.DateTime.Now.AddMilliseconds(Constants.INTERACTION_DELAY_IN_MS);
        }
        else if (InputHandler.keyPresed(KEY.MAINMENU)) this.Destroy();
    }
}