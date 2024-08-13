using System;
using System.Collections.Generic;
using Editor.Scripts.Manager;
using UnityEditor;

namespace Editor.Scripts
{
    [InitializeOnLoad]
    public class EdenStudioInitializer
    {
        public static event Action<ItemInfo> OnSelectedItemChanged;

        private const string EdenStudioPath = "Assets/Eden";
        private static bool _initialized;

        private static ItemInfo _selectedItem;

        public static ItemInfo SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem == value) return;
                _selectedItem = value;
                OnSelectedItemChanged?.Invoke(value);
            }
        }
        
        public static void InvokeSelectedItemChanged()
        {
            OnSelectedItemChanged?.Invoke(SelectedItem);
        }
        
        // 그룹별 선택된 아이템 정보
        public static Dictionary<ItemInfo.ModelSlot, List<ItemInfo>> SelectedItems = new()
        {
            { ItemInfo.ModelSlot.FullBody , new List<ItemInfo>()},
            {ItemInfo.ModelSlot.Hair, new List<ItemInfo>()},
            {ItemInfo.ModelSlot.UpperBody, new List<ItemInfo>()},
            {ItemInfo.ModelSlot.LowerBody, new List<ItemInfo>()},
            {ItemInfo.ModelSlot.Hand, new List<ItemInfo>()},
            {ItemInfo.ModelSlot.Foot, new List<ItemInfo>()},
            {ItemInfo.ModelSlot.Accessory, new List<ItemInfo>()},
            {ItemInfo.ModelSlot.Other, new List<ItemInfo>()},
            
        };

        static EdenStudioInitializer()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            // Add your initialization code here

            if (!AssetDatabase.IsValidFolder(EdenStudioPath))
            {
                AssetDatabase.CreateFolder("Assets", "Eden");
            }

            ItemManager.Initialize();
        }

        private static void OnEditorUpdate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            EditorApplication.update -= OnEditorUpdate;
            Initialize();
        }
    }
}