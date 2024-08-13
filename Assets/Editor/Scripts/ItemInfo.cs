using System;
using System.Collections.Generic;
using Editor.Scripts.Struct;
using UnityEngine;

namespace Editor.Scripts
{
    [Serializable]
    public class ItemInfo : ScriptableObject
    {
        public enum ModelType { VRChat, VRM, Other }
        // 미선택, 아바타 모델, 머리, 상반신, 하반신, 손, 발, 전신, 악세사리, 효과, 기타
        public enum ModelSlot { None, Model, Hair, UpperBody, LowerBody, Hand, Foot, FullBody, Accessory, Effect, Other }
        public enum ModelStatus { Pinned, Show, Hidden, Other }

        public string path;
        public string modelName; 
        public string lastModified;
        public ModelType type;
        public ModelSlot slot;
        public ModelStatus status;
        public Texture2D preview { get; internal set; }
        public Dictionary<string, List<BlendShapeData>> SelectedBlendShapes { get; set; } = new();

        public ItemInfoData ToData()
        {
            return new ItemInfoData
            {
                path = path,
                modelName = modelName,
                lastModified = lastModified,
                type = (ItemInfoData.ModelType) Enum.Parse(typeof(ItemInfoData.ModelType), type.ToString()),
                slot = (ItemInfoData.ModelSlot) Enum.Parse(typeof(ItemInfoData.ModelSlot), slot.ToString()),
                status = (ItemInfoData.ModelStatus) Enum.Parse(typeof(ItemInfoData.ModelStatus), status.ToString()),
                preview = preview != null ? Convert.ToBase64String(preview.EncodeToPNG()) : null,
                SelectedBlendShapes = SelectedBlendShapes
            };
        }

    }
}