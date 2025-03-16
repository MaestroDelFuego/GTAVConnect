using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Numerics;
using GTA.Math;

public class PlayerData
{
    public Vector3 position;
    public float rotation;
    public string username;
}

public class EntityData
{
    public string id;
    public Vector3 position;
    public Vector3 rotation;
    public int model;
}

public class NetworkManager
{
    private ClientWebSocket _webSocket;
    private string _serverUri;
    private string _playerName;
    private Dictionary<string, PlayerData> _players = new Dictionary<string, PlayerData>();
    private string _myPlayerId;

    public event Action<Dictionary<string, PlayerData>> OnPlayersUpdated;
    public event Action<string, string, string> OnJoinLeave;
    public event Action<Dictionary<string, EntityData>> OnEntitiesUpdated;
    public event Action<string> OnEntityDeleted;

    public NetworkManager(string serverUri, string playerName)
    {
        _serverUri = serverUri;
        _playerName = playerName;
        _webSocket = new ClientWebSocket();
    }

    public async Task Connect()
    {
        try
        {
            await _webSocket.ConnectAsync(new Uri(_serverUri), CancellationToken.None);
            Console.WriteLine("Connected to server.");
            await SendUsername();
            _ = ReceiveMessages();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting: {ex.Message}");
        }
    }

    private async Task SendUsername()
    {
        var message = JsonConvert.SerializeObject(new { type = "username", username = _playerName });
        await SendMessage(message);
    }

    public async Task SendPlayerUpdate(Vector3 position, float rotation)
    {
        if (_myPlayerId == null) return;

        var message = JsonConvert.SerializeObject(new { type = "update", playerID = _myPlayerId, data = new { position = new { x = position.X, y = position.Y, z = position.Z }, rotation = rotation } });
        await SendMessage(message);
    }

    public async Task SendCreateEntity(EntityData entity)
    {
        var message = JsonConvert.SerializeObject(new { type = "create_entity", data = entity });
        await SendMessage(message);
    }

    public async Task SendUpdateEntity(string entityID, EntityData entity)
    {
        var message = JsonConvert.SerializeObject(new { type = "update_entity", entityID = entityID, data = entity });
        await SendMessage(message);
    }

    public async Task SendDeleteEntity(string entityID)
    {
        var message = JsonConvert.SerializeObject(new { type = "delete_entity", entityID = entityID });
        await SendMessage(message);
    }

    private async Task SendMessage(string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task ReceiveMessages()
    {
        var buffer = new byte[1024 * 4];
        while (_webSocket.State == WebSocketState.Open)
        {
            try
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    ProcessMessage(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving messages: {ex.Message}");
                break;
            }
        }
    }

    private void ProcessMessage(string message)
    {
        try
        {
            var data = JsonConvert.DeserializeObject<dynamic>(message);

            if (data.type == "welcome")
            {
                if (data.players != null)
                {
                    _players = JsonConvert.DeserializeObject<Dictionary<string, PlayerData>>(JsonConvert.SerializeObject(data.players));
                    foreach (KeyValuePair<string, PlayerData> player in _players)
                    {
                        if (player.Value.username == _playerName)
                        {
                            _myPlayerId = player.Key;
                            return;
                        }
                    }
                }
                if (data.entities != null)
                {
                    OnEntitiesUpdated?.Invoke(JsonConvert.DeserializeObject<Dictionary<string, EntityData>>(JsonConvert.SerializeObject(data.entities)));
                }
            }
            else if (data.type == "sync")
            {
                _players = JsonConvert.DeserializeObject<Dictionary<string, PlayerData>>(JsonConvert.SerializeObject(data.players));
                OnPlayersUpdated?.Invoke(_players);
            }
            else if (data.type == "join_leave")
            {
                string playerId = data.playerID;
                string action = data.action;
                string username = data.username;
                OnJoinLeave?.Invoke(playerId, action, username);
            }
            else if (data.type == "entity_update")
            {
                EntityData entity = JsonConvert.DeserializeObject<EntityData>(JsonConvert.SerializeObject(data.entity));
                Dictionary<string, EntityData> entities = new Dictionary<string, EntityData>();
                entities.Add(entity.id, entity);
                OnEntitiesUpdated?.Invoke(entities);
            }
            else if (data.type == "entity_delete")
            {
                string entityID = data.entityID;
                OnEntityDeleted?.Invoke(entityID);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
        }
    }

    public async Task Disconnect()
    {
        if (_webSocket.State == WebSocketState.Open)
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection", CancellationToken.None);
            Console.WriteLine("Disconnected from server.");
        }
    }

    public Dictionary<string, PlayerData> GetPlayers()
    {
        return _players;
    }
}