using UnityEngine;
// using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using System.Text;

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
        networkDriver = NetworkDriver.Create();
        reliableAndInOrderPipeline = networkDriver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));
        nonReliableNotInOrderedPipeline = networkDriver.CreatePipeline(typeof(FragmentationPipelineStage));
        if (!NetworkEndpoint.TryParse(IPAddress, NetworkPort, out var endpoint, NetworkFamily.Ipv4))
        {
            Debug.Log("Invalid server IP: " + IPAddress);
            return;
        }
        networkConnection = networkDriver.Connect(endpoint);
    }

    public void OnDestroy()
    {
        if (networkConnection.IsCreated) networkConnection.Disconnect(networkDriver);
        networkConnection = default;
        if (networkDriver.IsCreated) networkDriver.Dispose();
    }

    void Update()
    {
        #region Check Input and Send Msg

        if (Input.GetKeyDown(KeyCode.A))
            SendMessageToServer("Hello server's world, sincerely your network client");

        #endregion

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
            if (pipelineUsedToSendEvent == reliableAndInOrderPipeline)
                Debug.Log("Network event from: reliableAndInOrderPipeline");
            else if (pipelineUsedToSendEvent == nonReliableNotInOrderedPipeline)
                Debug.Log("Network event from: nonReliableNotInOrderedPipeline");

            switch (networkEventType)
            {
                case NetworkEvent.Type.Connect:
                    Debug.Log("We are now connected to the server");
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
                    ProcessReceivedMsg(msg);
                    break;
                case NetworkEvent.Type.Disconnect:
                    Debug.Log("Client has disconnected from server:" + (Unity.Networking.Transport.Error.DisconnectReason)streamReader.ReadByte());
                    networkConnection = default;
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

    private void ProcessReceivedMsg(string msg)
    {
        Debug.Log("Msg received = " + msg);
    }

    public void SendMessageToServer(string msg)
    {
        if (!networkConnection.IsCreated) return;
        int status = networkDriver.BeginSend(reliableAndInOrderPipeline, networkConnection, out var streamWriter);
        if (status != 0)
        {
            Debug.Log("BeginSend failed: " + status);
            return;
        }

        byte[] msgAsByteArray = Encoding.Unicode.GetBytes(msg);
        streamWriter.WriteInt(msgAsByteArray.Length);
        streamWriter.WriteBytes(msgAsByteArray);
        int sent = networkDriver.EndSend(streamWriter);
        if (sent < 0)
            Debug.LogWarning("EndSend failed: " + sent);
    }

}