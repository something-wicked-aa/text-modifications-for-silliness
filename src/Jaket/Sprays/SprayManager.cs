namespace Jaket.Sprays;

using System.Collections.Generic;
using System.IO;
using UnityEngine;

using Jaket.Assets;
using Jaket.Net;
using Jaket.UI.Elements;

/// <summary> Saves sprays of players and loads sprays of the local player. </summary>
public class SprayManager
{
    /// <summary> Spray currently selected by the local player. </summary>
    public static SprayFile CurrentSpray;
    /// <summary> Other sprays located in the sprays folder. </summary>
    public static List<SprayFile> Loaded = new();
    /// <summary> Whether the current spray has been uploaded. </summary>
    public static bool Uploaded;

    /// <summary> Sprays spawned by players. </summary>
    public static Dictionary<ulong, Spray> Sprays = new();
    /// <summary> Cached sprays of other players. </summary>
    public static Dictionary<ulong, SprayFile> Cache = new();

    /// <summary> Subscribes to various events to synchronize sprays or clear cache. </summary>
    public static void Load()
    {
        LoadFileSprays();

        // clear the cache in offline game & upload the current spray if it was changed
        Events.OnLoaded += () =>
        {
            if (LobbyController.Offline) Cache.Clear();
            else SprayDistributor.UploadLocal();
        };
        Events.OnLobbyEntered += () =>
        {
            Uploaded = LobbyController.IsOwner;
            Cache.Clear();
            Cache.Add(Networking.LocalPlayer.Id, CurrentSpray);
        };
        // process requests every second
        Events.EverySecond += SprayDistributor.ProcessRequests;
    }

    /// <summary> Loads sprays from the sprays folder. </summary>
    public static void LoadFileSprays()
    {
        Loaded.Clear();

        var folder = Path.Combine(Path.GetDirectoryName(Plugin.Instance.Info.Location), "sprays");
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        else foreach (var file in Directory.EnumerateFiles(folder))
                if (SprayFile.SUPPORTED.Contains(Path.GetExtension(file))) Loaded.Add(new(file));

        Log.Debug($"Loaded {Loaded.Count} sprays: {string.Join(", ", Loaded)}");

    }

    /// <summary> Sets the current spray. </summary>
    public static void SetSpray(SprayFile spray)
    {
        CurrentSpray = spray;
        Uploaded = false;
    }

    /// <summary> Spawns someone's spray in the given position. </summary>
    public static Spray Spawn(ulong owner, Vector3 position, Vector3 direction)
    {
        if (Sprays.TryGetValue(owner, out var spray))
        {
            spray.Lifetime = 19.5f;
            Sprays.Remove(owner);
        }
        if (!Cache.ContainsKey(owner))
        {
            if (owner == Networking.LocalPlayer.Id) // seems like the player is in offline game
                Cache.Add(owner, CurrentSpray);
            else
                SprayDistributor.Request(owner);
        }

        spray = Spray.Spawn(owner, position, direction);
        Sprays.Add(owner, spray);

        return spray;
    }

    /// <summary> Spawns the local player's spray in the given position. </summary>
    public static Spray Spawn(Vector3 position, Vector3 direction)
    {
        if (CurrentSpray == null)
        {
            Bundle.Hud("sprays.empty"); // You haven't chosen a spray. Please, choose on in settings.
            return null;
        }
        return Spawn(Networking.LocalPlayer.Id, position, direction);
    }
}
