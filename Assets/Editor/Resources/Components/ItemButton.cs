using System;
using System.Linq;
using Editor.Scripts;
using Editor.Scripts.Manager;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.Resources.Components
{
    public class ItemButton: Button
    {
        private ItemInfo.ModelSlot _selectedCategory;
        private ItemInfo _selectedItem;
        
        public ItemButton(ItemInfo item, Action onClick)
        {
            Debug.Log($"ItemButton: {item.modelName}");
            _selectedItem = item;
            _selectedCategory = item.slot;
            
            AddToClassList("item-button");
            var preview = new Image { image = item.preview, scaleMode = ScaleMode.ScaleToFit, style = { width = 64, height = 64 } };
            Add(preview);

            var labelContainer = new VisualElement()
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    alignItems = Align.FlexStart,
                    justifyContent = Justify.Center,
                    flexGrow = 1,
                }
            };

            var label = new Label(item.modelName)
            {
                style =
                {
                    unityTextAlign = TextAnchor.MiddleLeft,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            
            var categoryDropdown = new DropdownField
            {
                label = "",
                choices = Enum.GetNames(typeof(ItemInfo.ModelSlot)).ToList(),
                value = item.slot.ToString()
            };
            categoryDropdown.EnableInClassList("w-one-third", true);
            categoryDropdown.RegisterValueChangedCallback(OnDropdownChanged);
            
            labelContainer.Add(label);

            Add(labelContainer);
            Add(categoryDropdown);

            clicked += onClick;
            clicked += () =>
            {
                EnableInClassList("selected", true);
                foreach (var child in parent.Children())
                {
                    if (child != this)
                    {
                        child.EnableInClassList("selected", false);
                    }
                }
            };
        }
        
        private void RefreshLocalization()
        {
            // label.text = LocalizationManager.GetLocalizedValue("settings");
            // emailLabel.text = LocalizationManager.GetLocalizedValue("email");
            // languageLabel.text = LocalizationManager.GetLocalizedValue("language");
            // logoutButton.text = LocalizationManager.GetLocalizedValue("logout");
        }

        private void OnDropdownChanged(ChangeEvent<string> evt)
        {
            Debug.Log($"Dropdown changed to {evt.newValue}");
            var category = (ItemInfo.ModelSlot)Enum.Parse(typeof(ItemInfo.ModelSlot), evt.newValue);
            _selectedCategory = category;
            _selectedItem.slot = category;
            ItemManager.UpdateItemInfo(_selectedItem, category);
        }
    }
}