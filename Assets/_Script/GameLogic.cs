using UnityEngine;

public class GameLogic : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    private string pendingName; //pending till the server answers
    public string SignedInName { get; private set; }

    void Start()
    {
        NetworkClientProcessing.SetGameLogic(this);
    }
    void OnDestroy()
    {
        NetworkClientProcessing.SetGameLogic(null);
    }

    #region Requests from the UI

    public void SubmitSignUp(string name, string pass) => SendAccountRequest(ClientToServerSignifier.SignUp, name, pass);
    public void SubmitSignIn(string name, string pass) => SendAccountRequest(ClientToServerSignifier.SignIn, name, pass);

    private void SendAccountRequest(ClientToServerSignifier signifier, string name, string pass)
    {
        if (!NetworkClientProcessing.IsConnectedToServer())
        {
            uiManager.ShowError("Not connected to server");
            return;
        }
        pendingName = name;
        NetworkClientProcessing.SendToServer(signifier, name + NetworkProtocol.Separator + pass);
    }

    #endregion

    #region Replies from the server

    public void OnSignUpSucceeded() => uiManager.ShowFeedback("Account \"" + pendingName + "\" created. You can now sign in.");
    public void OnSignUpFailed(string reason) => uiManager.ShowError(reason);

    public void OnSignInSucceeded()
    {
        SignedInName = pendingName;
        uiManager.ShowFeedback("Signed in as " + SignedInName);
    }
    public void OnSignInFailed(string reason) => uiManager.ShowError(reason);

    #endregion
}