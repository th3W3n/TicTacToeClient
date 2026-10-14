using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum AccountActions { SignIn, SignUp }

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameLogic gameLogic;

    [SerializeField] private TMP_Text TitleTxt;
    [SerializeField] private GameObject panelStart;
    [SerializeField] private GameObject panelAction;
    [SerializeField] private GameObject panelFeedback;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField passInput;
    [SerializeField] private TMP_InputField reEnterInput;
    [SerializeField] private GameObject reEnterInputGo;
    [SerializeField] private TMP_Text errorTxt;
    [SerializeField] private TMP_Text feedbackTxt;

    private AccountActions accountActions;

    const string TitleTxtStart = "Choose Your Action";
    const string TitleTxtSignIn = "Sign-In an Existing Account";
    const string TitleTxtSignUp = "Sign-Up a New Account";
    const string TitleTxtFeedback = "Congratulations!";
    const string ErrorTxt_01 = "Name cannot be empty or contain these symbols (, *)";
    const string ErrorTxt_02 = "Password cannot be empty or contain comma (,)";
    const string ErrorTxt_03 = "Re-entered password does not match";

    private void Start() => StartScreen();


    public void StartScreen() => SwitchPanel(TitleTxtStart, true, false, false);
    public void SignInScreen() => SignInSignUpScreen(AccountActions.SignIn);
    public void SignUpScreen() => SignInSignUpScreen(AccountActions.SignUp);
    private void SignInSignUpScreen(AccountActions _accountActions)
    {
        accountActions = _accountActions;
        switch (accountActions)
        {
            case AccountActions.SignIn:
                SwitchPanel(TitleTxtSignIn, false, true, false);
                reEnterInputGo.SetActive(false);
                break;
            case AccountActions.SignUp:
                SwitchPanel(TitleTxtSignUp, false, true, false);
                reEnterInputGo.SetActive(true);
                break;
        }
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate
        ((RectTransform)panelAction.transform);
        errorTxt.text = nameInput.text = passInput.text = reEnterInput.text = "";
    }
    private void SwitchPanel(string _title, bool _showPnlStart, bool _showPnlAction, bool _showPnlFbck)
    {
        TitleTxt.text = _title;
        panelStart.SetActive(_showPnlStart);
        panelAction.SetActive(_showPnlAction);
        panelFeedback.SetActive(_showPnlFbck);
    }

    public void SubmitInfo()
    {
        string name = nameInput.text;
        string pass = passInput.text;

        //filter by client first
        if (!NetworkProtocol.IsValidField(name) || name.Contains('*'))
        {
            errorTxt.text = ErrorTxt_01;
            return;
        }
        if (!NetworkProtocol.IsValidField(pass))
        {
            errorTxt.text = ErrorTxt_02;
            return;
        }

        switch (accountActions)
        {
            case AccountActions.SignIn:
                gameLogic.SubmitSignIn(name, pass);
                break;
            case AccountActions.SignUp:
                if (reEnterInput.text != pass)
                {
                    errorTxt.text = ErrorTxt_03;
                    return;
                }
                gameLogic.SubmitSignUp(name, pass);
                break;
        }
    }

    public void ClearErrorText() => errorTxt.text = "";

    public void ShowError(string _message) => errorTxt.text = _message;

    public void ShowFeedback(string _message)
    {
        SwitchPanel(TitleTxtFeedback, false, false, true);
        feedbackTxt.text = _message;
    }
}