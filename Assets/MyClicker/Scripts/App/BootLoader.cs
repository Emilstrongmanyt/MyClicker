using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyClicker.App
{
    public class BootLoader : MonoBehaviour
    {
        [SerializeField] float splashSeconds = 0.35f;

        void Start()
        {
            GameServices.Ensure();
            Invoke(nameof(Go), splashSeconds);
        }

        void Go()
        {
            string scene = GameServices.Instance.Save.HasCharacter ? "Battle" : "CharacterCreate";
            SceneManager.LoadScene(scene);
        }
    }
}
