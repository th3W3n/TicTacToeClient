using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Error; //for DisconnectReason
using System.Text; //for encoding

public enum TransportPipeline
{
    NotIdentified,
    ReliableAndInOrder,
    FireAndForget
}

//networking only: owns the socket and the connection to the server
//forwards everything else to NetworkClientProcessing
public class NetworkClient : MonoBehaviour
{
    private NetworkDriver networkDriver;
    //connection is a handle pointing to the server
    private NetworkConnection networkConnection;
    private NetworkPipeline reliableAndInOrderPipeline;
    private NetworkPipeline nonReliableNotInOrderedPipeline;

    //server's address (change to the current local IP address)
    [SerializeField] string IPAddress = "192.168.2.14";
    const ushort NetworkPort = 9001; //the port the server listens on

    private bool reportedNoConnection;

    void Start()
    {
        //singleton: only one client may exist
        if (NetworkClientProcessing.GetNetworkedClient() != null)
        {
            Debug.LogError("Another NetworkClient existed already");
            Destroy(gameObject);
            return;
        }
        NetworkClientProcessing.SetNetworkedClient(this);
        DontDestroyOnLoad(gameObject);

        Connect();
    }

    void OnDestroy()
    {
        //only the real client clears the registration
        if (NetworkClientProcessing.GetNetworkedClient() == this)
            NetworkClientProcessing.SetNetworkedClient(null);

        Disconnect();
        if (networkDriver.IsCreated) networkDriver.Dispose();
    }

    void Update()
    {
        if (!networkDriver.IsCreated) return;

        networkDriver.ScheduleUpdate().Complete();

        #region Check for client to server connection

        if (!networkConnection.IsCreated)
        {
            if (!reportedNoConnection)
            {
                Debug.Log("Client is unable to connect to server");
                reportedNoConnection = true;
            }
            return;
        }

        #endregion

        #region Manage Network Events

        while (PopNetworkEventAndCheckForData(out var networkEventType, out var streamReader, out var pipelineUsedToSendEvent))
        {
            TransportPipeline pipelineUsed = TransportPipeline.NotIdentified;
            if (pipelineUsedToSendEvent == reliableAndInOrderPipeline)
                pipelineUsed = TransportPipeline.ReliableAndInOrder;
            else if (pipelineUsedToSendEvent == nonReliableNotInOrderedPipeline)
                pipelineUsed = TransportPipeline.FireAndForget;

            switch (networkEventType)
            {
                case NetworkEvent.Type.Connect:
                    NetworkClientProcessing.ConnectionEvent();
                    break;
                case NetworkEvent.Type.Data:
                    int sizeOfDataBuffer = streamReader.ReadInt();
                    if (sizeOfDataBuffer < 0 || sizeOfDataBuffer > streamReader.Length - streamReader.GetBytesRead())
                    {
                        Debug.LogWarning("Bad message size");
                        break;
                    }
                    byte[] byteBuffer = new byte[sizeOfDataBuffer];
                    streamReader.ReadBytes(byteBuffer);
                    string msg = Encoding.Unicode.GetString(byteBuffer);
                    NetworkClientProcessing.ReceivedMessageFromServer(msg, pipelineUsed);
                    break;
                case NetworkEvent.Type.Disconnect:
                    DisconnectReason reason = (DisconnectReason)streamReader.ReadByte();
                    Debug.Log("Disconnected from server: " + reason);
                    networkConnection = default;
                    NetworkClientProcessing.DisconnectionEvent();
                    break;
            }
        }

        #endregion
    }

    private bool PopNetworkEventAndCheckForData(out NetworkEvent.Type networkEventType, out DataStreamReader streamReader, out NetworkPipeline pipelineUsedToSendEvent)
    {
        networkEventType = networkConnection.PopEvent(networkDriver, out streamReader, out pipelineUsedToSendEvent);

        if (networkEventType == NetworkEvent.Type.Empty)
            return false;
        return true;
    }

    public void SendMessageToServer(string msg, TransportPipeline pipeline)
    {
        if (!IsConnected())
        {
            Debug.LogWarning("SendMessageToServer: not connected to server");
            return;
        }

        NetworkPipeline networkPipeline = pipeline == TransportPipeline.FireAndForget
            ? nonReliableNotInOrderedPipeline
            : reliableAndInOrderPipeline;

        int status = networkDriver.BeginSend(networkPipeline, networkConnection, out var streamWriter);
        if (status != 0)
        {
            Debug.LogWarning("BeginSend failed: " + status);
            return;
        }

        byte[] msgAsByteArray = Encoding.Unicode.GetBytes(msg);
        streamWriter.WriteInt(msgAsByteArray.Length);
        streamWriter.WriteBytes(msgAsByteArray);
        int sent = networkDriver.EndSend(streamWriter);
        if (sent < 0)
            Debug.LogWarning("EndSend failed: " + sent);
    }

    public void Connect()
    {
        if (networkConnection.IsCreated)
        {
            Debug.LogWarning("Connect: already connected or connecting");
            return;
        }

        //create the driver only once;
        //calling Connect() again reuses it instead of leaking a new one
        if (!networkDriver.IsCreated)
        {
            networkDriver = NetworkDriver.Create();
            reliableAndInOrderPipeline = networkDriver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));
            nonReliableNotInOrderedPipeline = networkDriver.CreatePipeline(typeof(FragmentationPipelineStage));
        }

        if (!NetworkEndpoint.TryParse(IPAddress, NetworkPort, out var endpoint, NetworkFamily.Ipv4))
        {
            Debug.LogError("Invalid server IP: " + IPAddress);
            return;
        }
        networkConnection = networkDriver.Connect(endpoint);
        reportedNoConnection = false;
    }

    public void Disconnect()
    {
        if (!networkConnection.IsCreated) return;

        networkConnection.Disconnect(networkDriver);
        //send the disconnect packet now
        //so the server sees ClosedByRemote instead of a Timeout later
        networkDriver.ScheduleFlushSend().Complete();
        networkConnection = default;
    }

    public bool IsConnected()
    {
        return networkConnection.IsCreated
            && networkConnection.GetState(networkDriver) == NetworkConnection.State.Connected;
    }
}