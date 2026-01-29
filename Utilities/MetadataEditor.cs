using SDG.Unturned;
using System;
using System.Linq;
using UnityEngine.Windows;
using Wired.Utilities;

namespace Wired
{
    public class MetadataEditor
    {
        private Item _item;
        private PlayerEquipment _playerEquipment;
        public MetadataEditor(PlayerEquipment equipment)
        {
            _playerEquipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
            var item = EquipmentToItemJar(_playerEquipment);
            if (item != null)
            {
                _item = item;
            }
        }
        public bool GetMetadata(out byte[] metadata, byte offset = 0)
        {
            metadata = null;
            if (_item == null)
            {
                WiredLogger.Error($"_item null");
                return false;
            }
            var md = _item.metadata.Skip(offset).Take(2).ToArray();
            if (md == null || md.Length == 0)
            {
                WiredLogger.Error($"md null: {md == null}");
                return false;
            }
            metadata = md;
            return true;
        }
        public void SetMetadata(uint value, byte offset = 0)
        {
            var data = BitConverter.GetBytes(value);
            if(data == null || data.Length == 0)
            {
                WiredLogger.Error("data null");
                return;
            }
            if(offset == 2)
            {
                if(_item.metadata.Length > 2)
                {
                    _item.metadata[2] = data[0];
                    _item.metadata[3] = data[1];
                }
                else
                {
                    byte[] result = Enumerable.Repeat((byte)0, 2)
                          .Concat(data)
                          .ToArray();
                    _item.metadata = result;
                }
            }
            else
            {
                if(_item.metadata == null || _item.metadata.Length < 2)
                    _item.metadata = new byte[2];
                _item.metadata[0] = data[0];
                _item.metadata[1] = data[1];
            }
        }

        private Item EquipmentToItemJar(PlayerEquipment equipment)
        {
            var eqitem = equipment;

            if (eqitem == null || eqitem.player.inventory == null)
                return null;

            var page = eqitem.equippedPage;
            var x = eqitem.equipped_x;
            var y = eqitem.equipped_y;

            var index = eqitem.player.inventory.getIndex(page, x, y);
            return equipment.player.inventory.getItem(page, index).item;
        }
    }
}
