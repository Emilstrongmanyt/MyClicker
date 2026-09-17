using MyClicker.App;
using MyClicker.Audio;
using MyClicker.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MyClicker.UI
{
    public class SettingsPanel : MonoBehaviour
    {
        GameObject _root;
        bool _open;
        Button _music;
        Button _sfx;

        public bool Open => _open;

        public void Build(Transform parent, GameConfig.UiSkin skin)
        {
            var panel = StoneUi.Panel(parent, "SettingsPanel", skin);
            _root = panel.gameObject;
            StoneUi.Place(panel, 0.12f, 0.28f, 0.88f, 0.74f);

            var title = StoneUi.Label(panel.transform, "Title", "Settings", 36, TextAnchor.MiddleLeft);
            StoneUi.Place(title, 0.06f, 0.82f, 0.72f, 0.96f);
            var close = StoneUi.Button(panel.transform, "Close", "X", skin, Hide);
            StoneUi.Place(close, 0.78f, 0.82f, 0.96f, 0.96f);

            _music = StoneUi.Button(panel.transform, "Music", "Music", skin, ToggleMusic);
            StoneUi.Place(_music, 0.08f, 0.54f, 0.92f, 0.74f);
            _sfx = StoneUi.Button(panel.transform, "Sfx", "Sound", skin, ToggleSfx);
            StoneUi.Place(_sfx, 0.08f, 0.32f, 0.92f, 0.52f);
            var look = StoneUi.Button(panel.transform, "Look", "Hero look", skin, OpenCreator);
            StoneUi.Place(look, 0.08f, 0.08f, 0.92f, 0.28f);
            Hide();
        }

        public void Toggle()
        {
            if (_open) Hide();
            else Show();
        }

        public void Show()
        {
            _open = true;
            if (_root != null)
            {
                _root.SetActive(true);
                StoneUi.BringFront(_root);
            }

            Refresh();
        }

        public void Hide()
        {
            _open = false;
            if (_root != null)
                _root.SetActive(false);
        }

        public void Refresh()
        {
            if (!_open)
                return;
            var profile = GameServices.Instance != null && GameServices.Instance.Save != null
                ? GameServices.Instance.Save.Profile
                : null;
            SetLabel(_music, profile != null && profile.muteMusic ? "Music  off" : "Music  on");
            SetLabel(_sfx, profile != null && profile.muteSfx ? "Sound  off" : "Sound  on");
        }

        void ToggleMusic()
        {
            var save = GameServices.Instance != null ? GameServices.Instance.Save : null;
            if (save == null || save.Profile == null)
                return;
            save.Profile.muteMusic = !save.Profile.muteMusic;
            save.MarkDirty();
            AudioDirector.Ensure().ApplyMix();
            Refresh();
        }

        void ToggleSfx()
        {
            var save = GameServices.Instance != null ? GameServices.Instance.Save : null;
            if (save == null || save.Profile == null)
                return;
            save.Profile.muteSfx = !save.Profile.muteSfx;
            save.MarkDirty();
            AudioDirector.Ensure().ApplyMix();
            Refresh();
        }

        void OpenCreator()
        {
            Hide();
            if (GameServices.Instance != null && GameServices.Instance.Save != null)
                GameServices.Instance.Save.PersistNow();
            SceneManager.LoadScene("CharacterCreate");
        }

        static void SetLabel(Button button, string text)
        {
            if (button == null)
                return;
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.text = text;
        }
    }
}
