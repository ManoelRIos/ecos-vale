using UnityEngine;

namespace Ecos
{
    /// <summary>A small, isolated menu stage. It does not change the world's environment.</summary>
    public sealed class MenuBackdrop : MonoBehaviour
    {
        const int BackdropLayer = 30;
        const int BackdropMask = 1 << BackdropLayer;
        static readonly Color Moss = new Color(.20f, .38f, .29f);
        static readonly Color Warm = new Color(1f, .52f, .16f);
        static readonly Color Teal = new Color(.18f, .68f, .60f);
        static readonly Color Timber = new Color(.24f, .15f, .18f);

        Palette palette;
        Camera view;
        Transform traveler;
        readonly Transform[] motes = new Transform[6];
        readonly Vector3[] moteOrigins = new Vector3[6];
        Vector3 travelerOrigin;
        float elapsed;

        /// <summary>
        /// Create after URP preparation. Disable the returned object before starting the world.
        /// The supplied camera should also be disabled when the gameplay camera takes over.
        /// </summary>
        public static GameObject Create(Transform parent, Camera camera)
        {
            var stage = new GameObject("Vale - cenario do menu");
            stage.transform.SetParent(parent, false);
            var backdrop = stage.AddComponent<MenuBackdrop>();
            backdrop.Build(camera);
            return stage;
        }

        void Build(Camera camera)
        {
            view = camera;
            palette = new Palette();
            var root = transform;
            palette.Part(root, "Base de pedra", PrimitiveType.Cylinder,
                new Vector3(5, -.85f, 4), new Vector3(24, .65f, 20), new Color(.12f, .17f, .22f));
            palette.Part(root, "Clareira", PrimitiveType.Cylinder,
                new Vector3(5, -.3f, 4), new Vector3(23, .3f, 19), Moss);

            var shrine = LowPolyFactory.Shrine(palette, 2);
            shrine.name = "Cabana e altar do menu";
            shrine.transform.SetParent(root, false);
            shrine.transform.localPosition = new Vector3(6, 0, 3.5f);
            shrine.transform.localScale = Vector3.one * .86f;

            Vector3[] treePositions = {
                new Vector3(.2f, 0, 7.5f), new Vector3(10.8f, 0, 6),
                new Vector3(-2.2f, 0, 10), new Vector3(13, 0, 10),
                new Vector3(3.2f, 0, 12), new Vector3(8.6f, 0, 12.5f)
            };
            int[] treeBiomes = { 4, 1, 0, 4, 2, 1 };
            Color[] foliage = {
                new Color(.16f, .53f, .43f), new Color(.54f, .60f, .25f),
                new Color(.32f, .21f, .40f), new Color(.22f, .55f, .46f),
                new Color(.22f, .40f, .43f), new Color(.60f, .44f, .22f)
            };
            for (int i = 0; i < treePositions.Length; i++)
            {
                var tree = LowPolyFactory.Prop(palette, treeBiomes[i], 0, foliage[i]);
                tree.name = "Arvore do menu";
                tree.transform.SetParent(root, false);
                tree.transform.localPosition = treePositions[i];
                tree.transform.localRotation = Quaternion.Euler(0, i * 71, 0);
                tree.transform.localScale = Vector3.one * (i == 2 ? .86f : .96f);
            }

            for (int i = 0; i < 3; i++)
            {
                var plant = LowPolyFactory.Prop(palette, i == 1 ? 0 : 4, 2, Teal);
                plant.name = "Luz da clareira";
                plant.transform.SetParent(root, false);
                plant.transform.localPosition = new Vector3(3.1f + i * 2.7f, .02f, .1f + i * .7f);
                plant.transform.localScale = Vector3.one * .8f;
            }
            var rock = LowPolyFactory.Prop(palette, 4, 1, Moss);
            rock.transform.SetParent(root, false);
            rock.transform.localPosition = new Vector3(9.3f, 0, 1.1f);
            rock.transform.localScale = Vector3.one * .9f;

            for (int i = 0; i < 5; i++)
            {
                var step = palette.Part(root, "Pedra da trilha", PrimitiveType.Cylinder,
                    new Vector3(1.6f + i * .8f, .045f, -1.6f + i * .92f),
                    new Vector3(.95f, .055f, .72f), new Color(.46f, .40f, .38f));
                step.transform.localRotation = Quaternion.Euler(0, i * 41, 0);
            }

            var figure = LowPolyFactory.Player(palette);
            traveler = figure.transform;
            traveler.SetParent(root, false);
            travelerOrigin = new Vector3(4.15f, .02f, .4f);
            traveler.localPosition = travelerOrigin;
            traveler.localRotation = Quaternion.Euler(0, 155, 0);
            AddLantern(new Vector3(7.7f, 1.55f, 2.1f));
            AddPointLight("Luz do viajante", new Vector3(4.65f, 1.1f, .4f), Warm, 1.25f, 5);

            var key = AddLight("Sol do cenario", LightType.Directional, new Color(1, .76f, .48f), 1.05f);
            key.transform.localRotation = Quaternion.Euler(42, -24, 0);
            key.shadows = LightShadows.Soft;
            var fill = AddLight("Luz fria do cenario", LightType.Directional, new Color(.35f, .61f, .87f), .36f);
            fill.transform.localRotation = Quaternion.Euler(25, 155, 0);
            AddPointLight("Brilho do altar", new Vector3(6, 2.4f, 3.5f), Teal, 1.2f, 7);

            for (int i = 0; i < motes.Length; i++)
            {
                Vector3 point = new Vector3(2.7f + i * 1.1f, 1.2f + (i % 3) * .65f, 1.3f + (i % 2) * 2.1f);
                moteOrigins[i] = point;
                motes[i] = palette.Part(root, "Vagalume", PrimitiveType.Sphere, point,
                    Vector3.one * .055f, i % 2 == 0 ? Warm : Teal, true).transform;
            }

            foreach (var collider in GetComponentsInChildren<Collider>())
                Destroy(collider);
            foreach (var light in GetComponentsInChildren<Light>())
                light.cullingMask = BackdropMask;
            SetLayer(root);
            // This changes only the supplied menu camera, never the gameplay camera.
            view.cullingMask = BackdropMask;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(.035f, .075f, .095f);
            view.fieldOfView = 43;
            view.nearClipPlane = .1f;
            view.farClipPlane = 80;
            FrameCamera(0);
        }

        void AddLantern(Vector3 position)
        {
            palette.Part(transform, "Poste da lanterna", PrimitiveType.Cylinder,
                position + Vector3.down * .65f, new Vector3(.11f, .72f, .11f), Timber);
            palette.Part(transform, "Lanterna acesa", PrimitiveType.Cube,
                position, new Vector3(.23f, .33f, .23f), Warm, true);
            palette.Part(transform, "Tampa da lanterna", PrimitiveType.Cube,
                position + Vector3.up * .2f, new Vector3(.34f, .08f, .34f), Timber);
            AddPointLight("Luz da lanterna", position, Warm, 1.6f, 6);
        }

        Light AddLight(string objectName, LightType type, Color color, float intensity)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(transform, false);
            child.layer = BackdropLayer;
            var light = child.AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.cullingMask = BackdropMask;
            light.shadows = LightShadows.None;
            return light;
        }

        void AddPointLight(string objectName, Vector3 position, Color color, float intensity, float range)
        {
            var light = AddLight(objectName, LightType.Point, color, intensity);
            light.transform.localPosition = position;
            light.range = range;
        }

        static void SetLayer(Transform root)
        {
            root.gameObject.layer = BackdropLayer;
            foreach (Transform child in root)
                SetLayer(child);
        }

        void FrameCamera(float time)
        {
            if (!view) return;
            Vector3 position = new Vector3(8.4f + Mathf.Sin(time * .09f) * .42f, 5.8f, -16.6f);
            view.transform.position = transform.TransformPoint(position);
            view.transform.LookAt(transform.TransformPoint(new Vector3(.8f, 1.65f, 3.7f)));
        }

        void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            FrameCamera(elapsed);
            if (traveler)
                traveler.localPosition = travelerOrigin + Vector3.up * Mathf.Sin(elapsed * 1.7f) * .018f;
            for (int i = 0; i < motes.Length; i++)
            {
                if (!motes[i]) continue;
                Vector3 drift = new Vector3(Mathf.Sin(elapsed * .6f + i) * .14f,
                    Mathf.Sin(elapsed * .9f + i * 1.7f) * .18f, Mathf.Cos(elapsed * .4f + i) * .1f);
                motes[i].localPosition = moteOrigins[i] + drift;
            }
        }

        void OnDestroy()
        {
            if (palette != null)
                palette.Dispose();
        }
    }
}
