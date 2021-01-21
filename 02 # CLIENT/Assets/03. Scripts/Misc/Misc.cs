using System;
using System.Collections;
using System.Linq;
using System.Net.NetworkInformation;
using UnityEngine;

class Misc
{
    private sealed class WebHandler
    {
        private static netData output;

        public struct netData
        {
            public string output;
            public bool isError;
        }

        public static IEnumerator WebRequest(System.Action<netData> callback, params object[] parameters)
        {

            netData data = new netData();
            data.output = "";
            data.isError = false;

            string uri = null;
            WWWForm form = new WWWForm();
            foreach (var param in parameters)
            {
                try
                {
                    Tuple<string, string> paramData = (Tuple<string, string>)param;
                    if (paramData.Item1 != "target") form.AddField(paramData.Item1, paramData.Item2);
                    else uri = string.Format("{0}?{1}", Constants.LOGIN_SERVER, paramData.Item2);
                }
                catch (InvalidCastException ex)
                {
                    Debug.Log("Invalid field data type supplied, use Tuple<string, string> to pass in (field, value)");
                }
            }

            if (form.data.Length > 0 & uri != null)
            {
                using (UnityEngine.Networking.UnityWebRequest req = UnityEngine.Networking.UnityWebRequest.Post(uri, form))
                {
                    yield return req.SendWebRequest();

                    if (req.isNetworkError | req.isHttpError)
                    {
                        data.output = req.downloadHandler.text;
                        data.isError = true;
                    }
                    else data.output = req.downloadHandler.text;
                }
            }
            else if (uri == null) Misc.PrintDebugLine("WebHandler", "WebRequest", "No target location specified");


            callback(data);
        }

        public static void MakeWebRequest(MonoBehaviour targetInstance, Action<object, object> parseAction, params object[] parameters)
            => targetInstance.StartCoroutine(WebRequest((webOut) => parseAction(webOut.output, webOut.isError), parameters));
    }

    public static void MakeWebRequest(MonoBehaviour targetInstance, Action<object, object> parseAction, params Tuple<string, string>[] fieldInfo)
        => WebHandler.MakeWebRequest(targetInstance, parseAction, fieldInfo);

    public static void PrintDebugLine(string className, string funcName, string line)
    {
        //UnityEngine.Debug.Log($"[{className}::{funcName}]: {line}");
    }

    public static string MACAddr => NetworkInterface.GetAllNetworkInterfaces()
                                                .Where(i => !string.IsNullOrEmpty(i.GetPhysicalAddress().ToString()))
                                                .Select(i => i.GetPhysicalAddress().ToString()).FirstOrDefault();
}

