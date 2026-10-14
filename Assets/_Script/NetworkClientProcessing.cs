using UnityEngine;

//routes network events: parses server messages and calls GameLogic
//knows nothing about sockets (NetworkClient) or UI/game state (GameLogic)
static public class NetworkClientProcessing
{
    #region Send and Receive Data Functions

    static public void ReceivedMessageFromServer(string msg, TransportPipeline pipeline)
    {
        Debug.Log("Network msg received =  " + msg + ", from pipeline = " + pipeline);

        //split once: signifier + everything after it
        string[] csv = msg.Split(NetworkProtocol.Separator, 2);
        if (!int.TryParse(csv[0], out int signifierValue))
        {
            Debug.LogWarning("Message without a valid signifier: " + msg);
            return;
        }
        string payload = csv.Length > 1 ? csv[1] : null;
        if (gameLogic == null)
        {
            Debug.LogWarning("No GameLogic registered");
            return;
        }

        switch ((ServerToClientSignifier)signifierValue)
        {
            case ServerToClientSignifier.SignUpSucceeded:
                gameLogic.OnSignUpSucceeded();
                break;
            case ServerToClientSignifier.SignUpFailed:
                gameLogic.OnSignUpFailed(payload ?? "Unknown error");
                break;
            case ServerToClientSignifier.SignInSucceeded:
                gameLogic.OnSignInSucceeded();
                break;
            case ServerToClientSignifier.SignInFailed:
                gameLogic.OnSignInFailed(payload ?? "Unknown error");
                break;
            default:
                Debug.LogWarning("Unknown signifier " + signifierValue);
                break;
        }
    }

    static public void SendMessageToServer(string msg, TransportPipeline pipeline)
    {
        if (networkClient == null)
        {
            Debug.LogWarning("SendMessageToServer: no NetworkClient registered");
            return;
        }
        networkClient.SendMessageToServer(msg, pipeline);
    }

    static public void SendToServer(ClientToServerSignifier signifier, string data = null, TransportPipeline pipeline = TransportPipeline.ReliableAndInOrder)
    {
        string msg = ((int)signifier).ToString();
        if (data != null)
            msg += NetworkProtocol.Separator + data;
        SendMessageToServer(msg, pipeline);
    }

    #endregion

    #region Connection Related Functions and Events

    static public void ConnectionEvent()
    {
        Debug.Log("Network Connection Event!");
    }
    static public void DisconnectionEvent()
    {
        Debug.Log("Network Disconnection Event!");
    }
    static public bool IsConnectedToServer()
    {
        return networkClient != null && networkClient.IsConnected();
    }
    static public void ConnectToServer()
    {
        if (networkClient != null) networkClient.Connect();
    }
    static public void DisconnectFromServer()
    {
        if (networkClient != null) networkClient.Disconnect();
    }

    #endregion

    #region Setup

    static NetworkClient networkClient;
    static GameLogic gameLogic;

    static public void SetNetworkedClient(NetworkClient _client)
    {
        networkClient = _client;
    }
    static public NetworkClient GetNetworkedClient()
    {
        return networkClient;
    }
    static public void SetGameLogic(GameLogic _logic)
    {
        gameLogic = _logic;
    }

    #endregion

}