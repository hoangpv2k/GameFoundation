using System;
using System.Collections;
using CustomInspector;
using UnityEngine;

public class InternetController : SingletonDontDestroy<InternetController>
{
    [SerializeField] private InternetConfig internetConfig;
    [ReadOnly] public bool isConnected;

    private const float TimeStart = 0f;

    void Start()
    {
        var repeatRate = internetConfig.repeatRate;
        InvokeRepeating(nameof(CheckInternet), TimeStart, repeatRate);
    }

    private void CheckInternet()
    {
        StartCoroutine(CheckInternetConnection((isConnect) =>
        {
            isConnected = isConnect;
        }));
    }

    IEnumerator CheckInternetConnection(Action<bool> action)
    {
        WWW www = new WWW("http://google.com");
        yield return www;
        action(www.error == null);
    }
    private IEnumerator checkInternetConnection2(Action<bool> action)
    {
        bool result;
        result = Application.internetReachability != NetworkReachability.NotReachable;
        action(result);
        yield return null;
    }
}
