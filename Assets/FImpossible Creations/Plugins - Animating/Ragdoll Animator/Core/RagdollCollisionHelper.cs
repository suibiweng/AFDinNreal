using System;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class RagdollProcessor
    {
        public class RagdollCollisionHelper : MonoBehaviour
        {
            public bool Colliding = false;
            public bool DebugLogs = false;
            public RagdollProcessor Parent { get; private set; }
            public PosingBone RagdollBone { get; private set; }
            public HumanBodyBones LimbID { get; private set; }

            public RagdollCollisionHelper Initialize(RagdollProcessor owner, PosingBone c)
            {
                Parent = owner;
                LimbID = HumanBodyBones.LastBone;
                RagdollBone = c;

                LatestEnterCollision = null;
                LatestExitCollision = null;

                if (c != null)
                {
                    #region Identify Limb

                    if (c == owner.GetPelvisBone()) LimbID = HumanBodyBones.Hips;
                    else if (c == owner.GetSpineStartBone()) LimbID = HumanBodyBones.Spine;
                    else if (c == owner.GetHeadBone()) LimbID = HumanBodyBones.Head;
                    else if (c == owner.GetLeftForeArm()) LimbID = HumanBodyBones.LeftLowerArm;
                    else if (c == owner.GetRightForeArm()) LimbID = HumanBodyBones.RightLowerArm;
                    else if (c == owner.GetLeftUpperArm()) LimbID = HumanBodyBones.LeftUpperArm;
                    else if (c == owner.GetRightUpperArm()) LimbID = HumanBodyBones.RightUpperArm;
                    else if (c == owner.GetLeftUpperLeg()) LimbID = HumanBodyBones.LeftUpperLeg;
                    else if (c == owner.GetLeftLowerLeg()) LimbID = HumanBodyBones.LeftLowerLeg;
                    else if (c == owner.GetRightUpperLeg()) LimbID = HumanBodyBones.RightUpperLeg;
                    else if (c == owner.GetRightLowerLeg()) LimbID = HumanBodyBones.RightLowerLeg;
                    else if (owner.HasChest()) if (c == owner.GetChestBone()) LimbID = HumanBodyBones.Chest;

                    #endregion
                }

                return this;
            }

            [NonSerialized] public List<Transform> EnteredCollisions = new List<Transform>();
            [NonSerialized] public List<Transform> EnteredSelfCollisions = null;
            [NonSerialized] public List<Transform> ignores = new List<Transform>();
            internal bool CollidesJustWithSelf = false;

            public Collision LatestEnterCollision { get; private set; }
            public ContactPoint LatestContact { get; private set; }

            private void OnCollisionEnter(Collision collision)
            {
                if (ignores.Contains(collision.transform)) return;
                if (DebugLogs) UnityEngine.Debug.Log(name + " collides with " + collision.transform.name);

                LatestEnterCollision = collision;
                if (collision.contactCount > 0) LatestContact = collision.GetContact(0);
                EnteredCollisions.Add(collision.transform);

                //if ( parent.IgnoreSelfCollision)
                //{
                //    if (EnteredSelfCollisions == null) EnteredSelfCollisions = new List<Transform>();
                //    if ( parent.Limbs.Contains(collision.transform) ) EnteredSelfCollisions.Add(collision.transform);
                //}

                Colliding = true;

                if (Parent != null)
                {
                    Parent.OnCollisionEnterEvent(this);
                }
            }

            public Collision LatestExitCollision { get; private set; }
            private void OnCollisionExit(Collision collision)
            {
                LatestExitCollision = collision;
                EnteredCollisions.Remove(collision.transform);

                if (Parent.IgnoreSelfCollision)
                {
                    if (EnteredSelfCollisions == null) EnteredSelfCollisions = new List<Transform>();
                    if (Parent.Limbs.Contains(collision.transform)) EnteredSelfCollisions.Remove(collision.transform);
                }

                if (EnteredCollisions.Count == 0) Colliding = false;

                if (Parent != null)
                {
                    Parent.OnCollisionExitEvent(this);
                }

            }
        }

        [Tooltip("Game object which has component with public methods like 'ERagColl(RagdollCollisionHelper c)' or 'ERagCollExit(RagdollCollisionHelper c)' to handle collisions")]
        public GameObject SendCollisionEventsTo = null;
        public bool SendOnlyOnFreeFall = true;

        private void OnCollisionExitEvent(RagdollCollisionHelper c)
        {
            if (SendOnlyOnFreeFall) if (FreeFallRagdoll == false) return;
            if (SendCollisionEventsTo == null) return;
            SendCollisionEventsTo.SendMessage("ERagColl", c, SendMessageOptions.DontRequireReceiver);
        }

        private void OnCollisionEnterEvent(RagdollCollisionHelper c)
        {
            if (SendOnlyOnFreeFall) if (FreeFallRagdoll == false) return;
            if (SendCollisionEventsTo == null) return;
            SendCollisionEventsTo.SendMessage("ERagCollExit", c, SendMessageOptions.DontRequireReceiver);
        }
    }
}