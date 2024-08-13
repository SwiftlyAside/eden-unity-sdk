using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Editor.Scripts.Util;
using UnityEditor;
using UnityEngine;

namespace Editor.Scripts.Manager
{
    public class ItemManager : MonoBehaviour
    {
        private const string ItemsInfoPath = "Assets/Eden/items.eden";

        public static List<ItemInfo> ItemsInfoList;

        public static void Initialize()
        {
            if (!File.Exists(ItemsInfoPath))
            {
                SaveItemsInfo(new List<ItemInfo>());
            }

            UpdateItemsInfo();
        }

        public static void UpdateItemsInfo()
        {
            var allItems = GetItemsInfo();

            if (allItems == null) allItems = GetAllPrefabsAsItems();

            allItems = allItems.OrderByDescending(i => i.status == ItemInfo.ModelStatus.Pinned)
                .ThenByDescending(i => i.lastModified)
                .ToList();
            SaveItemsInfo(allItems);

            if (ItemsInfoList == null)
            {
                ItemsInfoList = allItems;
            }
        }

        internal static void UpdateItemInfo(ItemInfo item, ItemInfo.ModelSlot slot)
        {
            var items = ItemsInfoList;
            var index = items.FindIndex(i => i.path == item.path && i.modelName == item.modelName);
            items[index].slot = slot;
            SaveItemsInfo(items);
        }

        internal static void SaveItemsInfo(List<ItemInfo> items)
        {
            string directory = Path.GetDirectoryName(ItemsInfoPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonUtility.ToJson(new ItemInfoList { items = items }.ToData());
            File.WriteAllText(ItemsInfoPath, json);

            // 저장 후 ItemsInfoList 업데이트
            ItemsInfoList = items;
        }

        internal static List<ItemInfo> GetItemsInfo()
        {
            if (ItemsInfoList != null) return ItemsInfoList;

            if (File.Exists(ItemsInfoPath))
            {
                var json = File.ReadAllText(ItemsInfoPath);
                var itemsInfoList = JsonUtility.FromJson<ItemInfoListData>(json);
                ItemsInfoList = itemsInfoList.items.Select(i => new ItemInfo
                {
                    path = i.path,
                    modelName = i.modelName,
                    lastModified = i.lastModified,
                    type = (ItemInfo.ModelType)i.type,
                    slot = (ItemInfo.ModelSlot)i.slot,
                    status = (ItemInfo.ModelStatus)i.status,
                    preview = null,
                    SelectedBlendShapes = i.SelectedBlendShapes
                }).ToList();
            }

            return ItemsInfoList;
        }

        internal static List<ItemInfo> GetAllPrefabsAsItems(bool preview = false)
        {
            var guids = AssetUtility.FindAssetsExcludingDirectory("t:Prefab", new[] { "Resources", "Eden", "lilToon" });

            var items = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(GetItem)
                .OrderByDescending(p => p.modelName)
                .ToList();

            ItemsInfoList = items;

            return items;
        }

        internal static ItemInfo GetItem(string itemPath)
        {
            var itemInfo = ScriptableObject.CreateInstance<ItemInfo>();
            itemInfo.path = itemPath;
            itemInfo.modelName = Path.GetFileNameWithoutExtension(itemPath);
            itemInfo.lastModified = File.GetLastWriteTime(itemPath).ToString(CultureInfo.InvariantCulture);

            // 로컬 데이터에서 해당 아이템 정보 가져오기
            var items = GetItemsInfo();
            var item = items.Find(i => i.path == itemPath);

            if (item != null)
            {
                itemInfo.type = item.type;
                itemInfo.slot = item.slot;
                itemInfo.status = item.status;
                itemInfo.preview = item.preview;
                itemInfo.SelectedBlendShapes = item.SelectedBlendShapes;
            }
            else
            {
                itemInfo.type = ItemInfo.ModelType.Other;
                itemInfo.slot = ItemInfo.ModelSlot.None;
                itemInfo.status = ItemInfo.ModelStatus.Show;
            }

            return itemInfo;
        }
    }
}