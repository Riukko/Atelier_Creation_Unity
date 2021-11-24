using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OtterController : MonoBehaviour
{
    public Transform headBone;
    public Transform targetTransform;
    public Transform rightEye;
    public Transform leftEye;

    //Head parameters
    [SerializeField] float headRotationSpeed = 2f;
    [SerializeField] int maxHeadRotationangle = 30;

    //Eyes parameters
    [SerializeField] float eyeTrackingSpeed = 3f;
    [SerializeField] float leftEyeMaxYRotation;
    [SerializeField] float leftEyeMinYRotation;
    [SerializeField] float rightEyeMaxYRotation;
    [SerializeField] float rightEyeMinYRotation;


    void LateUpdate()
    {
        HeadTrackingUpdate();
    }

    void HeadTrackingUpdate()
    {
        //Storing the current head rotation
        Quaternion currentLocalHeadRotation = headBone.localRotation;

        //We want to work in world space to apply angle limit on the head rotation, so we're gonna transform local to word space
        headBone.localRotation = Quaternion.identity;

        //Vector from the head to the object to look at, in world position
        Vector3 headToObjectWorldVector = headBone.position - targetTransform.position;
        //Vector from the head to the object to look at, in local position
        Vector3 headToObjectLocalVector = headBone.parent.InverseTransformDirection(headToObjectWorldVector);

        //Head rotation angle limit
        headToObjectLocalVector = Vector3.RotateTowards(
            Vector3.forward,
            headToObjectLocalVector,
            Mathf.Deg2Rad * maxHeadRotationangle,
            0
        );

        //The head rotation that the model should have to look at the object
        Quaternion targetLocalHeadRotation = Quaternion.LookRotation(headToObjectLocalVector, Vector3.up);

        //Lerp to smooth the head rotation
        headBone.rotation = Quaternion.Slerp(
            currentLocalHeadRotation,
            targetLocalHeadRotation,
            1 - Mathf.Exp(-headRotationSpeed * Time.deltaTime)
        );
    }

    void EyeTrackingUpdate()
    {
        Quaternion targetEyeRotation = Quaternion.LookRotation(
          targetTransform.position - headBone.position, // toward target
          transform.up
        );

        leftEye.rotation = Quaternion.Slerp(
          leftEye.rotation,
          targetEyeRotation,
          1 - Mathf.Exp(-eyeTrackingSpeed * Time.deltaTime)
        );

        rightEye.rotation = Quaternion.Slerp(
          rightEye.rotation,
          targetEyeRotation,
          1 - Mathf.Exp(-eyeTrackingSpeed * Time.deltaTime)
        );
    }
}


