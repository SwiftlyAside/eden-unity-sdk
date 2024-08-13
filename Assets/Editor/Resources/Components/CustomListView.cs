using System;
using System.Collections.Generic;
using System.Linq;
using Editor.Scripts;
using Editor.Scripts.Manager;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.Resources.Components
{
    public class CustomListView: VisualElement
    {
        private Label label;
        private List<ItemInfo> items;
        private List<ItemClosetCheckbox> itemButtons;
        private VisualElement listView;
        private List<ItemInfo> _selectedItems;
        
        public event Action<List<ItemInfo>> OnSelectedChanged; 
        
        public CustomListView(string labelText, List<ItemInfo> items)
        {
            label = new Label(labelText);
            label.AddToClassList("header-small");
            listView = new VisualElement();
            itemButtons = new List<ItemClosetCheckbox>();
            _selectedItems = new List<ItemInfo>();

            foreach (var itemButton in items.Select(item => new ItemClosetCheckbox(item, check =>
            {
                if (check)
                {
                    _selectedItems.Add(item);
                }
                else
                {
                    _selectedItems.Remove(item);
                }

                ModularManager.ToggleObject(item.path);
                OnSelectedChanged?.Invoke(_selectedItems);
                EdenStudioInitializer.InvokeSelectedItemChanged();
            }, ModularManager.HasObject(item.path)))
            )
            {
                itemButtons.Add(itemButton);
                listView.Add(itemButton);
            }
            Add(label);
            Add(listView);
            
            EdenStudioInitializer.OnSelectedItemChanged += item =>
            {
                foreach (var button in itemButtons)
                {
                    Debug.Log($"button path: {button.GetPath()}");
                    Debug.Log(ModularManager.HasObject(button.GetPath()));
                    button.SetSelected(ModularManager.HasObject(button.GetPath()));
                }
            };
            
            this.style.flexDirection = FlexDirection.Column;
            listView.style.flexDirection = FlexDirection.Column;
            listView.style.borderTopWidth = 1;
            listView.style.borderBottomWidth = 1;
            listView.style.borderLeftWidth = 1;
            listView.style.borderRightWidth = 1;
            listView.style.backgroundColor = new StyleColor(Color.white);
        }
    }
}