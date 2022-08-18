#if UNITY_EDITOR
using FIMSpace.FEditor;
using UnityEditor;
#endif
using UnityEngine;
using System.Collections;
using System;

namespace FIMSpace.FProceduralAnimation
{
    [AddComponentMenu("FImpossible Creations/Ragdoll Animator")]
    [DefaultExecutionOrder(-1)]
    public class RagdollAnimator : MonoBehaviour
    {
        //[HideInInspector] public bool _EditorDrawSetup = true;

        [SerializeField]
        private RagdollProcessor Processor;

        [Tooltip("! REQUIRED ! Just object with Animator and skeleton as child transforms")]
        public Transform ObjectWithAnimator;
        [Tooltip("If null then it will be found automatically - do manual if you encounter some errors after entering playmode")]
        public Transform RootBone;

        [Space(2)]
        [Tooltip("! OPTIONAL ! Leave here nothing to not use the feature! \n\nObject with bones structure to which ragdoll should try fit with it's pose.\nUseful only if you want to animate ragdoll with other animations than the model body animator.")]
        public Transform CustomRagdollAnimator;

        //[Tooltip("Toggle it if you want to drive ragdoll animator with some custom procedural motion done on the bones, like Tail Animator or some other procedural animation plugin")]
        //public bool CaptureLateUpdate = false;

        [Tooltip("If generated ragdoll should be destroyed when main skeleton root object stops existing")]
        public bool AutoDestroy = true;

        [HideInInspector]
        [Tooltip("When false, then ragdoll dummy skeleton will be generated in playmode, when true, it will be generated in edit mode")]
        public bool PreGenerateDummy = false;

        [Tooltip("Generated ragdoll dummy will be put inside this transform as child object.\n\nAssign main character object for ragdoll to react with character movement rigidbody motion, set other for no motion reaction.")]
        public Transform TargetParentForRagdollDummy;


        public RagdollProcessor Parameters { get { return Processor; } }


        private void Reset()
        {
            if (Processor == null) Processor = new RagdollProcessor();
            Processor.TryAutoFindReferences(transform);
            Animator an = GetComponentInChildren<Animator>();
            if (an) ObjectWithAnimator = an.transform;
        }

        private void Start()
        {
            Processor.BackCompabilityCheck();

            Processor.Initialize(this, ObjectWithAnimator, CustomRagdollAnimator, RootBone, TargetParentForRagdollDummy);

            if (AutoDestroy)
            {
                if (!Processor.StartAfterTPose) SetAutoDestroy();
                else StartCoroutine(IEAutoDestroyAfterTPose());
            }

            _initialReposeMode = Parameters.ReposeMode;
        }

        #region Auto Destroy helpers

        IEnumerator IEAutoDestroyAfterTPose()
        {
            while (Parameters.Initialized == false)
            {
                yield return null;
            }

            SetAutoDestroy();
            yield break;
        }

        void SetAutoDestroy()
        {
            autoDestroy = Processor.RagdollDummyBase.gameObject.AddComponent<RagdollAutoDestroy>();
            autoDestroy.Parent = Processor.Pelvis.gameObject;
        }

        #endregion

        private void FixedUpdate()
        {
            Processor.FixedUpdate();
        }

        private void Update()
        {
            Processor.Update();
        }

        private void LateUpdate()
        {
            Processor.LateUpdate();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying == false)
            {
                if (Processor != null)
                    if (Processor._EditorDrawBones)
                        Processor.DrawSetupGizmos();
            }

            Processor.DrawGizmos();
        }


        bool wasDisabled = false;
        private void OnDisable()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
                wasDisabled = true;
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif

                if (wasDisabled)
                {
                    wasDisabled = false;

                    //if (rag.enabled)
                    //{
                    //    rag.enabled = false;
                    //    rag.Parameters.RagdollDummyRoot.gameObject.SetActive(false);
                    //}
                    Parameters.User_PoseAsInitalPose();
                    //rag.enabled = true;
                    Parameters.RagdollDummyRoot.gameObject.SetActive(true);
                    //rag.Parameters.User_PoseAsAnimator();
                }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                Parameters.SwitchAllExtendedAnimatorSync(Parameters.ExtendedAnimatorSync);
            }
        }

#endif

        public RagdollProcessor.EBaseTransformRepose _initialReposeMode { get; set; }

        /// <summary>
        /// Change repose mode to target and restore to the component's initial repose value after the delay
        /// </summary>
        public void User_ChangeReposeAndRestore(RagdollProcessor.EBaseTransformRepose set, float restoreAfter)
        {
            StartCoroutine(IEChangeReposeAndRestore(set, restoreAfter));
        }

        IEnumerator IEChangeReposeAndRestore(RagdollProcessor.EBaseTransformRepose set, float restoreAfter)
        {
            Parameters.ReposeMode = set;
            yield return new WaitForSeconds(restoreAfter);
            Parameters.ReposeMode = _initialReposeMode;
        }

        /// <summary>
        /// Change blend on collision to some value and restore it to prrevious value after delay
        /// </summary>
        public void User_ChangeBlendOnCollisionAndRestore(bool temporaryBlend, float delay)
        {
            StartCoroutine(IEChangeBlendOnCollAndRestore(temporaryBlend, delay));
        }

        IEnumerator IEChangeBlendOnCollAndRestore(bool temporaryBlend, float delay)
        {
            bool toRestore = Parameters.BlendOnCollision;
            Parameters.BlendOnCollision = temporaryBlend;
            yield return new WaitForSeconds(delay);
            Parameters.BlendOnCollision = toRestore;
        }

        /// <summary>
        /// Enable free fall ragdoll mode with delay
        /// </summary>
        public void User_SwitchFreeFallRagdoll(bool freeFall, float delay = 0f)
        {
            if (freeFall == Parameters.FreeFallRagdoll) return;

            if (delay > 0f)
            {
                StartCoroutine(IESwitchFreeFallRagdoll(freeFall, delay));
                return;
            }
            else
            {
                Parameters.SwitchFreeFallRagdoll(freeFall);
            }
        }


        IEnumerator IESwitchFreeFallRagdoll(bool freeFall, float delay)
        {
            yield return new WaitForSeconds(delay);
            User_SwitchFreeFallRagdoll(freeFall);
        }

        /// <summary>
        /// Resulting in bounding box of all ragdoll dummy bones
        /// </summary>
        public Bounds User_GetRagdollBonesStateBounds(bool fast = true)
        {
            return Parameters.User_GetRagdollBonesStateBounds(fast);
        }


        // --------------------------------------------------------------------- UTILITIES


        /// <summary>
        /// Adding physical push impact to single rigidbody limb
        /// </summary>
        /// <param name="limb"> Access 'Parameters' for ragdoll limb </param>
        /// <param name="powerDirection"> World space direction vector </param>
        /// <param name="duration"> Time in seconds </param>
        public void User_SetLimbImpact(Rigidbody limb, Vector3 powerDirection, float duration)
        {
            StartCoroutine(Processor.User_SetLimbImpact(limb, powerDirection, duration));
        }

        /// <summary>
        /// Transitioning ragdoll blend value
        /// </summary>
        public void User_EnableFreeRagdoll(float blend = 1f, float transitionDuration = 0.2f)
        {
            Parameters.SwitchFreeFallRagdoll(true);
            User_FadeRagdolledBlend(blend, transitionDuration);
        }

        /// <summary>
        /// Adding physical push impact to all limbs of the ragdoll
        /// </summary>
        /// <param name="powerDirection"> World space direction vector </param>
        /// <param name="duration"> Time in seconds </param>
        public void User_SetPhysicalImpactAll(Vector3 powerDirection, float duration, ForceMode forceMode = ForceMode.Impulse)
        {
            if (duration <= 0f)
            {
                RagdollProcessor.PosingBone c = Parameters.GetPelvisBone();

                while (c != null)
                {
                    if (c.rigidbody) c.rigidbody.AddForce(powerDirection, forceMode);
                    c = c.child;
                }
            }
            else
            {
                StartCoroutine(Processor.User_SetPhysicalImpactAll(powerDirection, duration, forceMode));
            }
        }

        /// <summary>
        /// Adding physical torque impact to the core limbs
        /// </summary>
        /// <param name="rotationPower"> Rotation angles torque power </param>
        /// <param name="duration"> Time in seconds </param>
        public void User_SetPhysicalTorque(Vector3 rotationPower, float duration, bool relativeSpace = false, ForceMode forceMode = ForceMode.Impulse, bool deltaScale = false)
        {
            if (deltaScale)
            {
                if (Time.fixedDeltaTime > 0f) rotationPower /= Time.fixedDeltaTime;
            }

            if (duration <= 0f)
            {
                RagdollProcessor.PosingBone c = Parameters.GetPelvisBone();

                while (c != null)
                {
                    if (c.rigidbody)
                    {
                        if (relativeSpace) c.rigidbody.AddRelativeTorque(rotationPower, forceMode);
                        else c.rigidbody.AddTorque(rotationPower, forceMode);
                    }

                    c = c.child;
                }
            }
            else
            {
                StartCoroutine(Processor.User_SetPhysicalTorque(rotationPower, duration, relativeSpace, forceMode));
            }
        }

        /// <summary>
        /// Adding physical torque impact to the selected limb
        /// </summary>
        /// <param name="rotationPower"> Rotation angles torque power </param>
        /// <param name="duration"> Time in seconds </param>
        public void User_SetPhysicalTorque(Rigidbody limb, Vector3 rotationPower, float duration, bool relativeSpace = false, ForceMode forceMode = ForceMode.Impulse, bool deltaScale = false)
        {
            if ( deltaScale)
            {
                if (Time.fixedDeltaTime > 0f) rotationPower /= Time.fixedDeltaTime;
            }

            if (duration <= 0f)
            {
                if (relativeSpace)
                    limb.AddRelativeTorque(rotationPower, forceMode);
                else
                    limb.AddTorque(rotationPower, forceMode);
                return;
            }

            StartCoroutine(Processor.User_SetPhysicalTorque(limb, rotationPower, duration, relativeSpace, forceMode));
        }


        /// <summary>
        /// Adding physical torque impact to the core limbs by local euler angles for example of the baseTransform
        /// </summary>
        public void User_SetPhysicalTorqueFromLocal(Vector3 localEuler, Transform localOf, float duration, Vector3? power = null)
        {
            Quaternion rot = FEngineering.QToWorld(localOf.rotation, Quaternion.Euler(localEuler));
            Vector3 angles = FEngineering.WrapVector(rot.eulerAngles);

            if (power != null) angles = Vector3.Scale(angles, power.Value);

            StartCoroutine(Processor.User_SetPhysicalTorque(angles, duration, false));
        }


        /// <summary>
        /// Setting defined velocity value for all limbs of the ragdoll dummy
        /// </summary>
        public void User_SetVelocityAll(Vector3 newVelocity)
        {
            Processor.User_SetAllLimbsVelocity(newVelocity);
        }


        /// <summary>
        /// Enable / disable animator component with delay
        /// </summary>
        public void User_SwitchAnimator(Transform unityAnimator = null, bool enabled = false, float delay = 0f)
        {
            if (unityAnimator == null) unityAnimator = ObjectWithAnimator;
            if (unityAnimator == null) return;

            Animator an = unityAnimator.GetComponent<Animator>();
            if (an)
            {
                StartCoroutine(Processor.User_SwitchAnimator(an, enabled, delay));
            }
        }

        /// <summary>
        /// Triggering different methods which are used in the demo scene for animating getting up from ragdolled state
        /// </summary>
        /// <param name="groundMask"></param>
        public void User_GetUpStack(RagdollProcessor.EGetUpType getUpType, LayerMask groundMask, float targetRagdollBlend = 0f, float targetMusclesPower = 0.85f, float duration = 1.1f)
        {
            StopAllCoroutines();
            User_SwitchAnimator(null, true);
            User_ForceRagdollToAnimatorFor(duration * 0.5f, duration * 0.15f);
            Parameters.FreeFallRagdoll = false;
            User_FadeMuscles(targetMusclesPower, duration, duration * 0.125f);
            User_FadeRagdolledBlend(targetRagdollBlend, duration, duration * 0.125f);
            User_RepositionRoot(null, null, getUpType, groundMask);
            Parameters._User_GetUpResetProbe();
        }

        /// <summary>
        /// Just fre fall off, fade muscles, fade ragdolled blend
        /// + repose switch to none and blend on collisions disable
        /// </summary>
        public void User_GetUpStackV2(float targetRagdollBlend = 0f, float targetMusclesPower = 0.85f, float duration = 1.1f)
        {
            StopAllCoroutines();
            User_SwitchFreeFallRagdoll(false, duration * 0.75f);
            User_FadeMuscles(targetMusclesPower, duration * 0.75f);
            User_FadeRagdolledBlend(targetRagdollBlend, duration * 0.15f, 0f);

            User_ChangeReposeAndRestore(RagdollProcessor.EBaseTransformRepose.None, duration);
            User_ChangeBlendOnCollisionAndRestore(false, duration);
            Parameters._User_GetUpResetProbe();
        }


        /// <summary>
        /// Capture current animator state pose for ragdoll pose drive
        /// </summary>
        public void User_OverrideRagdollStateWithCurrentAnimationState()
        {
            Processor.User_OverrideRagdollStateWithCurrentAnimationState();
        }


        /// <summary>
        /// Force move visible animator bones to the ragdoll pose (with blending amount) if done some
        /// changes between execution order
        /// </summary>
        public void User_UpdateBonesToRagdollPose()
        {
            Processor.UpdateBonesToRagdollPose();
        }


        /// <summary>
        /// Transitioning all rigidbody muscles power to target value
        /// </summary>
        /// <param name="forcePoseEnd"> Target muscle power </param>
        /// <param name="duration"> Transition duration </param>
        /// <param name="delay"> Delay to start transition </param>
        public void User_FadeMuscles(float forcePoseEnd = 0f, float duration = 0.75f, float delay = 0f)
        {
            StartCoroutine(Parameters.User_FadeMuscles(forcePoseEnd, duration, delay));
        }

        /// <summary>
        /// Forcing applying rigidbody pose to the animator pose and fading out to zero smoothly
        /// </summary>
        internal void User_ForceRagdollToAnimatorFor(float duration = 1f, float forcingFullDelay = 0.2f)
        {
            StartCoroutine(Parameters.User_ForceRagdollToAnimatorFor(duration, forcingFullDelay));
        }

        /// <summary>
        /// Transitioning ragdoll blend value
        /// </summary>
        public void User_FadeRagdolledBlend(float targetBlend = 0f, float duration = 0.75f, float delay = 0f)
        {
            StartCoroutine(Parameters.User_FadeRagdolledBlend(targetBlend, duration, delay));
        }

        /// <summary>
        /// Setting all ragdoll limbs rigidbodies kinematic or non kinematic
        /// </summary>
        public void User_SetAllKinematic(bool kinematic = true)
        {
            Parameters.User_SetAllKinematic(kinematic);
        }

        /// <summary>
        /// Setting all ragdoll limbs rigidbodies angular speed limit (by default unity restricts it very tightly)
        /// </summary>
        public void User_SetAllAngularSpeedLimit(float angularLimit)
        {
            Parameters.User_SetAllAngularSpeedLimit(angularLimit);
        }

        /// <summary>
        /// Making pelvis kinematic and anchored to pelvis position
        /// </summary>
        public void User_AnchorPelvis(bool anchor = true, float duration = 0f)
        {
            StartCoroutine(Parameters.User_AnchorPelvis(anchor, duration));
        }

        /// <summary>
        /// Moving ragdoll controller object to fit with current ragdolled position hips
        /// </summary>
        public void User_RepositionRoot(Transform root = null, Vector3? worldUp = null, RagdollProcessor.EGetUpType getupType = RagdollProcessor.EGetUpType.None, LayerMask? snapToGround = null)
        {
            Parameters.User_RepositionRoot(root, null, worldUp, getupType, snapToGround);
        }




        #region Auto Destroy Reference

        private void OnDestroy()
        {
            if (autoDestroy != null) autoDestroy.StartChecking();
        }

        private RagdollAutoDestroy autoDestroy = null;
        private class RagdollAutoDestroy : MonoBehaviour
        {
            public GameObject Parent;
            public void StartChecking() { Check(); if (Parent != null) InvokeRepeating("Check", 0.05f, 0.5f); }
            void Check() { if (Parent == null) Destroy(gameObject); }
        }

        #endregion

    }
}