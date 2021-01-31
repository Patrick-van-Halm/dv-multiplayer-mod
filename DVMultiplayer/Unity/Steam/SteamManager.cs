using DVMultiplayer;
using Steamworks;
using System;

class SteamManager : SingletonBehaviour<SteamManager>
{
    internal bool SteamInitialized { get; private set; } = false;
    protected override void Awake()
    {
        base.Awake();

        if (SingletonBehaviour<SteamManager>.Instance != this)
            DestroyImmediate(this.gameObject);

        try
        {
            SteamClient.Init(588030);
            SteamInitialized = true;
        }
        catch (Exception ex)
        {
            Main.DebugLog(ex.Message);
        }

        DontDestroyOnLoad(this.gameObject);
    }

    void Update()
    {
        if(SteamInitialized)
            SteamClient.RunCallbacks();
    }

    void OnApplicationQuit()
    {
        if (SteamInitialized)
            SteamClient.Shutdown();
    }
}