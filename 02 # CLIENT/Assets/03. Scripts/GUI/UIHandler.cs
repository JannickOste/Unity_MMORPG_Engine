using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

class UIHandler : MonoBehaviour
{
    /* Interface vars */
    private UIBase currentUI;
    private UIBase overlay;

    public bool UIActive() => currentUI != null;
    public void SetUI(System.Type loadUI)
    {
        if (Assembly
            .GetAssembly(typeof(UIBase))
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(UIBase))).Contains(loadUI))
        {

            if (this.UIActive()) currentUI.Destroy();
            else if (loadUI == typeof(StartMenu) && overlay != null) overlay.Destroy();

            this.gameObject.AddComponent(loadUI);

            UIBase cUI = this.gameObject.GetComponent(loadUI) as UIBase;
            cUI.Load();

            if (loadUI != typeof(Overlay)) currentUI = cUI;
            else overlay = cUI;

        }
    }

    public UIBase GetCurrentUI(System.Type typeCheck = null)
    {
        if(currentUI != null)
        {
            if (typeCheck != null && currentUI.GetType() != typeCheck) 
                throw new System.InvalidOperationException($"Class of type \"{typeCheck.Name}\" is an invalid type, " +
                                                    $"current loaded type is {currentUI.GetType().Name}");
            else return currentUI;
        } return null;
    }

    public void HandleUIPacket(Dictionary<string, object> packetData)
    {
        if (packetData.ContainsKey("dialog"))
        {
            UIHandler uiHandler = GameManager.instance.GetComponent<UIHandler>();

            UIBase currentUI = uiHandler.GetCurrentUI();
            if (currentUI == null || currentUI.GetType() != typeof(Chatbox))
            {
                uiHandler.SetUI(typeof(Chatbox));

                try
                {
                    Chatbox chat = uiHandler.GetCurrentUI(typeof(Chatbox)) as Chatbox;
                    if (chat != null) chat.SetDialog(packetData["dialog"].ToString());

                }
                catch (System.InvalidOperationException ex)
                {
                    Debug.Log($"[UIHandler::ExceptionCaught]: {ex.Message}");
                }
            }
        }
    }
}


