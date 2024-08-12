using System;
using Editor.Resources.Screens.Export;
using Editor.Resources.Screens.Setting;
using Editor.Scripts.Manager;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.Resources
{
    public class EdenStudio : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;
        private VisualElement container;
        [SerializeField]
        private VisualTreeAsset loginVisualTreeAsset = default;
        [SerializeField]
        private VectorImage m_LoginLogo = default;
        private VisualElement loginModal;

        public static StyleSheet style;
        public static StyleSheet tailwind;
        private static Button prefab_list_button;
        private static Button settings_button;
        private static Label prefab_list_label;
        private static Label settings_label;
        private static EnumField languageDropdown;

        private bool isLoggingIn = false;
        
        [MenuItem("Window/EdenStudio")]
        public static void ShowExample()
        {
            var wnd = GetWindow<EdenStudio>();
            wnd.titleContent = new GUIContent("EdenStudio");
            wnd.minSize = new Vector2(1080, 640);
        }

        public void CreateGUI()
        {
            style = UnityEngine.Resources.Load<StyleSheet>("style");
            tailwind = UnityEngine.Resources.Load<StyleSheet>("tailwind");

            var root = rootVisualElement;
            root.styleSheets.Add(style);
            root.styleSheets.Add(tailwind);

            // Instantiate UXML
            VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
            root.Add(labelFromUXML);
            container = labelFromUXML.Q("container");
            prefab_list_button = labelFromUXML.Q<Button>("prefab_list_button");
            prefab_list_label = labelFromUXML.Q<Label>("prefab_list_label");
            settings_button = labelFromUXML.Q<Button>("settings_button");
            settings_label = labelFromUXML.Q<Label>("settings_label");
            
            LocalizationManager.OnLanguageChanged += () =>
            {
                prefab_list_label.text = LocalizationManager.GetLocalizedValue("prefab_list");
                settings_label.text = LocalizationManager.GetLocalizedValue("settings");
            };
            
            prefab_list_label.text = LocalizationManager.GetLocalizedValue("prefab_list");
            settings_label.text = LocalizationManager.GetLocalizedValue("settings");
            
            prefab_list_button.clicked += OnExportClicked;
            settings_button.clicked += OnSettingsClicked;
            
            Export.Show(container, OnExportVrmClicked);
            ShowLoginModal();
        }

        private void OnLanguageChanged(ChangeEvent<Enum> evt)
        {
            Debug.Log($"Language changed to {evt.newValue}");
            var language = (Language)evt.newValue;
            LocalizationManager.SetLanguage(language);
        }

        private void OnExportVrmClicked()
        {
            ExportVrm.Show(container, OnBackClicked);
        }
        
        private void OnExportClicked()
        {
            Export.Show(container, OnExportVrmClicked);
        }
        
        private void OnSettingsClicked()
        {
            Settings.Show(container, OnLogoutClicked);
        }

        private void OnBackClicked()
        {
            Export.Show(container, OnExportVrmClicked);
        }
        
        private void OnLogoutClicked()
        {
            Debug.Log("Logout clicked");
            AuthManager.Logout(() =>
            {
                Debug.Log("Logout success");
                ShowLoginModal();
            });
        }
        
        private void ShowLoginModal()
        {
            AuthManager.Initialize();
            // 로그인 되어 있는지 확인
            if (AuthManager.IsAuthenticated)
            {
                return;
            }
            if (loginVisualTreeAsset == null)
            {
                loginVisualTreeAsset = UnityEngine.Resources.Load<VisualTreeAsset>("Components/LoginModal");
            }
            
            if (m_LoginLogo == null)
            {
                m_LoginLogo = UnityEngine.Resources.Load<VectorImage>("Images/logo");
            }
            
            loginModal = loginVisualTreeAsset.Instantiate();
            var main = rootVisualElement.Q("main");
            var loginModalContainer = loginModal.Q("overlay-background");
            main.Add(loginModalContainer);

            var emailField = loginModalContainer.Q<TextField>("email-field");
            var passwordField = loginModalContainer.Q<TextField>("password-field");
            passwordField.isPasswordField = true;
            var loginButton = loginModalContainer.Q<Button>("login-button");
            var registerInfo = loginModalContainer.Q<Label>("register-info");
            LocalizationManager.OnLanguageChanged += () =>
            {
                registerInfo.text = LocalizationManager.GetLocalizedValue("register_info");
            };
            registerInfo.text = LocalizationManager.GetLocalizedValue("register_info");
            
            var lang = loginModalContainer.Q<EnumField>("language-field");
            var resetPasswordButton = loginModalContainer.Q<Button>("reset-password");
            var registerButton = loginModalContainer.Q<Button>("register");
            
            var logo = loginModalContainer.Q<Image>("logo");
            if (logo == null)
            {
                Debug.LogError("Logo element not found in the UXML file");
            }
            else
            {
                logo.vectorImage = m_LoginLogo;
                if (logo.vectorImage == null)
                {
                    Debug.LogError("VectorImage is not assigned");
                }
            }

            loginButton.clicked += () =>
            {
                Debug.Log("Login button clicked");
                if (!isLoggingIn)
                {
                    string email = emailField.value;
                    string password = passwordField.value;

                    // 로그인 로직 수행
                    ExecuteLogin(email, password);
                }
            };
            
            resetPasswordButton.clicked += () =>
            {
                Debug.Log("Reset password button clicked");
                // go to reset password web page
                Application.OpenURL("https://eden-world.net/reset-password");
            };
            
            registerButton.clicked += () =>
            {
                Debug.Log("Register button clicked");
                // go to register web page
                Application.OpenURL("https://eden-world.net/login");
            };
            
            lang.RegisterValueChangedCallback(OnLanguageChanged);
        }

        private void ExecuteLogin(string email, string password)
        {
            isLoggingIn = true;  
            EditorCoroutineUtility.StartCoroutine(AuthManager.Login(email, password, OnLoginResult), this);
        }

        private void OnLoginResult(bool success)
        {
            Debug.Log($"Login success: {success}");
            isLoggingIn = false;
            if (success)
            {
                var main = rootVisualElement.Q("main");
                var loginModalContainer = main.Q("overlay-background");
                main.Remove(loginModalContainer);
                loginModal = null;
            }
            else
            {
                EditorUtility.DisplayDialog("Login Failed", "Please check your email and password.", "OK");
            }
        }
    }
}
