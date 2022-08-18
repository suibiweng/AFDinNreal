/****************************************************************************
* Copyright 2019 Nreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.nreal.ai/        
* 
*****************************************************************************/

using UnityEngine;
using NRKernal;

    /// <summary> Controls the HelloAR example. </summary>
    [HelpURL("https://developer.nreal.ai/develop/unity/controller")]
    public class PlaceModel : MonoBehaviour
    {
        /// <summary> A model to place when a raycast from a user touch hits a plane. </summary>
        public GameObject TargetModel;
        public GameObject Player;

        /// <summary> Updates this object. </summary>
        void Update()
        {
            // If the player doesn't click the trigger button, we are done with this update.
            if (!NRInput.GetButtonDown(ControllerButton.TRIGGER))
            {
                return;
            }

            // Get controller laser origin.
            var handControllerAnchor = NRInput.DomainHand == ControllerHandEnum.Left ? ControllerAnchorEnum.LeftLaserAnchor : ControllerAnchorEnum.RightLaserAnchor;
            Transform laserAnchor = NRInput.AnchorsHelper.GetAnchor(NRInput.RaycastMode == RaycastModeEnum.Gaze ? ControllerAnchorEnum.GazePoseTrackerAnchor : handControllerAnchor);

            RaycastHit hitResult;
            if (Physics.Raycast(new Ray(laserAnchor.transform.position, laserAnchor.transform.forward), out hitResult, 10))
            {
                if (hitResult.collider.gameObject != null && hitResult.collider.gameObject.GetComponent<NRTrackableBehaviour>() != null)
                {
                    var behaviour = hitResult.collider.gameObject.GetComponent<NRTrackableBehaviour>();
                    if (behaviour.Trackable.GetTrackableType() != TrackableType.TRACKABLE_PLANE)
                    {
                        return;
                    }
                    TargetModel.transform.position=hitResult.point;
                    TargetModel.transform.LookAt(new Vector3(TargetModel.transform.position.x,Player.transform.position.y,TargetModel.transform.position.z));


                    // Instantiate Andy model at the hit point / compensate for the hit point rotation.
                   // Instantiate(AndyPlanePrefab, hitResult.point, Quaternion.identity, behaviour.transform);
                }
            }
        }
    }

