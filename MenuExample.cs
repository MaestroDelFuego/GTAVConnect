using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;

public class GTAVConnectMenu : Script
{
    private MenuPool _menuPool;
    private UIMenu mainMenu; // Main Menu
    private NetworkManager _networkManager;

    private Dictionary<string, PlayerData> _players = new Dictionary<string, PlayerData>();
    private Dictionary<string, Ped> _playerPeds = new Dictionary<string, Ped>();

    public GTAVConnectMenu()
    {
        try
        {
            _menuPool = new MenuPool();
            mainMenu = new UIMenu("GTAVConnect", "~b~Connect to Server");
            _menuPool.Add(mainMenu);

            // Initialize the NetworkManager for player connection and communication
            _networkManager = new NetworkManager("ws://located-nuke.gl.at.ply.gg:24089", Game.Player.Name);
            _networkManager.OnPlayersUpdated += HandlePlayersUpdated;
            _networkManager.OnJoinLeave += HandleJoinLeave;

            // "Connect" button on the main menu
            var connectItem = new UIMenuItem("Connect");
            mainMenu.AddItem(connectItem);

            // Event handler for menu selection
            mainMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == connectItem)  // Connect button press
                {
                    _networkManager.Connect();
                }
            };

            _menuPool.RefreshIndex();

            Tick += (o, e) =>
            {
                _menuPool.ProcessMenus();
                SendPlayerPositionToServer();
                UpdatePlayerPositions();
            };
        }
        catch (Exception ex)
        {
            ConsoleManager.Log($"Error initializing GTAVConnectMenu: {ex.Message}", ConsoleManager.LogLevel.ERROR);
        }
    }

    private async void SendPlayerPositionToServer()
    {
        try
        {
            // Send player position and rotation to the server if the network manager is set up
            if (_networkManager != null && _networkManager.GetPlayers().Any(p => p.Value.username == Game.Player.Name))
            {
                Vector3 position = Game.Player.Character.Position;
                float rotation = Game.Player.Character.Rotation.Z;
                await _networkManager.SendPlayerUpdate(position, rotation);
            }
        }
        catch (Exception ex)
        {
            ConsoleManager.Log($"Error sending player position: {ex.Message}", ConsoleManager.LogLevel.ERROR);
        }
    }

    private void HandlePlayersUpdated(Dictionary<string, PlayerData> players)
    {
        try
        {
            _players = players;
            UpdatePlayerPositions();
        }
        catch (Exception ex)
        {
            ConsoleManager.Log($"Error updating players: {ex.Message}", ConsoleManager.LogLevel.ERROR);
        }
    }

    private void HandleJoinLeave(string playerId, string action, string username)
    {
        try
        {
            if (action == "joined")
            {
                // Player joined, create their ped (character in the game)
                Ped ped = World.CreatePed(PedHash.FreemodeMale01, _players[playerId].position);
                if (ped.Exists())
                {
                    _playerPeds.Add(playerId, ped);
                }
            }
            else if (action == "left")
            {
                // Player left, delete their ped
                if (_playerPeds.ContainsKey(playerId))
                {
                    _playerPeds[playerId].Delete();
                    _playerPeds.Remove(playerId);
                }
            }
        }
        catch (Exception ex)
        {
            ConsoleManager.Log($"Error handling join/leave: {ex.Message}", ConsoleManager.LogLevel.ERROR);
        }
    }

    private void UpdatePlayerPositions()
    {
        try
        {
            foreach (var player in _players)
            {
                if (player.Value.username != Game.Player.Name)
                {
                    if (!_playerPeds.ContainsKey(player.Key))
                    {
                        // Create ped for the new player
                        Ped ped = World.CreatePed(PedHash.FreemodeMale01, player.Value.position);
                        if (ped.Exists())
                        {
                            _playerPeds.Add(player.Key, ped);
                        }
                    }
                    else
                    {
                        // Update the position and rotation of the existing ped
                        _playerPeds[player.Key].Position = player.Value.position;
                        _playerPeds[player.Key].Rotation = new Vector3(0, 0, player.Value.rotation);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ConsoleManager.Log($"Error updating player positions: {ex.Message}", ConsoleManager.LogLevel.ERROR);
        }
    }
}
