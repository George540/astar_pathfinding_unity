using AI;
using UnityEngine;

namespace _Runtime._Scripts.AI
{
    public class Pursue : AIMovement
    {
        public override SteeringOutput GetKinematic(AIAgent agent)
        {
            var output = base.GetKinematic(agent);

            // TODO: calculate linear component
            float distance = Vector3.Distance(agent.TargetPosition, transform.position);
            float ahead = distance / 10;
            Vector3 futurePosition = agent.TargetPosition + agent.TargetVelocity * ahead;

            output.linear = GetKinematic(agent).linear - agent.Velocity;
            
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

            output.linear = GetKinematic(agent).linear - agent.Velocity;
            
            if (debug) Debug.DrawRay(transform.position + agent.Velocity, output.linear, Color.green);

            return output;
        }
    }
}
