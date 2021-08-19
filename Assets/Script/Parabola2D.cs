using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parabola2D
{
    public float gravity { get { return Physics2D.gravity.y; } }

    public Vector2 VelocityData(Rigidbody2D MyRb, Vector2 targetPos, float hight = 5)
    {
        return CalculateLaunchData(MyRb, targetPos, hight).initialVelocity;
    }

    LaunchData CalculateLaunchData(Rigidbody2D MyRb, Vector2 targetPos, float hight = 5)
    {
        float displacementY = targetPos.y - MyRb.position.y;
        Vector3 displacementXZ = new Vector3(targetPos.x - MyRb.position.x, 0, 0);
        float time = Mathf.Sqrt(-2 * hight / gravity) + Mathf.Sqrt(2 * (displacementY - hight) / gravity);
        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * hight);
        Vector3 velocityXZ = displacementXZ / time;

        return new LaunchData(velocityXZ + velocityY * -Mathf.Sign(gravity), time);
    }

    public void DrawPath(Rigidbody2D MyRb, Vector2 targetPos, float hight = 5)
    {
        LaunchData launchData = CalculateLaunchData(MyRb, targetPos, hight);
        Vector3 previousDrawPoint = MyRb.position;

        int resolution = 30;
        for (int i = 1; i <= resolution; i++)
        {
            float simulationTime = i / (float)resolution * launchData.timeToTarget;
            Vector2 displacement = launchData.initialVelocity * simulationTime + Vector3.up * gravity * simulationTime * simulationTime / 2f;
            Vector2 drawPoint = MyRb.position + displacement;
            Debug.DrawLine(previousDrawPoint, drawPoint, Color.green);
            previousDrawPoint = drawPoint;
        }
    }

    struct LaunchData
    {
        public readonly Vector3 initialVelocity;
        public readonly float timeToTarget;

        public LaunchData(Vector3 initialVelocity, float timeToTarget)
        {
            this.initialVelocity = initialVelocity;
            this.timeToTarget = timeToTarget;
        }

    }
}
