using System;
using System.Linq;
using Editor.Scripts;
using Editor.Scripts.Manager;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.Resources.Components
{
    public class ItemDropdownButton : VisualElement
    {
        private ItemInfo.ModelSlot _selectedCategory;
        private ItemInfo _selectedItem;
        
        public ItemDropdownButton(ItemInfo item, Action onItemSelected)
        {
            _selectedItem = item;
            _selectedCategory = item.slot;
            
            AddToClassList("item-dropdown-button");

            var dropdown = new DropdownField
            {
                label = item.modelName,
                choices = Enum.GetNames(typeof(ItemInfo.ModelSlot)).ToList(),
                value = item.slot.ToString()
            };
            dropdown.RegisterValueChangedCallback(OnDropdownChanged);
            
            Add(dropdown);

            dropdown.RegisterValueChangedCallback(evt =>
            {
                EnableInClassList("selected", true);
                foreach (var child in parent.Children())
                {
                    if (child != this)
                    {
                        child.EnableInClassList("selected", false);
                    }
                }

                onItemSelected?.Invoke();
            });
        }
        
        private void OnDropdownChanged(ChangeEvent<string> evt)
        {
            var category = (ItemInfo.ModelSlot)Enum.Parse(typeof(ItemInfo.ModelSlot), evt.newValue);
            _selectedCategory = category;
            _selectedItem.slot = category;
            ItemManager.UpdateItemInfo(_selectedItem, category);
        }
    }
}