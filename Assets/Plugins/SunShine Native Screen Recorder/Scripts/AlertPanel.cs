using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AlertPanel : MonoBehaviour
{
    [SerializeField] private Text messageText;

    public void ShowAlert(string message)
    {
        messageText.text = message;
        this.gameObject.SetActive(true);
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }

}
