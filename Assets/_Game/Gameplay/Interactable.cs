using UnityEngine;

namespace Ecos
{
    public sealed class Interactable : MonoBehaviour
    {
        public string id, title, itemId = "crystal";
        public bool shrine;
        public int amount = 1;
        void Update()
        {
            if (!shrine)
                transform.Rotate(0, 30 * Time.deltaTime, 0, Space.World);
        }
    }
}
