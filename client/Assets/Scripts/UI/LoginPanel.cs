using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TexasHoldem.Network;

namespace TexasHoldem.UI
{
    public class LoginPanel : MonoBehaviour
    {
        [Header("Login Tab")]
        [SerializeField] private TMP_InputField loginUsernameInput;
        [SerializeField] private TMP_InputField loginPasswordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button guestLoginButton;
        [SerializeField] private Button switchToRegisterButton;

        [Header("Register Tab")]
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private TMP_InputField registerUsernameInput;
        [SerializeField] private TMP_InputField registerEmailInput;
        [SerializeField] private TMP_InputField registerPasswordInput;
        [SerializeField] private TMP_InputField registerConfirmPasswordInput;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button switchToLoginButton;

        [Header("UI Elements")]
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private GameObject loadingIndicator;
        [SerializeField] private Toggle rememberMeToggle;

        [Header("Settings")]
        [SerializeField] private string serverUrl = "http://localhost:8080";

        public event Action<string, UserData> OnLoginSuccess;
        public event Action<string> OnLoginFailed;

        private bool _isLoginMode = true;

        private void Start()
        {
            SetupButtons();
            LoadSavedCredentials();
            ShowLoginMode();
        }

        private void SetupButtons()
        {
            loginButton?.onClick.AddListener(() => _ = Login());
            guestLoginButton?.onClick.AddListener(() => _ = GuestLogin());
            registerButton?.onClick.AddListener(() => _ = Register());
            
            switchToRegisterButton?.onClick.AddListener(ShowRegisterMode);
            switchToLoginButton?.onClick.AddListener(ShowLoginMode);

            loginPasswordInput?.onSubmit.AddListener(_ => _ = Login());
            registerConfirmPasswordInput?.onSubmit.AddListener(_ => _ = Register());
        }

        private void ShowLoginMode()
        {
            _isLoginMode = true;
            registerPanel?.SetActive(false);
            ClearError();
        }

        private void ShowRegisterMode()
        {
            _isLoginMode = false;
            registerPanel?.SetActive(true);
            ClearError();
        }

        private async Task Login()
        {
            string username = loginUsernameInput?.text?.Trim();
            string password = loginPasswordInput?.text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("请输入用户名和密码");
                return;
            }

            SetLoading(true);
            ClearError();

            try
            {
                var request = new LoginRequest
                {
                    username = username,
                    password = password
                };

                var response = await HttpClient.PostAsync<AuthResponse>($"{serverUrl}/api/auth/login", request);

                if (response != null && !string.IsNullOrEmpty(response.token))
                {
                    SaveCredentials(username, password);
                    OnLoginSuccess?.Invoke(response.token, response.user);
                }
                else
                {
                    ShowError("登录失败");
                    OnLoginFailed?.Invoke("Login failed");
                }
            }
            catch (Exception ex)
            {
                ShowError($"登录失败: {ex.Message}");
                OnLoginFailed?.Invoke(ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async Task GuestLogin()
        {
            SetLoading(true);
            ClearError();

            try
            {
                var response = await HttpClient.PostAsync<AuthResponse>($"{serverUrl}/api/auth/guest", null);

                if (response != null && !string.IsNullOrEmpty(response.token))
                {
                    OnLoginSuccess?.Invoke(response.token, response.user);
                }
                else
                {
                    ShowError("游客登录失败");
                }
            }
            catch (Exception ex)
            {
                ShowError($"游客登录失败: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async Task Register()
        {
            string username = registerUsernameInput?.text?.Trim();
            string email = registerEmailInput?.text?.Trim();
            string password = registerPasswordInput?.text;
            string confirmPassword = registerConfirmPasswordInput?.text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || 
                string.IsNullOrEmpty(password))
            {
                ShowError("请填写所有必填项");
                return;
            }

            if (password != confirmPassword)
            {
                ShowError("两次输入的密码不一致");
                return;
            }

            if (password.Length < 6)
            {
                ShowError("密码至少6个字符");
                return;
            }

            if (!IsValidEmail(email))
            {
                ShowError("请输入有效的邮箱地址");
                return;
            }

            SetLoading(true);
            ClearError();

            try
            {
                var request = new RegisterRequest
                {
                    username = username,
                    email = email,
                    password = password
                };

                var response = await HttpClient.PostAsync<AuthResponse>($"{serverUrl}/api/auth/register", request);

                if (response != null && !string.IsNullOrEmpty(response.token))
                {
                    SaveCredentials(username, password);
                    OnLoginSuccess?.Invoke(response.token, response.user);
                }
                else
                {
                    ShowError("注册失败");
                }
            }
            catch (Exception ex)
            {
                ShowError($"注册失败: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private bool IsValidEmail(string email)
        {
            return email.Contains("@") && email.Contains(".");
        }

        private void ShowError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
                errorText.gameObject.SetActive(true);
            }
        }

        private void ClearError()
        {
            if (errorText != null)
            {
                errorText.text = "";
                errorText.gameObject.SetActive(false);
            }
        }

        private void SetLoading(bool loading)
        {
            loadingIndicator?.SetActive(loading);
            
            if (loginButton != null) loginButton.interactable = !loading;
            if (guestLoginButton != null) guestLoginButton.interactable = !loading;
            if (registerButton != null) registerButton.interactable = !loading;
        }

        private void SaveCredentials(string username, string password)
        {
            if (rememberMeToggle != null && rememberMeToggle.isOn)
            {
                PlayerPrefs.SetString("SavedUsername", username);
                PlayerPrefs.SetString("SavedPassword", password);
                PlayerPrefs.Save();
            }
        }

        private void LoadSavedCredentials()
        {
            if (PlayerPrefs.HasKey("SavedUsername"))
            {
                if (loginUsernameInput != null)
                    loginUsernameInput.text = PlayerPrefs.GetString("SavedUsername");
                if (loginPasswordInput != null)
                    loginPasswordInput.text = PlayerPrefs.GetString("SavedPassword");
                if (rememberMeToggle != null)
                    rememberMeToggle.isOn = true;
            }
        }

        public void ClearSavedCredentials()
        {
            PlayerPrefs.DeleteKey("SavedUsername");
            PlayerPrefs.DeleteKey("SavedPassword");
            PlayerPrefs.Save();
        }
    }

    [Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
    }

    [Serializable]
    public class RegisterRequest
    {
        public string username;
        public string email;
        public string password;
    }

    [Serializable]
    public class AuthResponse
    {
        public string token;
        public long expiresAt;
        public UserData user;
    }

    [Serializable]
    public class UserData
    {
        public string id;
        public string username;
        public string nickname;
        public string avatar;
        public int level;
        public long exp;
        public long chips;
        public long diamonds;
        public int vipLevel;
    }
}
