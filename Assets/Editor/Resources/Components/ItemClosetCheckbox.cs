using System;
using Editor.Scripts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.Resources.Components
{
    public class ItemClosetCheckbox : Button
    {
        private readonly ItemInfo _selectedItem;
        private readonly Toggle _checkbox;

        public ItemClosetCheckbox(ItemInfo item, Action<bool> onCheckboxChanged, bool isSelected = false)
        {
            style.flexDirection = FlexDirection.Row;
            _selectedItem = item;
            
            AddToClassList("item-button");

            var preview = new Image
            {
                image = item.preview,
                scaleMode = ScaleMode.ScaleToFit,
                style = { width = 64, height = 64 }
            };
            Add(preview);

            var labelContainer = new VisualElement()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    justifyContent = Justify.SpaceBetween,
                    flexGrow = 1,
                    paddingLeft = 10,
                    paddingRight = 10
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

            labelContainer.Add(label);

            _checkbox = new Toggle()
            {
                style =
                {
                    marginLeft = 10
                }
            };
            _checkbox.SetValueWithoutNotify(isSelected);
            _checkbox.RegisterValueChangedCallback(evt => 
            {
                onCheckboxChanged?.Invoke(evt.newValue);
            });

            labelContainer.Add(_checkbox);
            Add(labelContainer);
        }
        
        public string GetPath()
        {
            return _selectedItem.path;
        }
        
        public ItemInfo.ModelSlot GetSlot()
        {
            return _selectedItem.slot;
        }
        
        public void SetSelected(bool selected)
        {
            _checkbox.SetValueWithoutNotify(selected);
        }
    }
}
