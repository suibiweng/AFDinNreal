using FIMSpace;
using System;
using UnityEngine;


namespace FIMSpace.FProceduralAnimation
{
    public partial class RagdollProcessor
    {
        // Transforms ----------------------

        public Transform BaseTransform;

        public Transform Pelvis;
        public Transform SpineStart;
        [Tooltip("Chest is optional bone, you can leave this field empty if you have small amount of spine bones")]
        public Transform Chest;
        public Transform Head;

        public Transform LeftUpperArm;
        public Transform LeftForeArm;

        public Transform RightUpperArm;
        public Transform RightForeArm;

        public Transform LeftUpperLeg;
        public Transform LeftLowerLeg;

        public Transform RightUpperLeg;
        public Transform RightLowerLeg;

        // Extra
        public Transform LeftFist;
        public Transform RightFist;
        public Transform LeftFoot;
        public Transform RightFoot;

        public Transform LeftShoulder;
        public Transform RightShoulder;


        public Vector3 PelvisLocalRight;
        public Vector3 PelvisLocalUp;
        public Vector3 PelvisLocalForward;
        public Vector3 PelvisToBase;
        public Vector3 LForearmToHand;
        public Vector3 RForearmToHand;
        public Vector3 HeadToTip;

        public void TryAutoFindReferences(Transform root)
        {
            BaseTransform = root;
            Animator a = root.GetComponentInChildren<Animator>();
            if (!a) a = root.GetComponentInParent<Animator>();

            if (a)
                if (a.isHuman)
                {
                    Pelvis = a.GetBoneTransform(HumanBodyBones.Hips);
                    SpineStart = a.GetBoneTransform(HumanBodyBones.Spine);
                    Chest = a.GetBoneTransform(HumanBodyBones.Chest);
                    Head = a.GetBoneTransform(HumanBodyBones.Head);

                    RightUpperArm = a.GetBoneTransform(HumanBodyBones.RightUpperArm);
                    RightForeArm = a.GetBoneTransform(HumanBodyBones.RightLowerArm);
                    LeftUpperArm = a.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                    LeftForeArm = a.GetBoneTransform(HumanBodyBones.LeftLowerArm);

                    RightUpperLeg = a.GetBoneTransform(HumanBodyBones.RightUpperLeg);
                    RightLowerLeg = a.GetBoneTransform(HumanBodyBones.RightLowerLeg);
                    LeftUpperLeg = a.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                    LeftLowerLeg = a.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
                }

            if (Pelvis == null) Pelvis = FTransformMethods.FindChildByNameInDepth("pelvis", root);
            if (Pelvis == null) Pelvis = FTransformMethods.FindChildByNameInDepth("hips", root);
            if (SpineStart == null) SpineStart = FTransformMethods.FindChildByNameInDepth("spine", root);
            if (Chest == null) Chest = FTransformMethods.FindChildByNameInDepth("chest", root);
            if (Head == null) Head = FTransformMethods.FindChildByNameInDepth("head", root);

            if (BaseTransform != null)
                if (LeftUpperArm == null)
                {
                    LeftUpperArm = FTransformMethods.FindChildByNameInDepth("arm", root);
                    if (LeftUpperArm != null)
                        if (BaseTransform.InverseTransformPoint(LeftUpperArm.position).x > 0.001f)
                            LeftUpperArm = FTransformMethods.FindChildByNameInDepth("left", root);
                }

            if (RightUpperArm == null) RightUpperArm = FTransformMethods.FindChildByNameInDepth("right", root);
            if (LeftUpperLeg == null) LeftUpperLeg = FTransformMethods.FindChildByNameInDepth("leg", root);
            if (RightUpperLeg == null) RightUpperLeg = FTransformMethods.FindChildByNameInDepth("leg", root);
        }

        void RefreshPelvisGuides()
        {
            if (Pelvis)
            {
                PelvisLocalRight = Pelvis.InverseTransformDirection(BaseTransform.right);
                PelvisLocalUp = Pelvis.InverseTransformDirection(BaseTransform.up);
                PelvisLocalForward = Pelvis.InverseTransformDirection(BaseTransform.forward);
                PelvisToBase = Pelvis.transform.InverseTransformPoint(BaseTransform.position);
                pelvisAnimatorPosition = posingPelvis.transform.position;
                pelvisAnimatorRotation = posingPelvis.transform.rotation;
            }

            if (LeftForeArm) if (LeftForeArm.childCount > 0) LForearmToHand = LeftForeArm.GetChild(0).localPosition;
            if (RightForeArm) if (RightForeArm.childCount > 0) RForearmToHand = RightForeArm.GetChild(0).localPosition;
            if (Head) if (Head.childCount > 0) HeadToTip = Head.GetChild(0).localPosition;
        }

        // Pose references ----------------

        public PosingBone GetPelvisBone() { return posingPelvis; }
        public PosingBone GetHeadBone() { return posingHead; }
        public PosingBone GetSpineStartBone() { return posingSpineStart; }
        public bool HasChest() { return posingChest != null; }
        public PosingBone GetChestBone() { return posingChest != null ? posingChest : posingSpineStart; }
        public PosingBone GetRightForeArm() { return posingRightForeArm; }
        public PosingBone GetLeftUpperArm() { return posingLeftUpperArm; }
        public PosingBone GetRightUpperArm() { return posingRightUpperArm; }
        public PosingBone GetLeftForeArm() { return posingLeftForeArm; }
        public PosingBone GetLeftLowerLeg() { return posingLeftLowerLeg; }
        public PosingBone GetLeftUpperLeg() { return posingLeftUpperLeg; }
        public PosingBone GetRightUpperLeg() { return posingRightUpperLeg; }
        public PosingBone GetRightLowerLeg() { return posingRightLowerLeg; }
        public Transform GetRagdolledPelvis() { return posingPelvis.transform; }
        public Transform GetAnimatorPelvis() { return posingPelvis.visibleBone; }


        //private PosingBone posingRootSkelBone;
        private PosingBone posingPelvis;
        private PosingBone posingSpineStart;
        private PosingBone posingChest;
        private PosingBone posingHead;

        private PosingBone posingLeftUpperArm;
        private PosingBone posingLeftForeArm;
        private PosingBone posingRightUpperArm;
        private PosingBone posingRightForeArm;

        private PosingBone posingLeftUpperLeg;
        private PosingBone posingLeftLowerLeg;
        private PosingBone posingRightUpperLeg;
        private PosingBone posingRightLowerLeg;

        private PosingBone posingLeftFist = null;
        private PosingBone posingLeftFoot = null;
        private PosingBone posingRightFist = null;
        private PosingBone posingRightFoot = null;

        private PosingBone posingLeftShoulder = null;
        private PosingBone posingRightShoulder = null;

        public void SetRagdollTargetBones(Transform dummy, Transform pelvis = null, Transform spineStart = null, Transform chest = null, Transform head = null,
            Transform leftUpperArm = null, Transform leftForeArm = null, Transform rightUpperArm = null, Transform rightForeArm = null,
            Transform leftUpperLeg = null, Transform leftLowerLeg = null, Transform rightUpperLeg = null, Transform rightLowerLeg = null,
            Transform leftShoulder = null, Transform rightShoulder = null
            )
        {
            posingPelvis = new PosingBone(pelvis, this);
            posingSpineStart = new PosingBone(spineStart, this);

            if (chest)
            {
                if (chest.GetComponent<Rigidbody>() != null) posingChest = new PosingBone(chest, this);
            }

            posingHead = new PosingBone(head, this);

            if (LeftShoulder) posingLeftShoulder = new PosingBone(leftShoulder, this);
            posingLeftUpperArm = new PosingBone(leftUpperArm, this);
            posingLeftForeArm = new PosingBone(leftForeArm, this);

            if (rightShoulder) posingRightShoulder = new PosingBone(rightShoulder, this);
            posingRightUpperArm = new PosingBone(rightUpperArm, this);
            posingRightForeArm = new PosingBone(rightForeArm, this);

            posingLeftUpperLeg = new PosingBone(leftUpperLeg, this);
            posingLeftLowerLeg = new PosingBone(leftLowerLeg, this);
            posingRightUpperLeg = new PosingBone(rightUpperLeg, this);
            posingRightLowerLeg = new PosingBone(rightLowerLeg, this);


            #region Completing Posing Bones

            posingPelvis.child = posingSpineStart;

            if (chest != null)
            {
                posingSpineStart.child = posingChest;
                posingChest.child = posingHead;
            }
            else
            {
                posingSpineStart.child = posingHead;
            }

            posingHead.child = posingLeftUpperArm;

            if (leftShoulder)
            {
                posingHead.child = posingLeftShoulder;
                posingLeftShoulder.child = posingLeftUpperArm;
            }

            posingLeftUpperArm.child = posingLeftForeArm;
            posingLeftForeArm.child = posingRightUpperArm;

            if (RightShoulder)
            {
                posingLeftForeArm.child = posingRightShoulder;
                posingRightShoulder.child = posingRightUpperArm;
            }

            posingRightUpperArm.child = posingRightForeArm;
            posingRightForeArm.child = posingLeftUpperLeg;

            posingLeftUpperLeg.child = posingLeftLowerLeg;
            posingLeftLowerLeg.child = posingRightUpperLeg;

            posingRightUpperLeg.child = posingRightLowerLeg;

            PosingBone latestChain = posingRightLowerLeg;

            // Fists and foots detection

            if (leftForeArm.childCount > 0)
            {
                Transform ch = leftForeArm.GetChild(0);
                if (LeftFist) ch = LeftFist;

                Joint j = ch.GetComponent<Joint>();
                if (j)
                {
                    ch = FTransformMethods.FindChildByNameInDepth(ch.name, dummy);
                    posingLeftFist = new PosingBone(ch, this);
                    latestChain.child = posingLeftFist;
                    latestChain = posingLeftFist;
                }
            }

            if (rightForeArm.childCount > 0)
            {
                Transform ch = rightForeArm.GetChild(0);
                if (RightFist) ch = RightFist;

                Joint j = ch.GetComponent<Joint>();
                if (j)
                {
                    ch = FTransformMethods.FindChildByNameInDepth(ch.name, dummy);
                    posingRightFist = new PosingBone(ch, this);
                    latestChain.child = posingRightFist;
                    latestChain = posingRightFist;
                }
            }

            if (LeftLowerLeg.childCount > 0)
            {
                Transform ch = LeftLowerLeg.GetChild(0);
                Joint j = ch.GetComponent<Joint>();
                if (j)
                {
                    ch = FTransformMethods.FindChildByNameInDepth(ch.name, dummy);
                    posingLeftFoot = new PosingBone(ch, this);
                    latestChain.child = posingLeftFoot;
                    latestChain = posingLeftFoot;
                }
            }

            if (rightLowerLeg.childCount > 0)
            {
                Transform ch = rightLowerLeg.GetChild(0);
                Joint j = ch.GetComponent<Joint>();
                if (j)
                {
                    ch = FTransformMethods.FindChildByNameInDepth(ch.name, dummy);
                    posingRightFoot = new PosingBone(ch, this);
                    latestChain.child = posingRightFoot;
                    latestChain = posingRightFoot;
                }
            }

            latestChain.child = null;

            #endregion

        }

        public void SetAnimationPoseBones(Transform pelvis = null, Transform spineStart = null, Transform chest = null, Transform head = null,
        Transform leftUpperArm = null, Transform leftForeArm = null, Transform rightUpperArm = null, Transform rightForeArm = null,
        Transform leftUpperLeg = null, Transform leftLowerLeg = null, Transform rightUpperLeg = null, Transform rightLowerLeg = null
        )
        {
            posingPelvis.SetVisibleBone(pelvis);
            if (chest != null && posingChest != null) posingChest.SetVisibleBone(chest);
            posingSpineStart.SetVisibleBone(spineStart);
            posingHead.SetVisibleBone(head);

            posingLeftUpperArm.SetVisibleBone(leftUpperArm);
            posingLeftForeArm.SetVisibleBone(leftForeArm);
            posingRightUpperArm.SetVisibleBone(rightUpperArm);
            posingRightForeArm.SetVisibleBone(rightForeArm);

            posingLeftUpperLeg.SetVisibleBone(leftUpperLeg);
            posingLeftLowerLeg.SetVisibleBone(leftLowerLeg);
            posingRightUpperLeg.SetVisibleBone(rightUpperLeg);
            posingRightLowerLeg.SetVisibleBone(rightLowerLeg);

            //SetAnimationRefBones();
        }

        public void SetAnimationRefBones(Transform pelvis = null, Transform spineStart = null, Transform chest = null, Transform head = null,
    Transform leftUpperArm = null, Transform leftForeArm = null, Transform rightUpperArm = null, Transform rightForeArm = null,
    Transform leftUpperLeg = null, Transform leftLowerLeg = null, Transform rightUpperLeg = null, Transform rightLowerLeg = null
    )
        {
            posingPelvis.customRefBone = pelvis;
            posingSpineStart.customRefBone = spineStart;
            if (posingChest != null) posingChest.customRefBone = chest;
            posingHead.customRefBone = head;

            posingLeftUpperArm.customRefBone = leftUpperArm;
            posingLeftForeArm.customRefBone = leftForeArm;
            posingRightUpperArm.customRefBone = rightUpperArm;
            posingRightForeArm.customRefBone = rightForeArm;

            posingLeftUpperLeg.customRefBone = leftUpperLeg;
            posingLeftLowerLeg.customRefBone = leftLowerLeg;
            posingRightUpperLeg.customRefBone = rightUpperLeg;
            posingRightLowerLeg.customRefBone = rightLowerLeg;
        }



        /// <summary>
        /// Checking if fists/foots was generated in the older version way and correcting it for the curren version approach
        /// </summary>
        internal void BackCompabilityCheck()
        {
            if (BonesSetupMode == EBonesSetupMode.HumanoidLimbs)
            {
                Joint findChild;

                if (!LeftFist)
                    if (LeftForeArm)
                    {
                        findChild = _BackComp_FindChildJoint(LeftForeArm);
                        if (findChild != null) if (findChild.transform != LeftForeArm) LeftFist = findChild.transform;
                    }

                if (!RightFist)
                    if (RightForeArm)
                    {
                        findChild = _BackComp_FindChildJoint(RightForeArm);
                        if (findChild != null) if (findChild.transform != RightForeArm) RightFist = findChild.transform;
                    }


                if (!LeftFoot)
                    if (LeftLowerLeg)
                    {
                        findChild = _BackComp_FindChildJoint(LeftLowerLeg);
                        if (findChild != null) if (findChild.transform != LeftLowerLeg) LeftFoot = findChild.transform;
                    }


                if (!RightFoot)
                    if (RightLowerLeg)
                    {
                        findChild = _BackComp_FindChildJoint(RightLowerLeg);
                        if (findChild != null) if (findChild.transform != RightLowerLeg) RightFoot = findChild.transform;
                    }
            }
        }

        private Joint _BackComp_FindChildJoint(Transform parent)
        {
            Joint j = null;

            for (int i = 0; i < parent.childCount; i++)
            {
                j = parent.GetChild(i).GetComponent<Joint>();
                if (j != null) return j;
            }

            return j;
        }


    }
}