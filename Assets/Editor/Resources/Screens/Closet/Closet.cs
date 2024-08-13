using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Editor.Resources.Components;
using Editor.Scripts;
using Editor.Scripts.Manager;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ScrollView = UnityEngine.UIElements.ScrollView;

namespace Editor.Resources.Screens.Closet
{
    public class Closet : EditorWindow
    {
        [SerializeField] private VisualTreeAsset m_VisualTreeAsset;
        private static VisualElement _container;
        private static VisualElement _scrollView;
        private static List<ItemInfo> _items;
        private static Button _importButton;
        private static VisualElement _exportOptions;
        private static Button _exportButton;
        private static Button _exportUpkButton;
        private static Button _exportVrmButton;
        private static Preview _preview;
        private static Label _title;
        private static Label _subtitle;
        private static Button _applyButton;

        public static void Show(VisualElement root, Action onExportVrmClicked)
        {
            _container = root;
            _container.Clear();

            var visualTree = UnityEngine.Resources.Load<VisualTreeAsset>("Screens/Closet/Closet");
            visualTree.CloneTree(_container);
            _scrollView = _container.Q("scroll-view");
            _importButton = _container.Q<Button>("import-button");
            _importButton.clicked += ImportItem;
            _exportOptions = _container.Q("exportOptions");
            _exportButton = _container.Q<Button>("exportButton");
            _exportUpkButton = _container.Q<Button>("exportUpkButton");
            _exportVrmButton = _container.Q<Button>("exportVrmButton");
            _title = _container.Q<Label>("title");
            _subtitle = _container.Q<Label>("subtitle");
            _applyButton = _container.Q<Button>("applyButton");

            _exportUpkButton.clicked += ExportUnityPackage;
            _exportVrmButton.clicked += onExportVrmClicked;
            _preview = new Preview(_container.Q("preview"), EdenStudioInitializer.SelectedItem?.path ?? "");
            _preview.ShowContent();
            _exportOptions.style.display = DisplayStyle.None;

            _exportButton.clicked += () =>
            {
                _exportOptions.style.display = (_exportOptions.style.display == DisplayStyle.None)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            };

            EdenStudioInitializer.OnSelectedItemChanged += OnSelectedItemChanged;
            LocalizationManager.OnLanguageChanged += RefreshLocalization;

            RefreshLocalization();
            if (EdenStudioInitializer.SelectedItem != null)
            {
                _preview.RefreshPreview(EdenStudioInitializer.SelectedItem.path);
            }

            LoadItems();

            _applyButton.clicked += () =>
            {
                EditorApplication.ExecuteMenuItem("Window/General/Scene");
            };
        }

        private static void RefreshLocalization()
        {
            _importButton.text = LocalizationManager.GetLocalizedValue("import_prefab");
            _exportButton.text = LocalizationManager.GetLocalizedValue("export");
            _exportUpkButton.text = LocalizationManager.GetLocalizedValue("save_as_unitypackage");
            _exportVrmButton.text = LocalizationManager.GetLocalizedValue("save_as_vrm");
            _title.text = LocalizationManager.GetLocalizedValue("closet");
            _subtitle.text = LocalizationManager.GetLocalizedValue("closet_description");
        }

        private static void ExportUnityPackage()
        {
            var selectedItem = EdenStudioInitializer.SelectedItem;
            var path = EditorUtility.SaveFilePanel("Save UnityPackage", "", "New UnityPackage", "unitypackage");
            if (string.IsNullOrEmpty(path)) return;
            AssetDatabase.ExportPackage(selectedItem.path, path, ExportPackageOptions.IncludeDependencies);
        }

        private static void OnSelectedItemChanged(ItemInfo item)
        {
            _preview.RefreshPreview(item.path);
        }

        private static void ImportItem()
        {
            var path = EditorUtility.OpenFilePanel("Import Item", "", "unitypackage");
            if (string.IsNullOrEmpty(path)) return;
            var extension = Path.GetExtension(path);
            if (extension != ".unitypackage") return;
            AssetDatabase.onImportPackageItemsCompleted += OnImportItemsCompleted;
            AssetDatabase.ImportPackage(path, false);
        }

        private static void OnImportItemsCompleted(string[] importedAssets)
        {
            AssetDatabase.onImportPackageItemsCompleted -= OnImportItemsCompleted;
            LoadItems();
        }

        private static async void LoadItems()
        {
            await Task.Yield();
            GetItems();
        }

        private static void GetItems()
        {
            _scrollView.Clear();

            _items = ItemManager.GetAllPrefabsAsItems();

            if (_items.Count == 0)
            {
                var button = new Button(() => ImportItem());
                button.text = LocalizationManager.GetLocalizedValue("import_prefab");

                var label = new Label(LocalizationManager.GetLocalizedValue("add_prefabs_and_use_features"));
                _scrollView.Add(label);
                _scrollView.Add(button);
                
                return;
            }
            
            foreach(var item in _items)
            {
                Debug.Log(item.modelName);
                Debug.Log(item.slot);
            }
            
            //custom dropdown 추가
            var dropdown = new CustomDropdownField("아바타 선택" , _items.FindAll(item => item.slot == ItemInfo.ModelSlot.Model));
            dropdown.OnSelectedChanged += item =>
            {
                EdenStudioInitializer.SelectedItem = item;
            };
            _scrollView.Add(dropdown);
            // 아바타를 제외한 아이템들을 카테고리별로 나누어 리스트뷰로 보여줌
            var listPerCategory = _items
                .Where(item => item.slot != ItemInfo.ModelSlot.Model)
                .GroupBy(item => item.slot)
                .OrderByDescending(group => group.Key);
            
            foreach (var category in listPerCategory)
            {
                var categoryListView = new CustomListView(LocalizationManager.GetLocalizedValue(category.Key.ToString().ToLower()), category.ToList());
                _scrollView.Add(categoryListView);
                categoryListView.OnSelectedChanged += item =>
                {
                    EdenStudioInitializer.SelectedItems[category.Key] = item;
                    
                    Debug.Log("SelectedItems: " + EdenStudioInitializer.SelectedItems[category.Key].Count);
                };
            }
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Instantiate UXML
            VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
            root.Add(labelFromUXML);
        }
    }
}