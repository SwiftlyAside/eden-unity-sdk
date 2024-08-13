using System;
using System.Collections.Generic;
using System.Linq;
using Editor.Scripts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.Resources.Components
{
    public class CustomDropdownField : VisualElement
    {
        private Label label;
        private Button dropdownButton;
        private VisualElement dropdownMenu;
        private ItemInfo _selectedItem;

        // on selected item changed
        public event Action<ItemInfo> OnSelectedChanged;

        public CustomDropdownField(string labelText, List<ItemInfo> items)
        {
            Debug.Log("CustomDropdownField");
            Debug.Log(items.Count);
            Debug.Log(items[0].modelName);
            label = new Label(labelText);
            label.AddToClassList("header-small");
            dropdownButton = new ItemClosetButton(items[0], OnDropdownButtonClicked) { text = items[0].modelName };
            dropdownMenu = new VisualElement
            {
                style =
                {
                    display = DisplayStyle.None,
                }
            };

            foreach (var itemButton in items.Select(item => new ItemClosetButton(item, () =>
                     {
                         dropdownButton.text = item.modelName;
                         dropdownMenu.style.display = DisplayStyle.None;
                         _selectedItem = item;
                         OnSelectedChanged?.Invoke(item);
                     })))
            {
                dropdownMenu.Add(itemButton);
            }

            Add(label);
            Add(dropdownButton);
            Add(dropdownMenu);

            style.flexDirection = FlexDirection.Column;
            dropdownMenu.style.flexDirection = FlexDirection.Column;
            dropdownMenu.style.borderTopWidth = 1;
            dropdownMenu.style.borderBottomWidth = 1;
            dropdownMenu.style.borderLeftWidth = 1;
            dropdownMenu.style.borderRightWidth = 1;
            dropdownMenu.style.backgroundColor = new StyleColor(Color.white);
        }

        private void OnDropdownButtonClicked()
        {
            dropdownMenu.style.display =
                dropdownMenu.style.display == DisplayStyle.Flex ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void SetLabelText(string text)
        {
            label.text = text;
        }

        public void SetDropdownButtonText(string text)
        {
            dropdownButton.text = text;
        }

        public void AddDropdownItem(ItemInfo item)
        {
            var itemButton = new ItemClosetButton(item, () =>
            {
                dropdownButton.text = item.modelName;
                dropdownMenu.style.display = DisplayStyle.None;
            });
            dropdownMenu.Add(itemButton);
        }

        public void RemoveDropdownItem(ItemInfo item)
        {
            var itemButton = dropdownMenu.Children().FirstOrDefault(child =>
                child is ItemClosetButton && ((ItemClosetButton)child).text == item.modelName);
            if (itemButton != null)
            {
                dropdownMenu.Remove(itemButton);
            }
        }

        public void ClearDropdownItems()
        {
            dropdownMenu.Clear();
        }

        public void SetDropdownItems(List<ItemInfo> items)
        {
            ClearDropdownItems();
            foreach (var item in items)
            {
                AddDropdownItem(item);
            }
        }

        public ItemInfo GetSelectedItem()
        {
            return _selectedItem;
        }
    }
}