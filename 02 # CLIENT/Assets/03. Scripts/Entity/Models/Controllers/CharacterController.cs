using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharacterController : MonoBehaviour
{
    private Camera pCam;
    public float sensitivity = 500f;
    public float clampAngle = 85f;

    private float verticalRotation;
    private float horizontalRotation;

    public string username;
    public bool nullSend = false;

    private void Start()
    {
        pCam = this.GetComponentInChildren<Camera>();
        pCam.transform.rotation = new Quaternion(0, 0, 0, 0);
        verticalRotation = transform.localEulerAngles.x;
        horizontalRotation = EntityHandler.GetEntity<Character>(EntityGroup.PLAYER, Client.instance.myId).transform.eulerAngles.y;
    }

    private void Update()
    {
        StartCoroutine("CheckInput");
        CheckCamera();
    }

    #region KeyInput 
    IEnumerator CheckInput()
    {
        yield return new WaitForFixedUpdate();
        bool[] inputs = InputHandler.GetServerInputs().ToArray();

        if (inputs.Count(i => i) > 0 && !GameManager.instance.gameObject.GetComponent<UIHandler>().UIActive()) SendInputToServer(inputs);
        else if (!nullSend)
        {
            SendInputToServer(Enumerable.Range(0, inputs.Length).Select(i => false).ToArray());
            nullSend = true;
        }
    }

    private void SendInputToServer(bool[] _inputs)
    {
        nullSend = false;

        PacketHandler.SendPacket(ClientPacket.PLAYER_INPUT, new Dictionary<string, object>()
        {
            { "input", _inputs },
            { "rotation", transform.rotation }
        },
        udp: true,
        useEncrypt: false);
    }

    #endregion

    #region MouseInput
    private void CheckCamera()
    {
        UIHandler uiHandler = GameManager.instance.GetComponent<UIHandler>();
        if (!uiHandler.UIActive())
        {
            Look();
            Debug.DrawRay(transform.position, transform.forward * 2, Color.red);
        }
    }

    private void Look()
    {
        float _mouseVertical = -Input.GetAxis("Mouse Y");
        float _mouseHorizontal = Input.GetAxis("Mouse X");

        verticalRotation += _mouseVertical * sensitivity * Time.deltaTime;
        horizontalRotation += _mouseHorizontal * sensitivity * Time.deltaTime;

        verticalRotation = Mathf.Clamp(verticalRotation, -clampAngle, clampAngle);

        transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        EntityHandler.GetEntity(EntityGroup.PLAYER).transform.rotation = Quaternion.Euler(0f, horizontalRotation, 0f);
    }
    #endregion
}
