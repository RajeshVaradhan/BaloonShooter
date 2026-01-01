using UnityEngine;

namespace BalloonShooter
{
    public sealed class Shooter : MonoBehaviour
    {
        [SerializeField] private Camera aimCamera;
        [SerializeField] private float range = 80f;
        [SerializeField] private float fireCooldownSeconds = 0.09f;
        [SerializeField] private bool aimFromMousePosition = true;

        private GameManager _manager;
        private float _cooldown;

        public void SetManager(GameManager manager) => _manager = manager;

        private void Awake()
        {
            if (aimCamera == null) aimCamera = Camera.main;
        }

        private void Update()
        {
            if (_cooldown > 0f) _cooldown -= Time.unscaledDeltaTime;
            if (_manager == null || !_manager.IsRunning || _manager.IsPaused) return;
            if (aimCamera == null) return;

            if (Input.GetMouseButtonDown(0) && _cooldown <= 0f)
            {
                _cooldown = fireCooldownSeconds;
                Fire();
            }
        }

        private void Fire()
        {
            bool hitBalloon = false;
            bool hasPopInfo = false;
            BalloonPopResult popInfo = default;

            Vector3 screenPoint = aimFromMousePosition ? Input.mousePosition : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            Ray ray = aimCamera.ScreenPointToRay(screenPoint);
            if (Physics.Raycast(ray, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Collide))
            {
                Balloon balloon = hit.collider != null ? hit.collider.GetComponentInParent<Balloon>() : null;

                if (balloon != null && balloon.TryHit(out BalloonPopResult pop))
                {
                    hitBalloon = true;
                    hasPopInfo = true;
                    popInfo = pop;
                }
            }

            _manager.Score?.RegisterShot(hitBalloon);
            if (hitBalloon && hasPopInfo)
            {
                _manager.Score?.RegisterDirectHit(popInfo);
                if (popInfo.popped) _manager.Score?.RegisterPop(popInfo);
            }
        }
    }
}
