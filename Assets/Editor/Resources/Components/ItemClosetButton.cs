using System;
using Editor.Scripts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.Resources.Components
{
    public class ItemClosetButton: Button
    {
        private ItemInfo _selectedItem;
        
        public ItemClosetButton(ItemInfo item, Action onClick)
        {
            style.flexDirection = FlexDirection.Row;
            _selectedItem = item;
            
            AddToClassList("item-button");
            var preview = new Image { image = item.preview, scaleMode = ScaleMode.ScaleToFit, style = { width = 64, height = 64 } };
            Add(preview);

            var labelContainer = new VisualElement
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

            Add(labelContainer);

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
    }
}
