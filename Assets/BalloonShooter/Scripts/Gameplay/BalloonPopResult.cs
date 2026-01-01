using UnityEngine;

namespace BalloonShooter
{
    public readonly struct BalloonPopResult
    {
        public readonly bool popped;
        public readonly BalloonType type;
        public readonly float radius;
        public readonly float distanceFromCamera;
        public readonly BalloonPopCause cause;
        public readonly float pointsMultiplier;
        public readonly Vector3 popPosition;

        public BalloonPopResult(
            bool popped,
            BalloonType type,
            float radius,
            float distanceFromCamera,
            BalloonPopCause cause,
            float pointsMultiplier,
            Vector3 popPosition
        )
        {
            this.popped = popped;
            this.type = type;
            this.radius = radius;
            this.distanceFromCamera = distanceFromCamera;
            this.cause = cause;
            this.pointsMultiplier = pointsMultiplier;
            this.popPosition = popPosition;
        }
    }
}

