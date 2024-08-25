using UE = UnityEngine;
using BepConfig = BepInEx.Configuration;

namespace DDoor.Randomizer.Multiworld;

internal class MWSettings
{
    private BepConfig.ConfigEntry<string> ServerAddr;
    private BepConfig.ConfigEntry<string> Nickname;
    private BepConfig.ConfigEntry<string> RoomName;

    private const string MWGroup = "Multiworld";

    public MWSettings(BepConfig.ConfigFile config)
    {
        ServerAddr = config.Bind(MWGroup, "Server Address", "127.0.0.1", "The address or address:port of the multiworld server");
        Nickname = config.Bind(MWGroup, "Nickname", "", "The nickname other players will see");
        RoomName = config.Bind(MWGroup, "RoomName", "", "The name of the room to join");

        createButton(config, Ready, MWGroup, "Ready", "Connect to the server and join a room");
        createButton(config, Disconnect, MWGroup, "Disconnect", "Disconnect from the server");
        createButton(config, Start, MWGroup, "Start MW", "Begin shuffling items between worlds");
        createButton(config, Eject, MWGroup, "Eject", "Send out all items belonging to other players");
    }

    private void Ready()
    {
        MWConnection.Start();
        MWConnection.Current.Connect(ServerAddr.Value, Nickname.Value, RoomName.Value);
    }

    private void Disconnect()
    {
        MWConnection.Terminate();
        MainThread.Invoke(mt => mt.ShowMWStatus(""));
    }

    private void Start()
    {
        if (MWConnection.Current != null)
        {
            MWConnection.Current.StartRandomization();
        }
    }

    private void Eject()
    {
        MainThread.Invoke(mt => mt.EjectMW());
    }

    // 💙 Schy's button-drawing code mostly copy-pasted from here:
    // https://github.com/Jarlyk/Haiku.ModdingApi/blob/058f3f1fe4a446663fa01a762425c07d746b0297/Haiku.CoreModdingApi/ConfigManagerUtil.cs
    // under this license:
    /* MIT License

    Copyright (c) 2022 Jarlyk

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software. */
    private static void buttonDrawer(System.Action method, string name, string description, bool expandWidth = false)
    {
        if (UE.GUILayout.Button(new UE.GUIContent(name, description), UE.GUI.skin.button, UE.GUILayout.ExpandWidth(expandWidth)))
        {
            method();
        }
    }

    private static void createButton(BepConfig.ConfigFile config, System.Action method, string section, string btnName, string description, ConfigurationManagerAttributes? ConfigAttributes = null)
    {
        if (ConfigAttributes == null)
        {
            ConfigAttributes = new ConfigurationManagerAttributes();
        }
        ConfigAttributes.ReadOnly = true;
        ConfigAttributes.HideDefaultButton = true;
        ConfigAttributes.CustomDrawer = x => buttonDrawer(method, btnName, description);
        config.Bind(section, btnName, "", new BepConfig.ConfigDescription(description, null,
            ConfigAttributes));
    }

    private class ConfigurationManagerAttributes
    {
        public bool ReadOnly;
        public bool HideDefaultButton;
        public System.Action<BepConfig.ConfigEntryBase>? CustomDrawer;
    }
}
