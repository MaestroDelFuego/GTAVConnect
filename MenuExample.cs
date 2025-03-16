using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Numerics;
using GTA;
using NativeUI;
using System.Windows.Forms;
using System.Linq;
using GTA.Math;
using GTA.Native;

public class GTAVConnectMenu : Script
{
    private MenuPool _menuPool;
    private UIMenu mainMenu;  // Main  Menu
    private UIMenu debugMenu; // Debug Menu
    private UIMenu adminMenu; // Admin Menu


    private NetworkManager _networkManager;


    private Dictionary<string, PlayerData> _players = new Dictionary<string, PlayerData>();
    private Dictionary<string, Ped> _playerPeds = new Dictionary<string, Ped>();
    private Dictionary<string, EntityData> _entities = new Dictionary<string, EntityData>();
    private Dictionary<string, GTA.Prop> _entityProps = new Dictionary<string, GTA.Prop>();

    public GTAVConnectMenu()
    {
        try
        {
            ConsoleManager.Initialize(); // Start the console
            ConsoleManager.Log("GTAVConnect initialized.", ConsoleManager.LogLevel.INFO);

            _menuPool = new MenuPool();
            mainMenu = new UIMenu("GTAVConnect", "~b~Connect to Server");
            _menuPool.Add(mainMenu);

            debugMenu = new UIMenu("Debug Menu", "~b~Debug options");
            _menuPool.Add(debugMenu);

            adminMenu = new UIMenu("Admin Menu", "~r~Admin options");
            _menuPool.Add(adminMenu);

            _networkManager = new NetworkManager("127.0.0.1:8080", Game.Player.Name);
            _networkManager.OnPlayersUpdated += HandlePlayersUpdated;
            _networkManager.OnJoinLeave += HandleJoinLeave;

            var connectItem = new UIMenuItem("Connect");
            mainMenu.AddItem(connectItem);

            var debugItem = new UIMenuItem("Debug Menu");
            mainMenu.AddItem(debugItem);

            var adminItem = new UIMenuItem("Admin Menu");
            mainMenu.AddItem(adminItem);

            // Debug menu options
            var teleportToPosItem = new UIMenuItem("Teleport to Preset Location");
            debugMenu.AddItem(teleportToPosItem);
            var respawnItem = new UIMenuItem("Respawn Player");
            debugMenu.AddItem(respawnItem);

            // Admin menu options
            var kickPlayerItem = new UIMenuItem("Kick Player");
            adminMenu.AddItem(kickPlayerItem);
            var teleportToPlayerItem = new UIMenuItem("Teleport to Player");
            adminMenu.AddItem(teleportToPlayerItem);

            // Event handlers for menu selections
            mainMenu.OnItemSelect += (sender, item, index) =>
            {
                try
                {
                    if (item == debugItem)
                        debugMenu.Visible = !debugMenu.Visible;
                    if (item == adminItem)
                        adminMenu.Visible = !adminMenu.Visible;

                    ConsoleManager.Log($"Main menu item selected: {item.Text}", ConsoleManager.LogLevel.DEBUG);
                }
                catch (Exception ex)
                {
                    ConsoleManager.Log($"Error selecting menu item in Main Menu: {ex.Message}", ConsoleManager.LogLevel.ERROR);
                }
            };

            // Debug menu actions
            debugMenu.OnItemSelect += async (sender, item, index) =>
            {
                try
                {
                    if (item == teleportToPosItem)
                    {
                        // Example of teleporting to a fixed location
                        Game.Player.Character.Position = new Vector3(1000, 2000, 30);
                        ConsoleManager.Log("Teleported to preset location.", ConsoleManager.LogLevel.DEBUG);
                    }
                    if (item == respawnItem)
                    {
                        Game.Player.Character.Health = 0; // Simulate death to respawn
                        ConsoleManager.Log("Respawning player...", ConsoleManager.LogLevel.DEBUG);
                    }
                }
                catch (Exception ex)
                {
                    ConsoleManager.Log($"Error in Debug Menu action: {ex.Message}", ConsoleManager.LogLevel.ERROR);
                }
            };

            // Admin menu actions
            adminMenu.OnItemSelect += async (sender, item, index) =>
            {
                try
                {
                    if (item == kickPlayerItem)
                    {
                        // Add logic to kick a player
                        ConsoleManager.Log("Kicking player (admin tool).", ConsoleManager.LogLevel.DEBUG);
                        UI.Notify("Player kicked.");
                    }
                    if (item == teleportToPlayerItem)
                    {
                        // Example of teleporting to another player
                        if (_players.Count > 0)
                        {
                            var firstPlayer = _players.Values.First();
                            Game.Player.Character.Position = firstPlayer.position;
                            ConsoleManager.Log("Teleported to player " + firstPlayer.username, ConsoleManager.LogLevel.DEBUG);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ConsoleManager.Log($"Error in Admin Menu action: {ex.Message}", ConsoleManager.LogLevel.ERROR);
                }
            };

            _menuPool.RefreshIndex();

            Tick += (o, e) =>
            {
                try
                {
                    _menuPool.ProcessMenus();
                }
                catch (Exception ex)
                {
                    ConsoleManager.Log($"Error during Tick event: {ex.Message}", ConsoleManager.LogLevel.ERROR);
                }
            };

            KeyDown += (o, e) =>
            {
                try
                {
                    if (e.KeyCode == Keys.F5 && !_menuPool.IsAnyMenuOpen())
                        mainMenu.Visible = !mainMenu.Visible;
                }
                catch (Exception ex)
                {
                    ConsoleManager.Log($"Error handling key press: {ex.Message}", ConsoleManager.LogLevel.ERROR);
                }
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
            if (_networkManager != null && _networkManager.GetPlayers().Any(p => p.Value.username == Game.Player.Name))
            {
                Vector3 position = Game.Player.Character.Position;
                float rotation = Game.Player.Character.Rotation.Z;
                await _networkManager.SendPlayerUpdate(position, rotation);
                ConsoleManager.Log($"Sent player position update: {position}", ConsoleManager.LogLevel.DEBUG);
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
            ConsoleManager.Log("Received updated player list.", ConsoleManager.LogLevel.DEBUG);
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
                UI.Notify($"{username} joined the server.");
                ConsoleManager.Log($"{username} joined the server.", ConsoleManager.LogLevel.INFO);
            }
            else if (action == "left")
            {
                UI.Notify($"{username} left the server.");
                ConsoleManager.Log($"{username} left the server.", ConsoleManager.LogLevel.WARNING);

                if (_playerPeds.ContainsKey(playerId))
                {
                    ConsoleManager.Log($"Deleting ped for player {username}.", ConsoleManager.LogLevel.DEBUG);
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
                        ConsoleManager.Log($"Creating ped for player {player.Value.username}.", ConsoleManager.LogLevel.DEBUG);
                        Ped ped = World.CreatePed(PedHash.FreemodeMale01, player.Value.position);
                        _playerPeds.Add(player.Key, ped);
                    }
                    else
                    {
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

    private void HandleEntitiesUpdated(Dictionary<string, EntityData> entities)
    {
        try
        {
            foreach (KeyValuePair<string, EntityData> e in entities)
            {
                if (_entities.ContainsKey(e.Key))
                {
                    _entities[e.Key] = e.Value;
                    ConsoleManager.Log($"Updated entity {e.Key}.", ConsoleManager.LogLevel.DEBUG);
                }
                else
                {
                    _entities.Add(e.Key, e.Value);
                    ConsoleManager.Log($"Added new entity {e.Key}.", ConsoleManager.LogLevel.INFO);
                }
            }
        }
        catch (Exception ex)
        {
            ConsoleManager.Log($"Error updating entities: {ex.Message}", ConsoleManager.LogLevel.ERROR);
        }
    }

    private void HandleEntityDeleted(string entityID)
    {
        try
        {
            if (_entities.ContainsKey(entityID))
            {
                ConsoleManager.Log($"Deleting entity {entityID}.", ConsoleManager.LogLevel.WARNING);
                _entities.Remove(entityID);
            }

            if (_entityProps.ContainsKey(entityID))
            {
                ConsoleManager.Log($"Removing prop for entity {entityID}.", ConsoleManager.LogLevel.DEBUG);
                _entityProps[entityID].Delete();
                _entityProps.Remove(entityID);
            }
        }
        catch (Exception ex)
        {
            ConsoleManager.Log($"Error deleting entity: {ex.Message}", ConsoleManager.LogLevel.ERROR);
        }
    }

    private void UpdateEntityProps()
    {
        try
        {
            foreach (var entity in _entities)
            {
                if (!_entityProps.ContainsKey(entity.Key))
                {
                    Model model = new Model(entity.Value.model);
                    model.Request();
                    while (!model.IsLoaded)
                    {
                        Wait(0);
                    }
                    Prop prop = World.CreateProp(model, entity.Value.position, entity.Value.rotation, false, false);
                    _entityProps.Add(entity.Key, prop);
                    model.MarkAsNoLongerNeeded();
                    ConsoleManager.Log($"Created prop for entity {entity.Key}.", ConsoleManager.LogLevel.INFO);
                }
                else
                {
                    _entityProps[entity.Key].Position = entity.Value.position;
                    _entityProps[entity.Key].Rotation = entity.Value.rotation;
                    ConsoleManager.Log($"Updated prop for entity {entity.Key}.", ConsoleManager.LogLevel.DEBUG);
                }
            }
        }
        catch (Exception ex)
        {
            ConsoleManager.Log($"Error updating entity props: {ex.Message}", ConsoleManager.LogLevel.ERROR);
        }
    }
}
