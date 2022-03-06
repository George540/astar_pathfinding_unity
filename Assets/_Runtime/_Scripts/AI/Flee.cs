using UnityEngine;

namespace AI
{
    public class Flee : AIMovement
    {
        public override SteeringOutput GetKinematic(AIAgent agent)
        {
            var output = base.GetKinematic(agent);

            // TODO: calculate linear component
            Vector3 desiredVelocity = transform.position - agent.TargetPosition;
            desiredVelocity = desiredVelocity.normalized * agent.maxSpeed;

            output.linear = desiredVelocity;
            
            if (debug) Debug.DrawRay(transform.position, output.linear, Color.cyan);
			
            return output;
        }

        public override SteeringOutput GetSteering(AIAgent agent)
        {
            var output = base.GetSteering(agent);

            // TODO: calculate linear component
            Vector3 desiredVelocity = transform.position - agent.TargetPosition;
            desiredVelocity = desiredVelocity.normalized * agent.maxSpeed;
            Vector3 steering = desiredVelocity - agent.Velocity;

            output.linear = desiredVelocity;
            
            if (debug) Debug.DrawRay(transform.position, output.linear, Color.cyan);
			
            return output;
        }
    }
}
