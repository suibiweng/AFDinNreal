using FIMSpace.FEditor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{

    [System.Serializable]
    public class RagdollGenerator
    {
        public Transform BaseTransform = null;
        bool generateRagdoll = false;
        bool characterJoints = false;
        float ragdollScale = 1f;
        float ragdollBounciness = 0.0f;
        float ragdollDamper = 0f;
        float ragdollDrag = .5f;
        float ragdollAngularDrag = 1.25f;
        float ragdollSprings = 0f;
        float ragdollMassToDistribute = 65f;
        float ragdollMass = 1f;
        float projDist = .05f;
        float projAngle = 60f;
        RigidbodyInterpolation ragdInterpol = RigidbodyInterpolation.Interpolate;
        bool enableCollision = false;
        bool enablePreProcessing = false;
        public enum tweakRagd { None, Position, Scale };
        public tweakRagd ragdollTweak = tweakRagd.None;

        bool generateFists = false;
        bool generateFoots = false;
        bool generateShoulders = false;
        bool useSymmetry = false;

        public bool forceUpdateGenerate = false;

        public void Tab_RagdollGenerator(RagdollProcessor proc, bool assignProcessorBones)
        {
            if (proc.IsPreGeneratedDummy)
            {
                EditorGUILayout.HelpBox("Pre generated ragdoll is not available for ragdoll generator / colliders adjustements, generate ragdoll and do adjustements before pre generating", MessageType.Info);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 116;

                EditorGUI.BeginChangeCheck();
                generateRagdoll = EditorGUILayout.Toggle("Generate Ragdoll", generateRagdoll, new GUILayoutOption[] { GUILayout.Width(160) });
                EditorGUIUtility.labelWidth = 0;

                if (!generateRagdoll) GUI.enabled = false;
                if (GUILayout.Button(characterJoints ? "Now Using Character Joints" : "Now Using Configurable Joints")) characterJoints = !characterJoints;
                EditorGUILayout.EndHorizontal();

                EditorGUIUtility.labelWidth = 200;
                ragdollMassToDistribute = EditorGUILayout.FloatField("Target Mass of Whole Model:", ragdollMassToDistribute);
                ragdollScale = EditorGUILayout.Slider("Scale Ragdoll", ragdollScale, 0.35f, 1.5f);

                GUILayout.Space(5);
                ragdollDrag = EditorGUILayout.Slider("Drag for all Rigidbodies", ragdollDrag, 0f, 1f);
                ragdollAngularDrag = EditorGUILayout.Slider("Angular Drag for Rigidbodies", ragdollAngularDrag, 0f, 3f);
                GUILayout.Space(5);

                ragdInterpol = (RigidbodyInterpolation)EditorGUILayout.EnumPopup("Rigidbodies Interpolation", ragdInterpol);
                EditorGUIUtility.labelWidth = 0;

                if (proc.LeftFist || proc.RightFist) generateFists = true; else generateFists = false;
                if (proc.LeftFoot || proc.RightFoot) generateFoots = true; else generateFoots = false;
                if (proc.LeftShoulder || proc.RightShoulder) generateShoulders = true; else generateShoulders = false;

                GUILayout.Space(5);


                if (!generateRagdoll) GUI.enabled = true;

                GUILayout.Space(5);

                EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxStyle);


                EditorGUILayout.BeginHorizontal();
                ragdollTweak = (tweakRagd)EditorGUILayout.EnumPopup("Tweak Ragdoll Colliders", ragdollTweak);

                if (proc.BonesSetupMode == RagdollProcessor.EBonesSetupMode.HumanoidLimbs)
                {
                    GUILayout.Space(4);
                    EditorGUIUtility.labelWidth = 44;
                    useSymmetry = EditorGUILayout.Toggle(new GUIContent("Symm", "(EXPERIMENTAL) Use Symmetry for tweaking colliders"), useSymmetry, GUILayout.Width(64));
                    EditorGUIUtility.labelWidth = 0;
                }

                EditorGUILayout.EndHorizontal();


                GUILayout.Space(5);

                EditorGUILayout.HelpBox("Remember about correct collision LAYERS on bone transforms and on the movement controller!", MessageType.None);

                if (EditorGUI.EndChangeCheck() || forceUpdateGenerate)
                    if (generateRagdoll)
                        if (proc.RagdollDummyBase == null)
                        {
                            forceUpdateGenerate = false;
                            if (assignProcessorBones)
                            {
                                SetAllBoneReferences(proc);
                            }

                            UpdateOrGenerateRagdoll(proc, characterJoints, ragdollScale, ragdollBounciness, ragdollDamper, ragdollSprings, ragdollDrag, ragdollAngularDrag, ragdollMassToDistribute, true, projAngle, projDist, enableCollision, enablePreProcessing, false, ragdInterpol);
                        }

                GUILayout.Space(3);
                if (GUILayout.Button("Remove Ragdoll Components on Bones")) { RemoveRagdoll(proc); generateRagdoll = false; }

                EditorGUILayout.EndVertical();
            }

        }

        public void Ragdoll_IgnoreCollision(Transform a, Transform b)
        {
            CapsuleCollider ca, cb;
            ca = a.GetComponent<CapsuleCollider>();
            cb = b.GetComponent<CapsuleCollider>();

            if (ca != null && cb != null) Physics.IgnoreCollision(ca, cb);
        }

        public void Ragdoll_ComputeArmAxis(Transform baseTransform, Transform bone, Transform child, ref Vector3 f, ref Vector3 r, ref Vector3 u)
        {
            if (child == null)
                f = bone.transform.InverseTransformDirection(bone.position - bone.GetChild(0).position).normalized;
            else
                f = bone.transform.InverseTransformDirection(bone.position - child.position).normalized;

            r = -bone.transform.InverseTransformDirection(baseTransform.forward);
            u = Vector3.Cross(f, r);
        }

        public void Ragdoll_ComputeAxis(Transform baseTransform, Transform bone, ref Vector3 f, ref Vector3 r, ref Vector3 u)
        {
            r = bone.transform.InverseTransformDirection(baseTransform.right);
            f = bone.transform.InverseTransformDirection(baseTransform.forward);
            u = bone.transform.InverseTransformDirection(baseTransform.up);
        }


        public void UpdateOrGenerateRagdoll(RagdollProcessor proc, bool characterJoints = true, float scale = 1f, float bounciness = 0f, float damper = 0f, float spring = 0f, float drag = 0.5f, float aDrag = 1f, float massToDistr = 65f, bool projection = true, float projAngle = 90, float projDistance = 0.05f, bool enCollision = false, bool preProcessing = false, bool addToFoots = false, RigidbodyInterpolation interpolation = RigidbodyInterpolation.Interpolate)
        {
            if (proc.BonesSetupMode == RagdollProcessor.EBonesSetupMode.CustomLimbs)
            {
                if (PelvisBone == null || BoneChains.Count < 3)
                {
                    UnityEngine.Debug.Log("[Ragdoll Generator] No bones to generate ragdoll!");
                    return;
                }
            }
            else
            {
                if (LeftUpperArm == null || PelvisBone == null || SpineRoot == null)
                {
                    UnityEngine.Debug.Log("[Ragdoll Generator] No bones to generate ragdoll!");
                    return;
                }
            }

            float totalMass = massToDistr;
            Transform baseTransform = proc.BaseTransform;
            Transform t;

            if (proc.BonesSetupMode == RagdollProcessor.EBonesSetupMode.CustomLimbs)
            {

                #region Preparing ragdoll dummy beginning bones


                #region First chain is always hips/pelvis single bone and it's the anchor rigidbody

                RagdollProcessor.RagdollBoneSetup bone = BoneChains[0].BoneSetups[0];
                t = bone.t;

                var pelvisChildr = GetRagdollBonesAttachedWith(t, proc);

                if (pelvisChildr.Count == 0)
                {
                    UnityEngine.Debug.Log("[Ragdoll Generator] No pelvis child bones! Can't generate ragdoll!");
                    return;
                }

                #endregion


                #region Finding limb which is not pointing towards down the most but most upwards - to define next spine transform and redirect pelvis towards it

                Vector3 limbDir = Vector3.forward;
                Transform nonLeg = pelvisChildr[0];
                float bestDot = -111f;
                for (int i = 0; i < pelvisChildr.Count; i++)
                {
                    Vector3 diff = pelvisChildr[i].position - t.position;
                    float dot = Vector3.Dot(diff.normalized, BaseTransform.up);
                    if (dot > bestDot)
                    {
                        bestDot = dot;
                        nonLeg = pelvisChildr[i];
                        limbDir = diff.normalized;
                    }
                }

                #endregion


                //Ragdoll_RefreshComponents(characterJoints, bone, limbDir, false, bounciness, damper, drag, aDrag, projection, projAngle, projDistance, enCollision, preProcessing);
                //t.RagdollBody().mass = totalMass * 0.16f;
                SetupPelvisBone(proc, bounciness, damper, drag, aDrag, massToDistr, projection, projAngle, projDist, enCollision, preProcessing, interpolation);

                //Vector3 pelvisToNonLeg = nonLeg.position - t.position;
                Vector3 pelvisToNonLegLocal = t.InverseTransformVector(nonLeg.position - t.position);
                Ragdoll_AdjustCollider(bone, pelvisToNonLegLocal * 0.4f, pelvisToNonLegLocal, pelvisToNonLegLocal.magnitude * 0.35f * scale, pelvisToNonLegLocal.magnitude * 0.75f * scale, interpolation);


                #endregion


                for (int i = 1; i < BoneChains.Count; i++)
                {
                    var chain = BoneChains[i];

                    Transform chainParent = chain.GetAttachTransform(proc);
                    Rigidbody chainParentRig = chainParent.GetComponent<Rigidbody>();

                    if (chainParent)
                    {

                        for (int b = 0; b < chain.BoneSetups.Count; b++)
                        {
                            bone = chain.BoneSetups[b];
                            t = bone.t;

                            Ragdoll_RefreshComponents(characterJoints, bone, limbDir, true, bounciness, damper, drag, aDrag, projection, projAngle, projDistance, enCollision, preProcessing);
                            t.RagdollBody().mass = bone.MassPercentage * totalMass;

                            Transform next = null, pre = null;
                            if (b < chain.BoneSetups.Count - 1) next = chain.BoneSetups[b + 1].t;
                            else if (bone.t.childCount > 0) next = bone.t.GetChild(0);

                            if (b > 0) pre = chain.BoneSetups[b - 1].t;
                            else pre = bone.t.parent;

                            Vector3 toNext, toNextLocal, preTo, preToLocal;
                            Vector3 targetDir = Vector3.forward, targetDirLocal = Vector3.forward;
                            float targetLength;

                            #region Compute collider helper direction

                            if (next != null)
                            {
                                toNext = (next.position - t.position);
                                toNextLocal = t.InverseTransformDirection(toNext);

                                targetDir = toNext;
                                targetDirLocal = toNextLocal;
                            }

                            if (pre != null)
                            {
                                preTo = (t.position - pre.position);
                                preToLocal = t.InverseTransformDirection(preTo);

                                if (next == null)
                                {
                                    targetDir = preTo;
                                    targetDirLocal = preToLocal;
                                }
                            }

                            targetLength = targetDir.magnitude;

                            #endregion

                            Ragdoll_AdjustCollider(bone, targetDirLocal * 0.5f, targetDirLocal * scale, targetLength * 1f * scale, targetLength * 0.125f * scale, interpolation);
                        }

                        Vector3 f = Vector3.zero, r = Vector3.zero, u = Vector3.zero;

                        // Adjusting rigidbody and joints connections
                        for (int b = 0; b < chain.BoneSetups.Count; b++)
                        {
                            bone = chain.BoneSetups[b];
                            Joint j = bone.t.GetComponent<Joint>();

                            if (j == null) continue;

                            // Attach first bone to the parent
                            if (b == 0)
                                j.connectedBody = chainParentRig;
                            else // Attach this bone to the previous rigidbody
                                j.connectedBody = chain.BoneSetups[b - 1].t.GetComponent<Rigidbody>();

                            Ragdoll_Joint(chain.BoneSetups[b].t, -90f, 90f, 90f, 90f, spring, damper);
                            Ragdoll_ComputeAxis(baseTransform, chain.BoneSetups[b].t, ref f, ref r, ref u);
                            Ragdoll_JointAxis(chain.BoneSetups[b].t, r, null, r, f, u);
                        }

                    }

                }

            }
            else
            {

                #region Humanoid Fast Mode Code


                if (!generateFists)
                {
                    if (LeftForearm)
                        if (LeftForearm.childCount > 0) Ragdoll_RemoveFrom(LeftForearm.GetLimbChild());
                    if (RightForearm)
                        if (RightForearm.childCount > 0) Ragdoll_RemoveFrom(RightForearm.GetLimbChild());
                }

                if (!generateFoots)
                {
                    if (LeftLowerLeg)
                        if (LeftLowerLeg.childCount > 0) Ragdoll_RemoveFrom(LeftLowerLeg.GetLimbChild());
                    if (RightLowerLeg)
                        if (RightLowerLeg.childCount > 0) Ragdoll_RemoveFrom(RightLowerLeg.GetLimbChild());
                }

                if (!generateShoulders)
                {
                    if (LeftUpperArm) if (LeftUpperArm.parent.childCount < 3) Ragdoll_RemoveFrom(LeftUpperArm.parent);
                    if (RightUpperArm) if (RightUpperArm.parent.childCount < 3) Ragdoll_RemoveFrom(RightUpperArm.parent);
                }

                Transform nck = Head.parent;
                Transform Neck = Head.parent;

                Vector3 r = new Vector3(), u = new Vector3(), f = new Vector3();

                Transform chst = Chest;
                if (chst == null) chst = Head.parent;

                // Pelvis to head
                //Ragdoll_RefreshComponents(characterJoints, PelvisBone, chst.position - PelvisBone.position, false, bounciness, damper, drag, aDrag, projection, projAngle, projDistance, enCollision, preProcessing);
                //t = PelvisBone;
                //PelvisBone.RagdollBody().mass = 0.16f * massToDistr;
                //PelvisBone.RagdollCollider().height = t.InverseTransformVector(chst.position - t.position).magnitude * 0.4f * scale;
                //PelvisBone.RagdollCollider().center = t.InverseTransformVector(chst.position - t.position) / 8f;
                //PelvisBone.RagdollCollider().radius = PelvisBone.RagdollCollider().height * 1f * scale;
                SetupPelvisBone(proc, bounciness, damper, drag, aDrag, massToDistr, projection, projAngle, projDist, enCollision, preProcessing, interpolation);

                t = PelvisBone;
                Vector3 pelvisToNonLegLocal = t.InverseTransformVector(chst.position - t.position);
                Ragdoll_AdjustCollider(proc.GetPelvisSetupBone(), pelvisToNonLegLocal * 0.4f, pelvisToNonLegLocal, pelvisToNonLegLocal.magnitude * 0.35f * scale, pelvisToNonLegLocal.magnitude * 0.75f * scale, interpolation);


                Ragdoll_RefreshComponents(characterJoints, SpineRoot, nck.position - SpineRoot.position, true, bounciness, damper, drag, aDrag, projection, projAngle, projDistance, enCollision, preProcessing);
                t = SpineRoot;
                SpineRoot.RagdollBody().mass = 0.09f * massToDistr;
                SpineRoot.RagdollCollider().height = t.InverseTransformVector(nck.position - t.position).magnitude * 0.52f * scale;
                SpineRoot.RagdollCollider().center = t.InverseTransformVector(nck.position - t.position) / 3.175f;
                SpineRoot.RagdollCollider().radius = SpineRoot.RagdollCollider().height * 0.55f * scale;

                Ragdoll_JointbodyConnect(SpineRoot, PelvisBone.RagdollBody());
                Ragdoll_Joint(SpineRoot, -40f, 25f, 10f, 10f, spring, damper);
                Ragdoll_ComputeAxis(baseTransform, SpineRoot, ref f, ref r, ref u);
                Ragdoll_JointAxis(SpineRoot, r, null, r, f, u);

                if (Chest != null)
                {
                    Ragdoll_RefreshComponents(characterJoints, Chest, nck.position - Chest.position, true, bounciness, damper, drag, aDrag, projection, projAngle, projDistance, enCollision, preProcessing);
                    t = Chest;
                    Chest.RagdollBody().mass = 0.05f * massToDistr;
                    Chest.RagdollCollider().height = t.InverseTransformVector(nck.position - t.position).magnitude * 0.6f * scale;
                    Chest.RagdollCollider().center = t.InverseTransformVector(nck.position - t.position) / 1.675f;
                    Chest.RagdollCollider().radius = Chest.RagdollCollider().height * 1f * scale;

                    Ragdoll_JointbodyConnect(Chest, SpineRoot.RagdollBody());
                    Ragdoll_Joint(Chest, -40f, 25f, 10f, 10f, spring, damper);
                    Ragdoll_ComputeAxis(baseTransform, Chest, ref f, ref r, ref u);
                    Ragdoll_JointAxis(Chest, r, null, r, f, u);
                }
                else
                {
                    chst = SpineRoot;
                }

                Transform ht = Head;
                Transform hr = Neck;

                if (Head.childCount > 0) { ht = Head.GetChild(0); hr = Head; }

                Vector3 hdDir;
                if (Neck == null) hdDir = (chst.position - ht.position) * 0.8f;
                else hdDir = ht.position - hr.position;

                Ragdoll_RefreshComponents(characterJoints, Head, hdDir, true, bounciness, damper, drag, aDrag, projection, projAngle, projDistance, enCollision, preProcessing);
                t = Head;

                Head.RagdollBody().mass = 0.04f * massToDistr;
                Head.RagdollCollider().height = t.InverseTransformVector(hdDir).magnitude * 1.5f * scale;
                Head.RagdollCollider().center = t.InverseTransformVector(hdDir) / 2.7f;
                Head.RagdollCollider().radius = Head.RagdollCollider().height * 1f * scale;

                Ragdoll_JointbodyConnect(Head, chst.RagdollBody());
                Ragdoll_Joint(Head, -50f, 50f, 35f, 35f, spring, damper);
                Ragdoll_ComputeAxis(baseTransform, Head, ref f, ref r, ref u);
                Ragdoll_JointAxis(Head, r, null, r, f, u);

                Transform armAttach = chst;

                // Left Shoulder
                if (generateShoulders)
                {
                    ht = LeftUpperArm; hr = LeftShoulder;
                    Ragdoll_RefreshComponents(characterJoints, hr, ht.position - hr.position, true, bounciness, damper, drag, aDrag, projection, projAngle, projDistance, enCollision, preProcessing);
                    t = hr;

                    float ddiv = 2.85f;

                    hr.RagdollBody().mass = 0.03f * massToDistr;
                    hr.RagdollCollider().height = t.InverseTransformVector(ht.position - hr.position).magnitude * 1.5f * scale;
                    hr.RagdollCollider().center = t.InverseTransformVector(ht.position - hr.position) / ddiv;
                    hr.RagdollCollider().radius = hr.RagdollCollider().height * 0.235f * scale;

                    Ragdoll_JointbodyConnect(hr, chst.RagdollBody());
                    Ragdoll_Joint(hr, -45f, 55f, 25f, 55f, spring, damper);
                    Ragdoll_ComputeArmAxis(baseTransform, hr, ht, ref f, ref r, ref u);
                    Ragdoll_JointAxis(hr, r, -f, r, f, u);
                    armAttach = hr;
                }


                // Left Arm
                ht = LeftForearm; hr = LeftUpperArm;
                Ragdoll_RefreshComponents(characterJoints, LeftUpperArm, ht.position - hr.position, true, bounciness, damper, drag, aDrag, projection, projAngle, projDistance, enCollision, preProcessing);
                t = LeftUpperArm;

                float upperArmShldDiv = 2.85f;

                LeftUpperArm.RagdollBody().mass = 0.03f * massToDistr;
                LeftUpperArm.RagdollCollider().height = t.InverseTransformVector(ht.position - hr.position).magnitude * 1.5f * scale;
                LeftUpperArm.RagdollCollider().center = t.InverseTransformVector(ht.position - hr.position) / upperArmShldDiv;
                LeftUpperArm.RagdollCollider().radius = LeftUpperArm.RagdollCollider().height * 0.235f * scale;

                Ragdoll_JointbodyConnect(LeftUpperArm, armAttach.RagdollBody());
                Ragdoll_Joint(LeftUpperArm, -45f, 55f, 25f, 55f, spring, damper);
                Ragdoll_ComputeArmAxis(baseTransform, LeftUpperArm, LeftForearm, ref f, ref r, ref u);
                Ragdoll_JointAxis(LeftUpperArm, r, -f, r, f, u);

                float fistsMul1 = 1.8f;
                float fistsMul2 = 1.75f;
                if (generateFists) { fistsMul1 = 1.25f; fistsMul2 = 2f; }


                Transform lHand = LeftHand;
                if (lHand == null) lHand = LeftForearm.GetLimbChild();

                ht = lHand; hr = LeftForearm;
                Ragdoll_RefreshComponents(characterJoints, LeftForearm, ht.position - hr.position, true, bounciness, damper, drag, aDrag, projection, projAngle, projDistance, enCollision, preProcessing);
                t = LeftForearm;
                LeftForearm.RagdollBody().mass = 0.02f * massToDistr;
                LeftForearm.RagdollCollider().height = t.InverseTransformVector(ht.position - hr.position).magnitude * fistsMul1 * scale;
                LeftForearm.RagdollCollider().center = t.InverseTransformVector(ht.position - hr.position) / fistsMul2;
                LeftForearm.RagdollCollider().radius = LeftForearm.RagdollCollider().height * 0.175f * scale;

                Ragdoll_JointbodyConnect(LeftForearm, LeftUpperArm.RagdollBody());
                Ragdoll_Joint(LeftForearm, -35f, 5f, 12f, 75f, spring, damper);
                Ragdoll_ComputeArmAxis(baseTransform, LeftForearm, lHand, ref f, ref r, ref u);
                Ragdoll_JointAxis(LeftForearm, u, -f, r, f, u);



                // Right Shoulder
                if (generateShoulders)
                {
                    ht = RightUpperArm; hr = RightShoulder;
                    Ragdoll_RefreshComponents(characterJoints, hr, ht.position - hr.position, true, bounciness, damper, drag, aDrag, projection, projAngle, projDistance, enCollision, preProcessing);
                    t = hr;

                    float ddiv = 2.85f;

                    hr.RagdollBody().mass = 0.03f * massToDistr;
                    hr.RagdollCollider().height = t.InverseTransformVector(ht.position - hr.position).magnitude * 1.5f * scale;
                    hr.RagdollCollider().center = t.InverseTransformVector(ht.position - hr.position) / ddiv;
                    hr.RagdollCollider().radius = hr.RagdollCollider().height * 0.235f * scale;

                    Ragdoll_JointbodyConnect(hr, chst.RagdollBody());
                    Ragdoll_Joint(hr, -45f, 55f, 25f, 55f, spring, damper);
                    Ragdoll_ComputeArmAxis(baseTransform, hr, ht, ref f, ref r, ref u);
                    Ragdoll_JointAxis(hr, r, -f, r, f, u);
                    armAttach = hr;
                }


                // Right Arm
                ht = RightForearm; hr = RightUpperArm;
                Ragdoll_RefreshComponents(characterJoints, RightUpperArm, ht.position - hr.position, true, bounciness, damper, drag, aDrag, projection, projAngle, projDistance, enCollision, preProcessing);
                t = RightUpperArm;
                RightUpperArm.RagdollBody().mass = 0.03f * massToDistr;
                RightUpperArm.RagdollCollider().height = t.InverseTransformVector(ht.position - hr.position).magnitude * 1.5f * scale;
                RightUpperArm.RagdollCollider().center = t.InverseTransformVector(ht.position - hr.position) / upperArmShldDiv;
                RightUpperArm.RagdollCollider().radius = RightUpperArm.RagdollCollider().height * 0.235f * scale;

                Ragdoll_JointbodyConnect(RightUpperArm, armAttach.RagdollBody());
                Ragdoll_Joint(RightUpperArm, -45f, 55f, 25f, 55f, spring, damper);
                Ragdoll_ComputeArmAxis(baseTransform, RightUpperArm, RightForearm, ref f, ref r, ref u);
                Ragdoll_JointAxis(RightUpperArm, -r, -f, r, f, u);


                Transform rightHand = RightHand;
                if (rightHand == null) rightHand = RightForearm.GetLimbChild();

                ht = rightHand; hr = RightForearm;
                Ragdoll_RefreshComponents(characterJoints, RightForearm, ht.position - hr.position, true, bounciness, damper, drag, aDrag, projection, projAngle, projDistance, enCollision, preProcessing);
                t = RightForearm;
                RightForearm.RagdollBody().mass = 0.02f * massToDistr;
                RightForearm.RagdollCollider().height = t.InverseTransformVector(ht.position - hr.position).magnitude * fistsMul1 * scale;
                RightForearm.RagdollCollider().center = t.InverseTransformVector(ht.position - hr.position) / fistsMul2;
                RightForearm.RagdollCollider().radius = RightForearm.RagdollCollider().height * 0.175f * scale;

                Ragdoll_JointbodyConnect(RightForearm, RightUpperArm.RagdollBody());
                Ragdoll_Joint(RightForearm, -35f, 5f, 12f, 75f, spring, damper);
                Ragdoll_ComputeArmAxis(baseTransform, RightForearm, rightHand, ref f, ref r, ref u);
                Ragdoll_JointAxis(RightForearm, u, -f, r, f, u);


                // Left Leg
                ht = LeftLowerLeg; hr = LeftUpperLeg;
                Ragdoll_RefreshComponents(characterJoints, LeftUpperLeg, ht.position - hr.position, true, bounciness, damper, drag, aDrag, projection, projAngle, projDistance, enCollision, preProcessing);
                t = LeftUpperLeg;
                LeftUpperLeg.RagdollBody().mass = 0.05f * massToDistr;
                LeftUpperLeg.RagdollCollider().height = t.InverseTransformVector(ht.position - hr.position).magnitude * 1.15f * scale;
                LeftUpperLeg.RagdollCollider().center = t.InverseTransformVector(ht.position - hr.position) / 2f;
                LeftUpperLeg.RagdollCollider().radius = LeftUpperLeg.RagdollCollider().height * 0.21f * scale;

                Ragdoll_JointbodyConnect(LeftUpperLeg, PelvisBone.RagdollBody());
                Ragdoll_Joint(LeftUpperLeg, -60f, 60f, 15f, 24f, spring, damper);
                Ragdoll_ComputeAxis(baseTransform, LeftUpperLeg, ref f, ref r, ref u);
                Ragdoll_JointAxis(LeftUpperLeg, r, null, r, f, u);


                float footsMul = 1.5f;
                float footsMul2 = 1.75f;
                if (generateFoots) { footsMul = 1.15f; footsMul2 = 2.1f; }

                Transform lFoot = LeftFoot;
                if (lFoot == null) lFoot = LeftLowerLeg.GetLimbChild();

                ht = lFoot; hr = LeftLowerLeg;
                Ragdoll_RefreshComponents(characterJoints, LeftLowerLeg, ht.position - hr.position, true, bounciness, damper, drag, aDrag, projection, projAngle, projDistance, enCollision, preProcessing);
                t = LeftLowerLeg;
                LeftLowerLeg.RagdollBody().mass = 0.03f * massToDistr;
                LeftLowerLeg.RagdollCollider().height = t.InverseTransformVector(ht.position - hr.position).magnitude * footsMul * scale;
                LeftLowerLeg.RagdollCollider().center = t.InverseTransformVector(ht.position - hr.position) / footsMul2;
                LeftLowerLeg.RagdollCollider().radius = LeftLowerLeg.RagdollCollider().height * 0.145f * scale;

                Ragdoll_JointbodyConnect(LeftLowerLeg, LeftUpperLeg.RagdollBody());
                Ragdoll_Joint(LeftLowerLeg, -155f, 1f, 15f, 15f, spring, damper);
                Ragdoll_ComputeAxis(baseTransform, LeftLowerLeg, ref f, ref r, ref u);
                Ragdoll_JointAxis(LeftLowerLeg, r, null, r, f, u);


                // Right Leg
                ht = RightLowerLeg; hr = RightUpperLeg;
                Ragdoll_RefreshComponents(characterJoints, RightUpperLeg, ht.position - hr.position, true, bounciness, damper, drag, aDrag, projection, projAngle, projDistance, enCollision, preProcessing);
                t = RightUpperLeg;
                RightUpperLeg.RagdollBody().mass = 0.05f * massToDistr;
                RightUpperLeg.RagdollCollider().height = t.InverseTransformVector(ht.position - hr.position).magnitude * 1.15f * scale;
                RightUpperLeg.RagdollCollider().center = t.InverseTransformVector(ht.position - hr.position) / 2f;
                RightUpperLeg.RagdollCollider().radius = RightUpperLeg.RagdollCollider().height * 0.21f * scale;

                Ragdoll_JointbodyConnect(RightUpperLeg, PelvisBone.RagdollBody());
                Ragdoll_Joint(RightUpperLeg, -60f, 60f, 15f, 24f, spring, damper);
                Ragdoll_ComputeAxis(baseTransform, RightUpperLeg, ref f, ref r, ref u);
                Ragdoll_JointAxis(RightUpperLeg, r, null, r, f, u);


                Transform rFoot = RightFoot;
                if (rFoot == null) rFoot = RightLowerLeg.GetLimbChild();

                ht = rFoot; hr = RightLowerLeg;
                Ragdoll_RefreshComponents(characterJoints, RightLowerLeg, ht.position - hr.position, true, bounciness, damper, drag, aDrag, projection, projAngle, projDistance, enCollision, preProcessing);
                t = RightLowerLeg;
                RightLowerLeg.RagdollBody().mass = 0.03f * massToDistr;
                RightLowerLeg.RagdollCollider().height = t.InverseTransformVector(ht.position - hr.position).magnitude * footsMul * scale;
                RightLowerLeg.RagdollCollider().center = t.InverseTransformVector(ht.position - hr.position) / footsMul2;
                RightLowerLeg.RagdollCollider().radius = RightLowerLeg.RagdollCollider().height * 0.145f * scale;

                Ragdoll_JointbodyConnect(RightLowerLeg, RightUpperLeg.RagdollBody());
                Ragdoll_Joint(RightLowerLeg, -155f, 1f, 15f, 15f, spring, damper);

                Ragdoll_ComputeAxis(baseTransform, RightLowerLeg, ref f, ref r, ref u);
                Ragdoll_JointAxis(RightLowerLeg, r, null, r, f, u);


                if (generateFists)
                {
                    float refLen = Vector3.Distance(rightHand.position, RightForearm.position) * 0.35f;
                    ht = RightForearm; hr = RightUpperArm;
                    refLen *= scale;

                    BoxCollider footBox = AddIfDontHave<BoxCollider>(rightHand);

                    float firstHeight = refLen * 0.21f;
                    float firstWidth = refLen * 0.8f;
                    float fistLen = refLen * 0.8f;

                    footBox.size =
                        footBox.transform.InverseTransformVector(baseTransform.right * fistLen) * 2f
                        + footBox.transform.InverseTransformVector(baseTransform.up * firstHeight) * 2f
                        + footBox.transform.InverseTransformVector(baseTransform.forward * firstWidth) * 2f
                        ;

                    footBox.center = footBox.transform.InverseTransformVector(baseTransform.right * (fistLen * scale))
                        + footBox.transform.InverseTransformVector(baseTransform.forward * (firstWidth * scale * 0.8f));

                    Ragdoll_RefreshComponents(characterJoints, rightHand, ht.position - hr.position, true, bounciness, damper, drag, aDrag, projection, projAngle, projDistance, enCollision, preProcessing, false);
                    rightHand.RagdollBody().mass = 0.008f * massToDistr;

                    Ragdoll_JointbodyConnect(rightHand, RightForearm.RagdollBody());
                    Ragdoll_Joint(rightHand, -155f, 1f, 15f, 15f, spring, damper);

                    Ragdoll_ComputeAxis(baseTransform, rightHand, ref f, ref r, ref u);
                    Ragdoll_JointAxis(rightHand, r, null, r, f, u);


                    // Left fist

                    refLen = Vector3.Distance(LeftHand.position, LeftForearm.position) * 0.35f;
                    ht = LeftForearm; hr = LeftUpperArm;
                    refLen *= scale;

                    footBox = AddIfDontHave<BoxCollider>(LeftHand);

                    firstHeight = refLen * 0.21f;
                    firstWidth = refLen * 0.8f;
                    fistLen = refLen * 0.8f;

                    footBox.size =
                        footBox.transform.InverseTransformVector(-baseTransform.right * fistLen) * 2f
                        + footBox.transform.InverseTransformVector(baseTransform.up * firstHeight) * 2f
                        + footBox.transform.InverseTransformVector(baseTransform.forward * firstWidth) * 2f
                        ;

                    footBox.center = footBox.transform.InverseTransformVector(-baseTransform.right * (fistLen * scale))
                        + footBox.transform.InverseTransformVector(baseTransform.forward * (firstWidth * scale * 0.8f));

                    Ragdoll_RefreshComponents(characterJoints, LeftHand, ht.position - hr.position, true, bounciness, damper, drag, aDrag, projection, projAngle, projDistance, enCollision, preProcessing, false);
                    LeftHand.RagdollBody().mass = 0.008f * massToDistr;

                    Ragdoll_JointbodyConnect(LeftHand, LeftForearm.RagdollBody());
                    Ragdoll_Joint(LeftHand, -155f, 1f, 15f, 15f, spring, damper);

                    Ragdoll_ComputeAxis(baseTransform, LeftHand, ref f, ref r, ref u);
                    Ragdoll_JointAxis(LeftHand, r, null, r, f, u);
                }


                if (generateFoots)
                {
                    float refLen = Vector3.Distance(RightFoot.position, RightLowerLeg.position) * 0.35f;
                    refLen *= scale;

                    BoxCollider footBox = AddIfDontHave<BoxCollider>(RightFoot);

                    float footHeight = refLen * 0.2f;
                    float footWidth = refLen * 0.175f;
                    float footLen = refLen * 0.8f;

                    footBox.size =
                        footBox.transform.InverseTransformVector(baseTransform.forward * footLen) * 2f
                        + footBox.transform.InverseTransformVector(baseTransform.up * footHeight) * 2f
                        + footBox.transform.InverseTransformVector(baseTransform.right * footWidth) * 2f
                        ;

                    footBox.center = footBox.transform.InverseTransformVector(baseTransform.forward * (footLen * scale));

                    Ragdoll_RefreshComponents(characterJoints, RightFoot, ht.position - hr.position, true, bounciness, damper, drag, aDrag, projection, projAngle, projDistance, enCollision, preProcessing, false);
                    RightFoot.RagdollBody().mass = 0.008f * massToDistr;

                    Ragdoll_JointbodyConnect(RightFoot, RightLowerLeg.RagdollBody());
                    Ragdoll_Joint(RightFoot, -155f, 1f, 15f, 15f, spring, damper);

                    Ragdoll_ComputeAxis(baseTransform, RightFoot, ref f, ref r, ref u);
                    Ragdoll_JointAxis(RightFoot, r, null, r, f, u);



                    // Left foot
                    ht = lFoot; hr = LeftLowerLeg;
                    refLen = Vector3.Distance(LeftFoot.position, LeftLowerLeg.position) * 0.35f;
                    refLen *= scale;

                    footBox = AddIfDontHave<BoxCollider>(LeftFoot);

                    footHeight = refLen * 0.2f;
                    footWidth = refLen * 0.175f;
                    footLen = refLen * 0.8f;

                    footBox.size =
                        footBox.transform.InverseTransformVector(baseTransform.forward * footLen) * 2f
                        + footBox.transform.InverseTransformVector(baseTransform.up * footHeight) * 2f
                        + footBox.transform.InverseTransformVector(-baseTransform.right * footWidth) * 2f
                        ;

                    footBox.center = footBox.transform.InverseTransformVector(baseTransform.forward * (footLen * scale));

                    Ragdoll_RefreshComponents(characterJoints, LeftFoot, ht.position - hr.position, true, bounciness, damper, drag, aDrag, projection, projAngle, projDistance, enCollision, preProcessing, false);
                    LeftFoot.RagdollBody().mass = 0.008f * massToDistr;

                    Ragdoll_JointbodyConnect(LeftFoot, LeftLowerLeg.RagdollBody());
                    Ragdoll_Joint(LeftFoot, -155f, 1f, 15f, 15f, spring, damper);

                    Ragdoll_ComputeAxis(baseTransform, LeftFoot, ref f, ref r, ref u);
                    Ragdoll_JointAxis(LeftFoot, r, null, r, f, u);



                }

                #endregion

                foreach (Transform tb in bones)
                {
                    if (tb == null) continue;
                    Rigidbody rig = tb.GetComponent<Rigidbody>();
                    if (rig) rig.interpolation = interpolation;
                }

            }

        }


        void SetupPelvisBone(RagdollProcessor proc, float bounciness = 0f, float damper = 0f, float drag = 0.5f, float aDrag = 1f, float massToDistr = 65f, bool projection = true, float projAngle = 90, float projDistance = 0.05f, bool enCollision = false, bool preProcessing = false, RigidbodyInterpolation interpolation = RigidbodyInterpolation.Interpolate)
        {
            var pelvSetup = proc.GetPelvisSetupBone();
            Ragdoll_RefreshComponents(false, pelvSetup, Head.position - PelvisBone.position, false, bounciness, damper, drag, aDrag, projection, projAngle, projDistance, enCollision, preProcessing);
            //Transform t = PelvisBone;
            PelvisBone.RagdollBody().mass = pelvSetup.MassPercentage * massToDistr;
            PelvisBone.RagdollBody().interpolation = interpolation;

            //PelvisBone.RagdollCollider().height = t.InverseTransformVector(Head.position - t.position).magnitude * 0.3f * scale;
            //PelvisBone.RagdollCollider().center = t.InverseTransformVector(Head.position - t.position) / 8f;
            //PelvisBone.RagdollCollider().radius = PelvisBone.RagdollCollider().height * 1f * scale;
        }



        public List<Transform> GetRagdollBonesAttachedWith(Transform target, RagdollProcessor proc)
        {
            List<Transform> child = new List<Transform>();

            for (int i = 0; i < proc.CustomLimbsBonesChains.Count; i++)
            {
                var chain = proc.CustomLimbsBonesChains[i];
                Transform attached = chain.GetAttachTransform(proc);
                if (attached == target) child.Add(chain.BoneSetups[0].t);
            }

            return child;
        }



        public void OnSceneGUI(RagdollProcessor proc)
        {
            if (ragdollTweak != tweakRagd.None)
            {
                generateRagdoll = false;
                bool tweakScale = ragdollTweak == tweakRagd.Scale;

                if (useSymmetry)
                    RagdollProcessor._editor_symmetryRef = BaseTransform;
                else
                    RagdollProcessor._editor_symmetryRef = null;


                if (proc.BonesSetupMode == RagdollProcessor.EBonesSetupMode.CustomLimbs)
                {
                    for (int i = 0; i < BoneChains.Count; i++)
                    {
                        var chain = BoneChains[i];
                        for (int b = 0; b < chain.BoneSetups.Count; b++)
                        {
                            var bone = chain.BoneSetups[b];
                            if (bone.t == null) continue;
                            Collider c = bone.t.GetComponent<Collider>();
                            if (c == null) continue;
                            RagdollProcessor.DrawColliderHandles(c, tweakScale, false);
                        }
                    }
                }
                else
                {
                    if (LeftUpperArm.RagdollCollider()) RagdollProcessor.DrawColliderHandles(LeftUpperArm.RagdollCollider(), tweakScale, false, RightUpperArm.RagdollCollider());
                    if (RightUpperArm.RagdollCollider()) RagdollProcessor.DrawColliderHandles(RightUpperArm.RagdollCollider(), tweakScale, false, LeftUpperArm.RagdollCollider());
                    if (LeftForearm.RagdollCollider()) RagdollProcessor.DrawColliderHandles(LeftForearm.RagdollCollider(), tweakScale, false, RightForearm.RagdollCollider());
                    if (RightForearm.RagdollCollider()) RagdollProcessor.DrawColliderHandles(RightForearm.RagdollCollider(), tweakScale, false, LeftForearm.RagdollCollider());

                    if (LeftUpperLeg.RagdollCollider()) RagdollProcessor.DrawColliderHandles(LeftUpperLeg.RagdollCollider(), tweakScale, false, RightUpperLeg.RagdollCollider());
                    if (LeftLowerLeg.RagdollCollider()) RagdollProcessor.DrawColliderHandles(LeftLowerLeg.RagdollCollider(), tweakScale, false, RightLowerLeg.RagdollCollider());
                    if (RightUpperLeg.RagdollCollider()) RagdollProcessor.DrawColliderHandles(RightUpperLeg.RagdollCollider(), tweakScale, false, LeftUpperLeg.RagdollCollider());
                    if (RightLowerLeg.RagdollCollider()) RagdollProcessor.DrawColliderHandles(RightLowerLeg.RagdollCollider(), tweakScale, false, LeftLowerLeg.RagdollCollider());

                    if (PelvisBone.RagdollCollider()) RagdollProcessor.DrawColliderHandles(PelvisBone.RagdollCollider(), tweakScale, true);
                    if (SpineRoot.RagdollCollider()) RagdollProcessor.DrawColliderHandles(SpineRoot.RagdollCollider(), tweakScale, true);
                    if (Chest) if (Chest.RagdollCollider()) RagdollProcessor.DrawColliderHandles(Chest.RagdollCollider(), tweakScale, true);
                    if (Head.RagdollCollider()) RagdollProcessor.DrawColliderHandles(Head.RagdollCollider(), tweakScale, true);

                    if (LeftHand) if (LeftHand.RagdollBCollider()) RagdollProcessor.DrawColliderHandles(LeftHand.RagdollBCollider(), tweakScale, false, RightHand.RagdollBCollider());
                    if (RightHand) if (RightHand.RagdollBCollider()) RagdollProcessor.DrawColliderHandles(RightHand.RagdollBCollider(), tweakScale, false, LeftHand.RagdollBCollider());
                    if (LeftFoot) if (LeftFoot.RagdollBCollider()) RagdollProcessor.DrawColliderHandles(LeftFoot.RagdollBCollider(), tweakScale, false, RightFoot.RagdollBCollider());
                    if (RightFoot) if (RightFoot.RagdollBCollider()) RagdollProcessor.DrawColliderHandles(RightFoot.RagdollBCollider(), tweakScale, false, LeftFoot.RagdollBCollider());
                }
            }

        }


        public List<Transform> bones;

        public Transform PelvisBone;
        public Transform SpineRoot;
        public Transform Chest;
        public Transform Head;
        public Transform LeftUpperArm;
        public Transform RightUpperArm;
        public Transform LeftForearm;
        public Transform RightForearm;
        public Transform LeftUpperLeg;
        public Transform LeftLowerLeg;
        public Transform RightUpperLeg;
        public Transform RightLowerLeg;

        public Transform RightHand;
        public Transform LeftHand;

        public Transform RightFoot;
        public Transform LeftFoot;

        public Transform RightShoulder;
        public Transform LeftShoulder;

        public List<RagdollProcessor.BonesChain> BoneChains = new List<RagdollProcessor.BonesChain>();


        public void SetAllBoneReferences(RagdollProcessor proc)
        {
            if (proc.BonesSetupMode == RagdollProcessor.EBonesSetupMode.HumanoidLimbs)
            {
                bones = new List<Transform>();

                PelvisBone = proc.Pelvis;
                SpineRoot = proc.SpineStart;
                Chest = proc.Chest;
                Head = proc.Head;
                LeftUpperArm = proc.LeftUpperArm;
                RightUpperArm = proc.RightUpperArm;
                LeftForearm = proc.LeftForeArm;
                RightForearm = proc.RightForeArm;
                LeftUpperLeg = proc.LeftUpperLeg;
                LeftLowerLeg = proc.LeftLowerLeg;
                RightUpperLeg = proc.RightUpperLeg;
                RightLowerLeg = proc.RightLowerLeg;

                RightShoulder = proc.RightShoulder;
                LeftShoulder = proc.LeftShoulder;
                if (RightShoulder) generateShoulders = true;

                LeftFoot = proc.LeftFoot;
                RightFoot = proc.RightFoot;
                if (LeftFoot) generateFoots = true;

                LeftHand = proc.LeftFist;
                RightHand = proc.RightFist;
                if (LeftHand) generateFists = true;

                bones.Add(PelvisBone);
                bones.Add(SpineRoot);
                bones.Add(Chest);
                bones.Add(Head);
                bones.Add(LeftUpperArm);
                bones.Add(RightUpperArm);
                bones.Add(LeftForearm);
                bones.Add(RightForearm);
                bones.Add(LeftUpperLeg);
                bones.Add(LeftLowerLeg);
                bones.Add(RightUpperLeg);
                bones.Add(RightLowerLeg);


                if (generateFists)
                {
                    LeftHand = proc.LeftFist;
                    RightHand = proc.RightFist;
                    bones.Add(LeftHand);
                    bones.Add(RightHand);
                }

                if (generateFoots)
                {
                    LeftFoot = proc.LeftFoot;
                    RightFoot = proc.RightFoot;
                    bones.Add(LeftFoot);
                    bones.Add(RightFoot);
                }

                if (generateShoulders)
                {
                    LeftShoulder = proc.LeftShoulder;
                    RightShoulder = proc.RightShoulder;
                    bones.Add(LeftShoulder);
                    bones.Add(RightShoulder);
                }

                return;
            }

            if (BoneChains == null) BoneChains = new List<RagdollProcessor.BonesChain>();
            else BoneChains.Clear();

            var pelvisChain = new RagdollProcessor.BonesChain();
            pelvisChain.BoneSetups = new List<RagdollProcessor.RagdollBoneSetup>();
            pelvisChain.BoneSetups.Add(proc.GetPelvisSetupBone());
            BoneChains.Add(pelvisChain);
            PelvisBone = pelvisChain.BoneSetups[0].t;
            Head = proc.Head;

            for (int i = 0; i < proc.CustomLimbsBonesChains.Count; i++)
            {
                BoneChains.Add(proc.CustomLimbsBonesChains[i]);
            }
        }




        public void Ragdoll_JointbodyConnect(Transform bone, Rigidbody connected)
        {
            CharacterJoint charJ = bone.GetComponent<CharacterJoint>();

            if (charJ) charJ.connectedBody = connected;
            else
            {
                ConfigurableJoint confJ = bone.GetComponent<ConfigurableJoint>();
                if (confJ) confJ.connectedBody = connected;
            }
        }

        public void Ragdoll_JointAxis(Transform bone, Vector3 axis, Vector3? swingAxis = null, Vector3? r = null, Vector3? f = null, Vector3? u = null)
        {
            CharacterJoint charJ = bone.GetComponent<CharacterJoint>();

            if (charJ)
            {
                charJ.axis = axis;
                if (swingAxis != null) charJ.swingAxis = swingAxis.Value;
            }
            else
            {
                ConfigurableJoint confJ = bone.GetComponent<ConfigurableJoint>();

                if (confJ)
                {
                    confJ.axis = r.Value;
                    if (swingAxis != null) confJ.secondaryAxis = f.Value;
                }
            }
        }

        public void Ragdoll_Joint(Transform bone, float lowTwist, float hightTwist, float lowSwing, float highSwing, float spring = 0f, float damp = 0f)
        {
            CharacterJoint joint = bone.GetComponent<CharacterJoint>();

            if (joint != null)
            {
                SoftJointLimit lim;

                lim = joint.highTwistLimit;
                lim.limit = hightTwist;
                joint.highTwistLimit = lim;

                lim = joint.lowTwistLimit;
                lim.limit = lowTwist;
                joint.lowTwistLimit = lim;

                lim = joint.swing1Limit;
                lim.limit = lowSwing;
                joint.swing1Limit = lim;

                lim = joint.swing2Limit;
                lim.limit = highSwing;
                joint.swing2Limit = lim;


                if (spring > 0f)
                {
                    SoftJointLimitSpring spr = joint.swingLimitSpring;
                    spr.spring = spring;
                    spr.damper = damp;
                    joint.swingLimitSpring = spr;
                }
            }

            ConfigurableJoint cjoint = bone.GetComponent<ConfigurableJoint>();
            if (cjoint)
            {
                var slim = cjoint.lowAngularXLimit;
                slim.limit = lowTwist;
                cjoint.lowAngularXLimit = slim;

                slim = cjoint.highAngularXLimit;
                slim.limit = hightTwist;
                cjoint.highAngularXLimit = slim;

                slim = cjoint.angularYLimit;
                slim.limit = lowSwing;
                cjoint.angularYLimit = slim;

                slim = cjoint.angularZLimit;
                slim.limit = highSwing;
                cjoint.angularZLimit = slim;
            }

        }


        public void RemoveRagdoll(RagdollProcessor proc)
        {
            if (proc.Pelvis == null) return;

            Ragdoll_RemoveFrom(proc.Pelvis);

            if (proc.BonesSetupMode == RagdollProcessor.EBonesSetupMode.HumanoidLimbs)
            {
                if (bones == null)
                {
                    SetAllBoneReferences(proc);
                }

                if (bones != null)
                    foreach (Transform t in bones)
                    {
                        if (t == null) continue;
                        Ragdoll_RemoveFrom(t);
                    }

                if (bones != null)
                    foreach (Transform t in bones)
                    {
                        if (t == null) continue;
                        if (t.childCount > 0)
                        {
                            Ragdoll_RemoveFrom(t.GetChild(0));
                            Ragdoll_RemoveFrom(t.GetLimbChild());
                        }
                    }
            }
            else
            {

                for (int c = 0; c < proc.CustomLimbsBonesChains.Count; c++)
                {
                    RagdollProcessor.BonesChain chain = proc.CustomLimbsBonesChains[c];
                    for (int b = 0; b < chain.BoneSetups.Count; b++)
                    {
                        var bone = chain.BoneSetups[b];
                        if (bone.t == null) continue;
                        Ragdoll_RemoveFrom(bone.t);
                    }
                }
            }

        }

        public void Ragdoll_RefreshComponents(bool characterJoint, Transform bone, Vector3 towards, bool addJoint = true, float bounciness = 0f, float damper = 0f, float drag = 1, float angDrag = 1f, bool projection = true, float projAngle = 90, float projDistance = 0.05f, bool enCollision = false, bool preProcessing = false, bool capsuleColl = true)
        {
            Rigidbody rig = AddIfDontHave<Rigidbody>(bone);

            ConfigurableJoint confJ = null;
            CharacterJoint charJ = null;

            if (addJoint)
            {
                if (characterJoint)
                {
                    charJ = AddIfDontHave<CharacterJoint>(bone);
                    DestroyIfHave<ConfigurableJoint>(bone);
                }
                else
                {
                    confJ = AddIfDontHave<ConfigurableJoint>(bone);
                    DestroyIfHave<CharacterJoint>(bone);
                }
            }


            rig.angularDrag = drag;
            rig.drag = angDrag;
            rig.interpolation = RigidbodyInterpolation.None;

            Vector3 capsuleDir = bone.transform.InverseTransformVector(towards);
            capsuleDir = Prepare_ChooseDominantAxis(capsuleDir);

            if (capsuleColl)
            {
                CapsuleCollider coll = AddIfDontHave<CapsuleCollider>(bone);
                if (capsuleDir.x > 0.1f || capsuleDir.x < -0.1f) coll.direction = 0;
                if (capsuleDir.y > 0.1f || capsuleDir.y < -0.1f) coll.direction = 1;
                if (capsuleDir.z > 0.1f || capsuleDir.z < -0.1f) coll.direction = 2;
            }

            if (confJ)
            {
                confJ.xMotion = ConfigurableJointMotion.Locked;
                confJ.yMotion = ConfigurableJointMotion.Locked;
                confJ.zMotion = ConfigurableJointMotion.Locked;

                confJ.angularXMotion = ConfigurableJointMotion.Limited;
                confJ.angularYMotion = ConfigurableJointMotion.Limited;
                confJ.angularZMotion = ConfigurableJointMotion.Limited;

                confJ.rotationDriveMode = RotationDriveMode.Slerp;

                var spr = confJ.angularXLimitSpring;
                spr.spring = 1500;
                confJ.angularXLimitSpring = spr;

                var drv = confJ.slerpDrive;
                drv.positionSpring = 1500;
                confJ.slerpDrive = drv;
            }

            if (charJ)
            {
                charJ.swingAxis = capsuleDir;

                charJ.enableProjection = projection;
                charJ.projectionAngle = projAngle;
                charJ.projectionDistance = projDistance;

                charJ.enableCollision = enCollision;
                charJ.enablePreprocessing = preProcessing;

                var sp1 = charJ.swingLimitSpring;
                //sp1.spring = 1500;
                sp1.damper = damper;
                charJ.swingLimitSpring = sp1;

                sp1 = charJ.twistLimitSpring;
                sp1.spring = 1500;
                sp1.damper = damper;
                charJ.twistLimitSpring = sp1;

                var sp2 = charJ.lowTwistLimit;
                sp2.bounciness = bounciness;
                charJ.lowTwistLimit = sp2;

                sp2 = charJ.highTwistLimit;
                sp2.bounciness = bounciness;
                charJ.highTwistLimit = sp2;

                sp2 = charJ.swing1Limit;
                sp2.bounciness = bounciness;
                charJ.swing1Limit = sp2;

                sp2 = charJ.swing2Limit;
                sp2.bounciness = bounciness;
                charJ.swing2Limit = sp2;
            }
        }


        public void Ragdoll_RefreshComponents(bool characterJoint, RagdollProcessor.RagdollBoneSetup bone, Vector3 towards, bool addJoint = true, float bounciness = 0f, float damper = 0f, float drag = 1, float angDrag = 1f, bool projection = true, float projAngle = 90, float projDistance = 0.05f, bool enCollision = false, bool preProcessing = false)
        {
            Transform t = bone.t;

            #region Preparing rigidbody and joints

            Rigidbody rig = AddIfDontHave<Rigidbody>(t);

            ConfigurableJoint confJ = null;
            CharacterJoint charJ = null;

            if (addJoint)
            {
                if (characterJoint)
                {
                    charJ = AddIfDontHave<CharacterJoint>(t);
                    DestroyIfHave<ConfigurableJoint>(t);
                }
                else
                {
                    confJ = AddIfDontHave<ConfigurableJoint>(t);
                    DestroyIfHave<CharacterJoint>(t);
                }
            }

            #endregion


            rig.angularDrag = drag;
            rig.drag = angDrag;
            rig.interpolation = RigidbodyInterpolation.None;

            if (bone.ColliderType == RagdollProcessor.RagdollBoneSetup.EColliderType.Mesh && bone.ColliderMesh == null) bone.ColliderType = RagdollProcessor.RagdollBoneSetup.EColliderType.Capsule;

            // Defining capsule collider and joints swing axis direction
            Vector3 swingDirection = t.InverseTransformVector(towards);
            swingDirection = Prepare_ChooseDominantAxis(swingDirection);

            if (bone.ColliderType == RagdollProcessor.RagdollBoneSetup.EColliderType.Capsule)
            {
                if (bone.Generator_OverrideCapsuleDir == RagdollProcessor.RagdollBoneSetup.ECapsDirOverride.None)
                    SetCapsuleAxis(AddIfDontHave<CapsuleCollider>(t), swingDirection, false);
                else
                {
                    AddIfDontHave<CapsuleCollider>(t).direction = (int)bone.Generator_OverrideCapsuleDir;
                }
            }
            else // In other case just adding target collider type
            {


                if (bone.ColliderType == RagdollProcessor.RagdollBoneSetup.EColliderType.Box) AddIfDontHave<BoxCollider>(t);
                else if (bone.ColliderType == RagdollProcessor.RagdollBoneSetup.EColliderType.Sphere) AddIfDontHave<SphereCollider>(t);
                else if (bone.ColliderType == RagdollProcessor.RagdollBoneSetup.EColliderType.Mesh) AddIfDontHave<MeshCollider>(t).sharedMesh = bone.ColliderMesh;
            }

            DestroyIfHave(t, bone.ColliderType);

            #region Setting up joints

            if (confJ)
            {
                confJ.xMotion = ConfigurableJointMotion.Locked;
                confJ.yMotion = ConfigurableJointMotion.Locked;
                confJ.zMotion = ConfigurableJointMotion.Locked;

                confJ.angularXMotion = ConfigurableJointMotion.Limited;
                confJ.angularYMotion = ConfigurableJointMotion.Limited;
                confJ.angularZMotion = ConfigurableJointMotion.Limited;

                confJ.rotationDriveMode = RotationDriveMode.Slerp;

                var spr = confJ.angularXLimitSpring;
                spr.spring = 1500;
                confJ.angularXLimitSpring = spr;

                var drv = confJ.slerpDrive;
                drv.positionSpring = 1500;
                confJ.slerpDrive = drv;
            }

            if (charJ)
            {
                charJ.swingAxis = swingDirection;

                charJ.enableProjection = projection;
                charJ.projectionAngle = projAngle;
                charJ.projectionDistance = projDistance;

                charJ.enableCollision = enCollision;
                charJ.enablePreprocessing = preProcessing;

                var sp1 = charJ.swingLimitSpring;
                //sp1.spring = 1500;
                sp1.damper = damper;
                charJ.swingLimitSpring = sp1;

                sp1 = charJ.twistLimitSpring;
                sp1.spring = 1500;
                sp1.damper = damper;
                charJ.twistLimitSpring = sp1;

                var sp2 = charJ.lowTwistLimit;
                sp2.bounciness = bounciness;
                charJ.lowTwistLimit = sp2;

                sp2 = charJ.highTwistLimit;
                sp2.bounciness = bounciness;
                charJ.highTwistLimit = sp2;

                sp2 = charJ.swing1Limit;
                sp2.bounciness = bounciness;
                charJ.swing1Limit = sp2;

                sp2 = charJ.swing2Limit;
                sp2.bounciness = bounciness;
                charJ.swing2Limit = sp2;
            }

            #endregion

        }

        void DestroyIfHave(Transform t, RagdollProcessor.RagdollBoneSetup.EColliderType toIgnore)
        {
            if (toIgnore != RagdollProcessor.RagdollBoneSetup.EColliderType.Box) DestroyIfHave<BoxCollider>(t);
            if (toIgnore != RagdollProcessor.RagdollBoneSetup.EColliderType.Sphere) DestroyIfHave<SphereCollider>(t);
            if (toIgnore != RagdollProcessor.RagdollBoneSetup.EColliderType.Capsule) DestroyIfHave<CapsuleCollider>(t);
            if (toIgnore != RagdollProcessor.RagdollBoneSetup.EColliderType.Mesh) DestroyIfHave<MeshCollider>(t);
        }

        public void SetCapsuleAxis(CapsuleCollider coll, Vector3 swingDirection, bool chooseDominant = true)
        {
            if (chooseDominant) swingDirection = Prepare_ChooseDominantAxis(swingDirection);

            if (swingDirection.x > 0.1f || swingDirection.x < -0.1f) coll.direction = 0;
            if (swingDirection.y > 0.1f || swingDirection.y < -0.1f) coll.direction = 1;
            if (swingDirection.z > 0.1f || swingDirection.z < -0.1f) coll.direction = 2;
        }

        public void Ragdoll_AdjustCollider(RagdollProcessor.RagdollBoneSetup bone, Vector3 center, Vector3 localScale, float length, float radius, RigidbodyInterpolation interpolation)
        {
            float scl = 1f;
            if (bone.t.lossyScale.x != 0f) scl = 1f / bone.t.lossyScale.x;

            Vector3 localScaleAbs = new Vector3(Mathf.Abs(localScale.x), Mathf.Abs(localScale.y), Mathf.Abs(localScale.z));

            if (localScaleAbs.x > localScaleAbs.y)
            {
                if (localScaleAbs.x > localScaleAbs.z)
                { center.x += localScaleAbs.x * bone.Generator_LengthOffset; }
                else
                { center.z += localScaleAbs.z * bone.Generator_LengthOffset; }
            }
            else if (localScaleAbs.y > localScaleAbs.z)
            { center.y += localScaleAbs.y * bone.Generator_LengthOffset; }
            else
            { center.z += localScaleAbs.z * bone.Generator_LengthOffset; }


            if (bone.ColliderType == RagdollProcessor.RagdollBoneSetup.EColliderType.Capsule)
            {
                CapsuleCollider coll = AddIfDontHave<CapsuleCollider>(bone.t);

                if (bone.Generator_OverrideCapsuleDir == RagdollProcessor.RagdollBoneSetup.ECapsDirOverride.None)
                    SetCapsuleAxis(coll, localScale);
                coll.height = length * scl * bone.Generator_LengthMul;
                coll.center = center * scl + bone.Generator_Offset;
                coll.radius = radius * scl * bone.Generator_ScaleMul;
            }
            else if (bone.ColliderType == RagdollProcessor.RagdollBoneSetup.EColliderType.Box)
            {
                BoxCollider coll = AddIfDontHave<BoxCollider>(bone.t);

                coll.size = localScaleAbs;
                float biggset = coll.size.x;
                if (coll.size.y > biggset) biggset = coll.size.y;
                if (coll.size.z > biggset) biggset = coll.size.z;

                Vector3 targetSize = coll.size;
                if (coll.size.x < biggset) targetSize.x = (biggset / 2f) * bone.Generator_ScaleMul;
                if (coll.size.y < biggset) targetSize.y = (biggset / 2f) * bone.Generator_ScaleMul;
                if (coll.size.z < biggset) targetSize.z = (biggset / 2f) * bone.Generator_ScaleMul;

                targetSize = Vector3.Scale(targetSize, bone.Generator_BoxScale);
                //if (coll.size.x == biggset) targetSize.x *= bone.Generator_LengthMul;
                //if (coll.size.y == biggset) targetSize.y *= bone.Generator_LengthMul;
                //if (coll.size.z == biggset) targetSize.z *= bone.Generator_LengthMul;

                coll.size = targetSize * scl;
                coll.center = center * scl + bone.Generator_Offset;
            }
            else if (bone.ColliderType == RagdollProcessor.RagdollBoneSetup.EColliderType.Sphere)
            {
                SphereCollider coll = AddIfDontHave<SphereCollider>(bone.t);
                coll.center = center * scl + bone.Generator_Offset;
                coll.radius = radius * scl * 1f * bone.Generator_ScaleMul;
            }
            else if (bone.ColliderType == RagdollProcessor.RagdollBoneSetup.EColliderType.Mesh)
            {
            }

            Rigidbody rig = bone.t.GetComponent<Rigidbody>();
            if (rig)
            {
                rig.interpolation = interpolation;
            }

        }

        /// <summary>
        /// Choosing vector with largest element value to define rounded axis if base transform is offsetted
        /// </summary>
        public static Vector3 Prepare_ChooseDominantAxis(Vector3 axis)
        {
            Vector3 abs = new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z));

            if (abs.x > abs.y)
            {
                if (abs.z > abs.x)
                    return new Vector3(0f, 0f, axis.z > 0f ? 1f : -1f);
                else
                    return new Vector3(axis.x > 0f ? 1f : -1f, 0f, 0f);
            }
            else
                if (abs.z > abs.y) return new Vector3(0f, 0f, axis.z > 0f ? 1f : -1f);
            else
                return new Vector3(0f, axis.y > 0f ? 1f : -1f, 0f);
        }

        public void Ragdoll_RemoveFrom(Transform bone)
        {
            DestroyIfHave<ConfigurableJoint>(bone);
            DestroyIfHave<CharacterJoint>(bone);
            DestroyIfHaveIgnoreTriggers<CapsuleCollider>(bone);
            DestroyIfHaveIgnoreTriggers<SphereCollider>(bone);
            DestroyIfHaveIgnoreTriggers<BoxCollider>(bone);
            DestroyIfHave<Rigidbody>(bone);
        }

        public T AddIfDontHave<T>(Transform owner) where T : Component
        {
            T comp = owner.GetComponent<T>();
            if (comp == null) comp = owner.gameObject.AddComponent<T>();
            return comp;
        }

        public void DestroyIfHave<T>(Transform owner) where T : Component
        {
            if (owner == null) return;
            T comp = owner.GetComponent<T>();
            if (comp != null) GameObject.DestroyImmediate(comp);
        }

        public void DestroyIfHaveIgnoreTriggers<T>(Transform owner) where T : Collider
        {
            if (owner == null) return;
            T comp = owner.GetComponent<T>();
            if (comp != null) if (comp.isTrigger == false) GameObject.DestroyImmediate(comp);
        }
    }
}
