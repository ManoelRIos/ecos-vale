using UnityEngine;

namespace Ecos
{
    public sealed class NightAtmosphere : MonoBehaviour
    {
        public WorldClock clock;
        public Transform player;
        public Palette palette;
        Transform stars;
        void Start()
        {
            stars = new GameObject("Estrelas estilizadas").transform;
            stars.SetParent(transform);
            for (int i = 0; i < 85; i++)
            {
                float az = WorldMath.Random(i, 1, 43) * Mathf.PI * 2, el = .2f + WorldMath.Random(i, 2, 43) * 1.2f;
                Vector3 p = new Vector3(Mathf.Cos(az) * Mathf.Cos(el), Mathf.Sin(el), Mathf.Sin(az) * Mathf.Cos(el)) * 300;
                palette.Part(stars, "Estrela", PrimitiveType.Cube, p, Vector3.one * (.25f + WorldMath.Random(i, 3, 43) * .6f), new Color(.7f, .78f, 1), true);
            }
        }

        void LateUpdate()
        {
            if (!stars)
                return;
            stars.position = player.position;
            stars.gameObject.SetActive(clock.IsNight);
        }
    }
}
