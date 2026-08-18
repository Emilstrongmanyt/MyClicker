using MyClicker.App;
using UnityEngine;

namespace MyClicker.Character
{
    public class CharacterView : MonoBehaviour
    {
        public float worldScale = 1.35f;

        CharacterPartLibrary _library;
        readonly System.Collections.Generic.Dictionary<string, SpriteRenderer> _layers =
            new System.Collections.Generic.Dictionary<string, SpriteRenderer>();

        public void Bind(CharacterPartLibrary library, CharacterSave save)
        {
            _library = library;
            if (_library == null)
                return;

            int order = 0;
            foreach (var slot in _library.Slots.Keys)
            {
                if (!_layers.TryGetValue(slot, out var renderer))
                {
                    var child = new GameObject(slot);
                    child.transform.SetParent(transform, false);
                    renderer = child.AddComponent<SpriteRenderer>();
                    _layers[slot] = renderer;
                }

                renderer.sortingOrder = order++;
                ApplySlot(slot, save.GetSlot(slot), save);
            }

            transform.localScale = Vector3.one * worldScale;
        }

        public void Cycle(string slot, int delta, CharacterSave save)
        {
            if (_library == null)
                return;
            int count = _library.Count(slot);
            if (count <= 0)
                return;
            int next = save.GetSlot(slot) + delta;
            next = ((next % count) + count) % count;
            save.SetSlot(slot, next);
            ApplySlot(slot, next, save);
        }

        void ApplySlot(string slot, int index, CharacterSave save)
        {
            if (!_layers.TryGetValue(slot, out var renderer))
                return;
            var sprite = _library.Get(slot, index);
            renderer.sprite = sprite;
            renderer.enabled = sprite != null;
            if (slot == "Hair")
                renderer.color = save.hairColor;
            else if (slot == "Armor")
                renderer.color = save.armorColor;
            else
                renderer.color = Color.white;
        }
    }
}
