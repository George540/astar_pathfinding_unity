using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AI;
using UnityEngine;

namespace _Runtime._Scripts
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        // List of players in the game
        [SerializeField] private List<AIAgent> _players = new List<AIAgent>();
        // 0: Red (Pursuer), 1: Green (Wanderer/Target), 2: Yellow (Frozen)
        public List<Material> _cMaterials = new List<Material>();
        public Transform _targetCone;

        // current pursuer
        public AIAgent _pursuer;

        public AIAgent _lastTarget;
        // maximum distance between target and pursuer to freeze
        public float _tagDistance = 1f;
        
        // Map's offset bounds
        public float maxOffset = 40f;

        // Setting up singleton GameManager.cs
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            // Set all players to wander
            foreach (var player in _players)
            {
                player.gameObject.AddComponent<Wander>();
                player.gameObject.AddComponent<LookWhereYouAreGoing>();
                player.transform.rotation = Quaternion.Euler(0f, Random.Range(-180f, 180f), 0f);
            }
            // Set up pursuer
            SetPursuer(_players[Random.Range(0, _players.Count)]);
        }

        // Update is called once per frame
        void Update()
        {
            CheckIfTagged();
            SetConeToTarget();
        }

        // Set selected agent as pursuer
        private void SetPursuer(AIAgent agent)
        {
            _pursuer = agent;
            if (_pursuer.gameObject.GetComponent<Wander>() != null)
            {
                Destroy(_pursuer.gameObject.GetComponent<Wander>());
            }

            if (_pursuer.gameObject.GetComponent<LookWhereYouAreGoing>() != null)
            {
                Destroy(_pursuer.gameObject.GetComponent<LookWhereYouAreGoing>());
            }
            _pursuer.maxSpeed *= 1.8f;
            _pursuer.SetMaterial(_cMaterials[0]);
            _pursuer._ePlayerState = PlayerState.EPlayerState.Tagged;
            _pursuer.TrackTarget(FindNearestTargetToFreeze().transform);
            _pursuer.gameObject.AddComponent<LookWhereYouAreGoing>();
            _pursuer.gameObject.AddComponent<Seek>();
        }

        private AIAgent FindNearestTargetToFreeze()
        {
            // Set last target by pursuer, so he can be pursuer once he's frozen and the game restarts
            if (_players.FindAll(player => player._ePlayerState == PlayerState.EPlayerState.Unfrozen).Count <= 1)
            {
                _lastTarget = _players.Find(player => player._ePlayerState != PlayerState.EPlayerState.Tagged);
            }
            
            AIAgent closestPlayer = null;
            float closestDistanceSqr = Mathf.Infinity;
            foreach (var player in _players)
            {
                // skip if it compares itself or someone who is already frozen, being a rescuer or being a pursuer themselves
                if (player._ePlayerState == PlayerState.EPlayerState.Tagged || 
                    player._ePlayerState == PlayerState.EPlayerState.Frozen || 
                    player._ePlayerState == PlayerState.EPlayerState.Rescuer) continue;

                var distance = ToroidalDistance(_pursuer.transform.position, player.transform.position);
                // check if current comparison is smaller than the current closest player to target
                if (distance < closestDistanceSqr)
                {
                    closestDistanceSqr = distance;
                    closestPlayer = player;
                }
            }

            if (closestPlayer != null)
            {
                // Destroy components if they exist
                if (closestPlayer.GetComponent<Wander>() != null)
                {
                    Destroy(closestPlayer.GetComponent<Wander>());
                }
                if (closestPlayer.GetComponent<LookWhereYouAreGoing>() != null)
                {
                    Destroy(closestPlayer.GetComponent<LookWhereYouAreGoing>());
                }
                closestPlayer.TrackTarget(_pursuer.transform);
                closestPlayer.behaviorType = AIAgent.EBehaviorType.Steering;
                closestPlayer._ePlayerState = PlayerState.EPlayerState.Targeted;
                closestPlayer.gameObject.AddComponent<Flee>();
                closestPlayer.gameObject.AddComponent<FaceAway>();
            }
            else
            {
                foreach (var player in _players)
                {
                    player.UnTrackTarget();
                    if (player.GetComponent<Stop>() != null)
                    {
                        Destroy(player.GetComponent<Stop>());
                    }
                    if (player.GetComponent<Seek>() != null)
                    {
                        Destroy(player.GetComponent<Seek>());
                    }
                    if (player.GetComponent<Arrive>() != null)
                    {
                        Destroy(player.GetComponent<Arrive>());
                    }
                    player.behaviorType = AIAgent.EBehaviorType.Steering;
                    player._ePlayerState = PlayerState.EPlayerState.Unfrozen;
                    player.gameObject.AddComponent<LookWhereYouAreGoing>();
                    player.gameObject.AddComponent<Wander>();
                    player.SetMaterial(_cMaterials[1]);
                }
                SetPursuer(_lastTarget);
            }
            return closestPlayer;
        }
        
        // Find closest unfrozen player who can unfreeze the frozen agent
        private AIAgent FindNearestTargetToUnfreeze(AIAgent frozenGuy)
        {
            AIAgent closestPlayer = null;
            var closestDistanceSqr = Mathf.Infinity;
            foreach (var player in _players)
            {
                if (player._ePlayerState == PlayerState.EPlayerState.Unfrozen)
                {
                    var distance = ToroidalDistance(frozenGuy.transform.position, player.transform.position);
                    // check if current comparison is smaller than the current closest player to target
                    if (distance < closestDistanceSqr)
                    {
                        closestDistanceSqr = distance;
                        closestPlayer = player;
                    }
                }
            }

            if (closestPlayer != null)
            {
                if (closestPlayer.GetComponent<Wander>() != null)
                {
                    Destroy(closestPlayer.GetComponent<Wander>());
                }
                if (closestPlayer.GetComponent<LookWhereYouAreGoing>() != null)
                {
                    Destroy(closestPlayer.GetComponent<LookWhereYouAreGoing>());
                }
                closestPlayer.TrackTarget(frozenGuy.transform);
                closestPlayer.behaviorType = AIAgent.EBehaviorType.Steering;
                closestPlayer._ePlayerState = PlayerState.EPlayerState.Rescuer;
                closestPlayer.gameObject.AddComponent<LookWhereYouAreGoing>();
                closestPlayer.gameObject.AddComponent<Arrive>();
                closestPlayer.SetMaterial(_cMaterials[3]);
            }
            return closestPlayer;
        }
        
        void CheckIfTagged()
        {
            if (_pursuer == null) return;
            if (!(Vector3.Distance(_pursuer.TargetPosition, _pursuer.transform.position) <= _tagDistance)) return;
            
            Destroy(_pursuer.trackedTarget.gameObject.GetComponent<Flee>());
            Destroy(_pursuer.trackedTarget.gameObject.GetComponent<FaceAway>());
            var target = _pursuer.trackedTarget.gameObject.GetComponent<AIAgent>();
            target.behaviorType = AIAgent.EBehaviorType.Kinematic;
            target.gameObject.AddComponent<Stop>();
            target.SetMaterial(_cMaterials[2]);
            target._ePlayerState = PlayerState.EPlayerState.Frozen;
            target.UnTrackTarget();

            _pursuer.UnTrackTarget();
            // Set both pursuer and chased agent to be targets of one another
            _pursuer.trackedTarget = FindNearestTargetToFreeze().transform;
                
            target.TrackTarget(FindNearestTargetToUnfreeze(target).transform);
        }

        bool CheckIfAllTagged()
        {
            var isGameEnded = _players.All(player => player._ePlayerState == PlayerState.EPlayerState.Frozen ||
                                             player._ePlayerState == PlayerState.EPlayerState.Tagged);
            return isGameEnded;
        }
        
        // Floating game object that indicates the pursuer's current target
        void SetConeToTarget()
        {
            if (_pursuer.trackedTarget != null)
            {
                _targetCone.position = _pursuer.trackedTarget.transform.position + new Vector3(0f, 3f, 0f);
            }
        }

        private float ToroidalDistance(Vector3 p1, Vector3 p2)
        {
            float dx = Mathf.Abs(p2.x - p1.x);
            float dz = Mathf.Abs(p2.z - p1.z);
 
            if (dx > 0.5f)
                dx = 1.0f - dx;
 
            if (dz > 0.5f)
                dz = 1.0f - dz;
 
            return Mathf.Sqrt(dx*dx + dz*dz);
        }
    }
}
