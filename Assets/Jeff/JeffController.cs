using UnityEngine;
using UnityEngine.InputSystem;

namespace Jeff
{
    public enum JeffState
    {
        Default = 0,
        Attack = 1,
        Hit = 2,
    }

    public partial class JeffController : MonoBehaviour
    {
        public Vector2 DesiredMovementDirection { get; private set; }
        public bool WantsToJump { get; private set; }
        public JeffState State { get; private set; } = JeffState.Default;

        public void OnMove(InputValue value)
        {
            DesiredMovementDirection = value.Get<Vector2>();
        }

        public void OnJump(InputValue value)
        {
            WantsToJump = value.Get<float>() > 0.5f;
            if (WantsToJump && State == JeffState.Default && _jumpsLeft > 0)
            {
                _jumpsLeft--;
                _timeOfLastJump = Time.fixedTime;
            }
        }

        public void OnAttack(InputValue value)
        {
            EnterAttackState();
        }
    }
}