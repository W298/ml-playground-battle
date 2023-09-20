using UnityEngine;

namespace Battle
{
    public class Arrow : MonoBehaviour
    {
        void Start()
        {
            GetComponent<Rigidbody>().AddForce(-transform.up * 100f, ForceMode.VelocityChange);
        }

        private void OnCollisionEnter(Collision other)
        {
            var rb = GetComponent<Rigidbody>();
            
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }
}
