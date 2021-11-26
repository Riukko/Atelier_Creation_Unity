using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OtterController : MonoBehaviour
{

    [SerializeField] Transform targetTransform;


    //Head parameters
    [Header("Head parameters and components")]
    [SerializeField] Transform headBone;
    [SerializeField] float headRotationSpeed = 2f;
    [SerializeField] int maxHeadRotationangle = 30;

    //Eyes parameters
    [Header("Eyes parameters and components")]
    [SerializeField] Transform rightEye;
    [SerializeField] Transform leftEye;
    [SerializeField] float eyeTrackingSpeed = 3f;
    [SerializeField] float leftEyeMaxYRotation;
    [SerializeField] float leftEyeMinYRotation;
    [SerializeField] float rightEyeMaxYRotation;
    [SerializeField] float rightEyeMinYRotation;

    [SerializeField] float leftEyeMaxXRotation;
    [SerializeField] float leftEyeMinXRotation;
    [SerializeField] float rightEyeMaxXRotation;
    [SerializeField] float rightEyeMinXRotation;

    //Legs parameters and components
    [Header("Legs parameters and components")]
    [SerializeField] Transform RightLegTarget;
    [SerializeField] Transform LeftLegTarget;
    [SerializeField] Transform RightKnee;
    [SerializeField] Transform LeftKnee;
    [SerializeField] Transform LeftLegHome;
    [SerializeField] Transform RightLegHome;
    //Stay within this distance of the knee
    [SerializeField] float distanceForStep;
    [SerializeField] float footMovementDuration;
    bool legIsMoving;

    Vector3 initialLeftTargetPos;
    Vector3 initialRightTargetPos;


    private void Start()
    {


    }

    void LateUpdate()
    {
        HeadTrackingUpdate();
        EyeTrackingUpdate();
        LegStepping();
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
        //Set up the rotation we want the eyes to have to look at the target object
        Quaternion targetEyeRotation = Quaternion.LookRotation(
          headBone.position - targetTransform.position, 
          transform.up
        );

        //Slerps to smooth out the movement of the eyes
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


        //We're clamping the rotation values only on the y and x axis, therefore we can work with Euler angles
        float leftEyeCurrentYRotation = leftEye.localEulerAngles.y;
        float rightEyeCurrentYRotation = rightEye.localEulerAngles.y;

        float leftEyeCurrentXRotation = leftEye.localEulerAngles.x;
        float rightEyeCurrentXRotation = rightEye.localEulerAngles.x;

        // Clamp the rotation to be between -180 and 180
        if (leftEyeCurrentYRotation > 180)
        {
            leftEyeCurrentYRotation -= 360;
        }
        if (rightEyeCurrentYRotation > 180)
        {
            rightEyeCurrentYRotation -= 360;
        }

        if (leftEyeCurrentXRotation > 180)
        {
            leftEyeCurrentXRotation -= 360;
        }
        if (rightEyeCurrentXRotation > 180)
        {
            rightEyeCurrentXRotation -= 360;
        }

        // Clamp the Y and X axis rotation according to the Min/Max Y and X rotation defined in the class attributes
        float leftEyeClampedYRotation = Mathf.Clamp(
            leftEyeCurrentYRotation,
            leftEyeMaxYRotation,
            leftEyeMinYRotation
        );
        float rightEyeClampedYRotation = Mathf.Clamp(
            rightEyeCurrentYRotation,
            rightEyeMinYRotation,
            rightEyeMaxYRotation
        );

        float leftEyeClampedXRotation = Mathf.Clamp(
            leftEyeCurrentXRotation,
            leftEyeMinXRotation,
            leftEyeMaxXRotation);

        float rightEyeClampedXRotation = Mathf.Clamp(
            rightEyeCurrentXRotation,
            rightEyeMinXRotation,
            rightEyeMaxXRotation);

        // Apply the clamped Y and X rotation without changing the X and Z rotations
        leftEye.localEulerAngles = new Vector3(
            leftEyeClampedXRotation,
            leftEyeClampedYRotation,
            0
        );
        rightEye.localEulerAngles = new Vector3(
            rightEyeClampedXRotation,
            rightEyeClampedYRotation,
            0
        );

    }

    void LegStepping()
    {
        if (legIsMoving) return;
        else
        {
            float distanceFromHomeRight = Vector3.Distance(RightLegTarget.position, RightLegHome.position);
            float distanceFromHomeLeft = Vector3.Distance(LeftLegTarget.position, LeftLegHome.position);

            //Debug.Log("Dist Right" + distanceFromHomeRight + "\n Dist Left : " + distanceFromHomeLeft);

            if (distanceFromHomeLeft > distanceForStep)
            {
                StartCoroutine(legMoveToHome(LeftLegTarget));
            }
            if (distanceFromHomeRight > distanceForStep)
            {
                StartCoroutine(legMoveToHome(RightLegTarget));
            }
        }
    }

    IEnumerator legMoveToHome(Transform targetLeg)
    {
        legIsMoving = true;

        Transform currentLegHome = targetLeg.gameObject.GetComponent<LegHandler>().legHome;

        Quaternion startRotation = targetLeg.rotation;
        Vector3 startPosition = targetLeg.position;

        Quaternion endRotation = currentLegHome.localRotation;
        Debug.Log(endRotation.x);
        Vector3 endPosition = currentLegHome.position;

        // Time since step started
        float timeElapsed = 0;

        // Here we use a do-while loop so the normalized time goes past 1.0 on the last iteration,
        // placing us at the end position before ending.
        do
        {
            // Add time since last frame to the time elapsed
            timeElapsed += Time.deltaTime;

            float normalizedTime = timeElapsed / footMovementDuration;

            // Interpolate position and rotation
            targetLeg.transform.position = Vector3.Lerp(startPosition, endPosition, normalizedTime);
            targetLeg.transform.rotation = Quaternion.Slerp(startRotation, endRotation, normalizedTime);

            // Wait for one frame
            yield return null;
        }
        while (timeElapsed < footMovementDuration);

        // Done moving
        legIsMoving = false;
    }
}


