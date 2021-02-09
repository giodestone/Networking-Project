using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// For displaying the player's IP in the main menu.
/// </summary>
public class PlayerIPTextScript : MonoBehaviour
{
    [SerializeField]
    Text IPText;

    /// <summary>
    /// Get all IPv4 entries and set IPText to them.
    /// </summary>
    void Start()
    {
        IPText.text = "Your IP Addresses:\n";
        var ips = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in ips.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                IPText.text += ip.ToString() + "\n";
        }
    }
}
