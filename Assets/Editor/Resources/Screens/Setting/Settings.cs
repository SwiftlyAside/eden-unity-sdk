using System;
using Editor.Scripts.Manager;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.Resources.Screens.Setting
{
    public class Settings: EditorWindow
    {
        [SerializeField] private VisualTreeAsset m_VisualTreeAsset;
        private static VisualElement _container;
        private static Label _title;
        private static Label _emailLabel;
        private static Label _languageLabel;
        private static EnumField _languageDropdown;
        private static Button _logoutButton;
        private static TextField _emailField;

        public static void Show(VisualElement root, Action onLogoutClicked)
        {
            _container = root;
            _container.Clear();
            var visualTree = UnityEngine.Resources.Load<VisualTreeAsset>("Screens/Setting/Settings");
            visualTree.CloneTree(_container);
            _title = _container.Q<Label>("title");
            _emailLabel = _container.Q<Label>("email_label");
            _emailField = _container.Q<TextField>("email_field");
            _languageLabel = _container.Q<Label>("language_label");
            _languageDropdown = _container.Q<EnumField>("language_field");
            _logoutButton = _container.Q<Button>("logout_button");
            
            LocalizationManager.OnLanguageChanged += RefreshLocalization;
            _languageDropdown.RegisterValueChangedCallback(OnLanguageChanged);
            _logoutButton.clicked += onLogoutClicked;
            _emailField.value = AuthManager.IsAuthenticated ? AuthManager.UserEmail : "";
        }
        
        
        private static void RefreshLocalization()
        {
            _title.text = LocalizationManager.GetLocalizedValue("settings");
            _emailLabel.text = LocalizationManager.GetLocalizedValue("email");
            _languageLabel.text = LocalizationManager.GetLocalizedValue("language");
            _logoutButton.text = LocalizationManager.GetLocalizedValue("logout");
        }
        
        private static void OnLanguageChanged(ChangeEvent<Enum> evt)
        {
            Debug.Log($"Language changed to {evt.newValue}");
            var language = (Language)evt.newValue;
            LocalizationManager.SetLanguage(language);
        }
    }
}