using UnityEngine;


namespace Path
{
    public class AgentManager : MonoBehaviour
    {
        public static AgentManager Instance;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);}
    }
}