using UnityEngine;
using UnityEngine.InputSystem;

namespace Ecos
{
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public bool blocked;
        float yaw = 30, pitch = 24, distance = 6;
        Vector3 smooth;
        void LateUpdate()
        {
            if (!target)
                return;
            var m = Mouse.current;
            if (!blocked && m != null)
            {
                Vector2 d = m.delta.ReadValue();
                yaw += d.x * .12f;
                pitch = Mathf.Clamp(pitch - d.y * .10f, -12, 65);
                distance = Mathf.Clamp(distance - m.scroll.ReadValue().y * .008f, 2, 11);
            }

            Vector3 focus = target.position + Vector3.up * 1.5f;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 dir = rotation * Vector3.back;
            float actual = distance;
            if (Physics.SphereCast(focus, .2f, dir, out var hit, distance, 1, QueryTriggerInteraction.Ignore))
            {
                if (!hit.transform.IsChildOf(target))
                    actual = Mathf.Max(.6f, hit.distance - .2f);
            }

            Vector3 desired = focus + dir * actual;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref smooth, .06f);
            transform.LookAt(focus);
        }
    }
}
