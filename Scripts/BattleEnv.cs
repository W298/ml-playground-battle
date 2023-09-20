using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.MLAgents;
using UnityEngine;

namespace Battle
{
    public class BattleEnv : MonoBehaviour
    {
        private List<BattleAgent> m_agentList;
        private Dictionary<BattleAgent.Team, SimpleMultiAgentGroup> m_groupDict;
        private Dictionary<BattleAgent.Team, int> m_score;

        public TMP_Text RemainStepText;
        public TMP_Text RedScoreText;
        public TMP_Text YellowScoreText;
            
        public int MaxEnvStep = 1000;
        [HideInInspector] public int CurrentTimeStep = 0;

        private void Start()
        {
            m_agentList = FindObjectsOfType<BattleAgent>().ToList();

            m_groupDict = new Dictionary<BattleAgent.Team, SimpleMultiAgentGroup>();
            
            m_score = new Dictionary<BattleAgent.Team, int>
            {
                { BattleAgent.Team.Red, 0 },
                { BattleAgent.Team.Yellow, 0 }
            };

            var redTeam = new SimpleMultiAgentGroup();
            var yellowTeam = new SimpleMultiAgentGroup();

            m_groupDict.Add(BattleAgent.Team.Red, redTeam);
            m_groupDict.Add(BattleAgent.Team.Yellow, yellowTeam);

            foreach (var battleAgent in m_agentList)
            {
                m_groupDict[battleAgent.AgentTeam].RegisterAgent(battleAgent);
            }

            ResetEnv();
        }

        private void FixedUpdate()
        {
            CurrentTimeStep += 1;
            if (CurrentTimeStep >= MaxEnvStep && MaxEnvStep > 0)
            {
                TimeOver();
            }

            RemainStepText.text = (MaxEnvStep - CurrentTimeStep).ToString();
        }

        private void AddScore(BattleAgent.Team team)
        {
            m_score[team]++;
            
            RedScoreText.text = m_score[BattleAgent.Team.Red].ToString("00");
            YellowScoreText.text = m_score[BattleAgent.Team.Yellow].ToString("00");
        }

        private void TimeOver()
        {
            var redList = m_agentList.Where(agent => agent.AgentTeam == BattleAgent.Team.Red).ToList();
            var yellowList = m_agentList.Where(agent => agent.AgentTeam == BattleAgent.Team.Yellow).ToList();

            var redHpSum = redList.Sum(a => a.Hp);
            var yellowHpSum = yellowList.Sum(a => a.Hp);

            var redHpMean = redHpSum / redList.Count;
            var yellowHpMean = yellowHpSum / yellowList.Count;

            if (Mathf.Abs(redHpSum - yellowHpSum) <= 0.0001f)
            {
                var redCumReward = redList.Sum(a => a.GetCumulativeReward());
                var yellowCumReward = yellowList.Sum(a => a.GetCumulativeReward());

                if (Mathf.Abs(redCumReward - yellowCumReward) <= 0.0001f) // Real Draw!
                {
                    foreach (var redAgent in redList)
                        redAgent.SetReward(0);
                    foreach (var yellowAgent in yellowList)
                        yellowAgent.SetReward(0);

                    m_groupDict[BattleAgent.Team.Red].SetGroupReward(0);
                    m_groupDict[BattleAgent.Team.Yellow].SetGroupReward(0);

                    Debug.Log("[TimeOver] Draw");
                }
                else if (redCumReward > yellowCumReward) // Hp Identical but Red Win!
                {
                    TimeOverRedWin(redList, yellowList, redHpMean, yellowHpMean);
                    AddScore(BattleAgent.Team.Red);
                }
                else // Hp Identical but Yellow Win!
                {
                    TimeOverYellowWin(redList, yellowList, redHpMean, yellowHpMean);
                    AddScore(BattleAgent.Team.Yellow);
                }
            }
            else if (redHpSum > yellowHpSum) // Red Win!
            {
                TimeOverRedWin(redList, yellowList, redHpMean, yellowHpMean);
                AddScore(BattleAgent.Team.Red);
            }
            else // Yellow Win!
            {
                TimeOverYellowWin(redList, yellowList, redHpMean, yellowHpMean);
                AddScore(BattleAgent.Team.Yellow);
            }

            m_groupDict[BattleAgent.Team.Red].GroupEpisodeInterrupted();
            m_groupDict[BattleAgent.Team.Yellow].GroupEpisodeInterrupted();

            ResetEnv();
        }

        private void TimeOverRedWin(List<BattleAgent> redList, List<BattleAgent> yellowList, float redHpMean,
            float yellowHpMean)
        {
            // Set Each Agent Reward
            foreach (var redAgent in redList)
                redAgent.SetReward(redAgent.Hp);
            foreach (var yellowAgent in yellowList)
                yellowAgent.SetReward(-1 + yellowAgent.Hp);

            // Set Group Reward
            m_groupDict[BattleAgent.Team.Red].SetGroupReward(redHpMean);
            m_groupDict[BattleAgent.Team.Yellow].SetGroupReward(-1 + yellowHpMean);

            Debug.Log("[TimeOver] Red Win");
            Debug.Log((redHpMean) + " / " + (-1 + yellowHpMean));
        }

        private void TimeOverYellowWin(List<BattleAgent> redList, List<BattleAgent> yellowList, float redHpMean,
            float yellowHpMean)
        {
            // Set Each Agent Reward
            foreach (var redAgent in redList)
                redAgent.SetReward(-1 + redAgent.Hp);
            foreach (var yellowAgent in yellowList)
                yellowAgent.SetReward(yellowAgent.Hp);

            // Set Group Reward
            m_groupDict[BattleAgent.Team.Red].SetGroupReward(-1 + redHpMean);
            m_groupDict[BattleAgent.Team.Yellow].SetGroupReward(yellowHpMean);

            Debug.Log("[TimeOver] Yellow Win");
            Debug.Log((-1 + redHpMean) + " / " + (yellowHpMean));
        }

        private void ResetEnv()
        {
            CurrentTimeStep = 0;

            foreach (var battleAgent in m_agentList)
            {
                battleAgent.ResetSelf();
            }
        }

        public void OnKilled(BattleAgent killer, BattleAgent prey)
        {
            var killerTeamList = m_agentList.Where(agent => agent.AgentTeam == killer.AgentTeam).ToList();
            var killerTeamHpSum = killerTeamList.Sum(a => a.Hp);

            var allKilled =
                m_agentList.Count(agent => agent.AgentTeam == prey.AgentTeam && agent != prey && !agent.IsDead) == 0;

            if (allKilled)
            {
                var winReward = 1 + (killerTeamHpSum / killerTeamList.Count) * 2f;
                m_groupDict[killer.AgentTeam].SetGroupReward(winReward);
                m_groupDict[prey.AgentTeam].SetGroupReward(-winReward);

                Debug.Log("[All-Killed] " + killer.AgentTeam);
                Debug.Log(winReward);

                m_groupDict[BattleAgent.Team.Red].EndGroupEpisode();
                m_groupDict[BattleAgent.Team.Yellow].EndGroupEpisode();

                AddScore(killer.AgentTeam);

                ResetEnv();
                return;
            }

            prey.IsDead = true;
        }

        public bool CheckPosition(Vector3 position)
        {
            return m_agentList.All(battleAgent => !(Vector3.Distance(battleAgent.transform.position, position) <= 5f));
        }
    }
}