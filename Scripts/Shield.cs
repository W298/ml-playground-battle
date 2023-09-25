using UnityEngine;

namespace Battle
{
    public class Shield : MonoBehaviour
    {
        private KnightAgent m_knightAgent;
        public bool IsFront = true;

        private void Start()
        {
            m_knightAgent = transform.parent.GetComponent<KnightAgent>();
        }

        private void OnCollisionEnter(Collision other)
        {
            if (other.collider.CompareTag("Sword") && other.collider.enabled)
            {
                var attacker = other.collider.transform.parent.GetComponent<BattleAgent>();
                if (attacker == null || m_knightAgent == null) return;
                if (attacker.AgentTeam == m_knightAgent.AgentTeam) return;
                if (!attacker.IsAttackValid) return;
                
                attacker.IsAttackValid = false;

                var force = 6 * transform.forward * (IsFront ? 1 : 0.5f);
                attacker.Pushed(force);
                m_knightAgent.Pushed(-force * 0.5f);
                
                m_knightAgent.AddReward(0.02f);
            }
        }
    }
}