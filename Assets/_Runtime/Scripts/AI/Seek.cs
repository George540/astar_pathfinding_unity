using UnityEngine;

namespace AI
{
    public class Seek : AIMovement
    {
        public override SteeringOutput GetKinematic(AIAgent agent)
        {
            var output = base.GetKinematic(agent);

            // TODO: calculate linear component
            Vector3 desiredVelocity = agent.TargetPosition - agent.transform.position;
            desiredVelocity = desiredVelocity.normalized * agent.maxSpeed;

            output.linear = desiredVelocity;
            

            if (debug) Debug.DrawRay(transform.position, output.linear, Color.cyan);

            return output;
        }

        public override SteeringOutput GetSteering(AIAgent agent)
        {
            var output = base.GetSteering(agent);

            // TODO: calculate linear component
            float distance = Vector3.Distance(agent.TargetPosition, transform.position);
            float ahead = distance / 10;
            Vector3 futurePosition = agent.TargetPosition + agent.TargetVelocity * ahead;
            
            // Seek()
            output.linear = KinematicSeek(agent, futurePosition) - agent.Velocity;
            
            if (debug) Debug.DrawRay(transform.position + agent.Velocity, output.linear, Color.green);

            return output;
        }

        private Vector3 KinematicSeek(AIAgent agent, Vector3 desiredPosition)
        {
            Vector3 desiredVelocity = desiredPosition - transform.position;
            desiredVelocity = desiredVelocity.normalized * agent.maxSpeed;
            return desiredVelocity;
        }
    }
}
