/*


    THIS IS A TEST SCRIPT, YOU SHOULD REPLACE THIS WITH YOUR OWN SCRIPT TO PROPERLY INITIALIZE DISCORD
    ONLY USE THIS FOR TESTING PURPOSES.
    GITHUB: https://github.com/Derek-R-S


*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Discord;

public class DiscordManager : MonoBehaviour
{
    public static DiscordManager instance;
    private Discord.Discord discord;
    [SerializeField] bool usePTB;
    [SerializeField] long clientID;
    [SerializeField] CreateFlags createFlags;
    [SerializeField] private DiscordTransport transport;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            return;
        }

        Destroy(this);
    }

    private void Start()
    {
        System.Environment.SetEnvironmentVariable("DISCORD_INSTANCE_ID", usePTB ? "1" : "0");
        discord = new Discord.Discord(clientID, (ulong)createFlags);
        transport.Initialize(discord);
    }
}