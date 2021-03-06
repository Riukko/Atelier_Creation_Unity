using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OtterController : MonoBehaviour
{

    [SerializeField] Transform targetTransform;
    

    //Root motion parameters
    [Header("Body movement parameters")]
    // How fast we can turn and move 
    [SerializeField] float turnSpeed;
    [SerializeField] float moveSpeed;
    // How fast we will reach the above speeds
    [SerializeField] float turnAcceleration;
    [SerializeField] float moveAcceleration;
    // Try to stay in this range from the target
    [SerializeField] float minDistToTarget;
    [SerializeField] float maxDistToTarget;
    // If we are above this angle from the target, start turning
    [SerializeField] float maxAngToTarget;
    //The transform point to calculate the distance with the target
    //(Using the transform of the model gives a point around the belly and the object is as high as the torso)
    [SerializeField] Transform pointToCalculateDistance;
    // World space velocity
    Vector3 currentVelocity;
    // We are only doing a rotation around the up axis, so we only use a float here
    float currentAngularVelocity;

    //Head parameters
    [Header("Head parameters and components")]
    [SerializeField] Transform headBone;
    [SerializeField] float headRotationSpeed = 2f;
    [SerializeField] int maxHeadRotationangle = 30;
    Quaternion lastWorldRotation = Quaternion.identity;

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

    //Legs parameters
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
    [SerializeField] float stepOvershootFractionUpperBound;
    [SerializeField] float stepOverShootFractionLowerBound;

    bool legIsMoving;
    Transform lastLegWhoHasMoved;

    //Arms parameters
    [Header("Arms parameters and components")]
    [SerializeField] Transform RightArmTarget;
    [SerializeField] Transform LeftArmTarget;
    [SerializeField] Transform RightHand;
    [SerializeField] Transform LeftHand;
    [SerializeField] Transform LeftHandGrab;
    [SerializeField] Transform RightHandGrab;
    [SerializeField] float ArmMovementSpeed;

    //Facial animations parameters
    //The facial animations will be done completely via code
    [Header("Facial Animations components and parameters")]
    [SerializeField] SkinnedMeshRenderer meshRenderer;
    [SerializeField] float facialAnimationDuration;
    [SerializeField] float distanceToHappy;

    Coroutine animCoroutine = null;
    [SerializeField]  bool isHappy = false;
    [SerializeField] bool isSad = false;

    //Blend shapes are the components used to change the facial expressions of our characters
    //Blend shapes index used for the sad animation
    int Sad_Mouth_Index;
    int Small_Pupil_Index;
    int Sad_Face_Index;

    //Blend shaped index used for the happy animation
    int Happy_Face_Index;
    int Open_Mouth_1_Index;
    




    private void Start()
    {
        //Get the blend shapes indexes
        Small_Pupil_Index = meshRenderer.sharedMesh.GetBlendShapeIndex("Pupil small");
        Sad_Face_Index = meshRenderer.sharedMesh.GetBlendShapeIndex("Sad");
        Sad_Mouth_Index = meshRenderer.sharedMesh.GetBlendShapeIndex("sad mouth");

        Happy_Face_Index = meshRenderer.sharedMesh.GetBlendShapeIndex("Happy closed eyes");
        Open_Mouth_1_Index = meshRenderer.sharedMesh.GetBlendShapeIndex("vrc.v_kk");

        Debug.Log(Sad_Mouth_Index);
    }


    void Update()
    {
        CheckIfEmotionChanges();
    }

    void LateUpdate()
    {
        EyeTrackingUpdate();
        LegStepping();
        RootMotionUpdate();
        ArmMovement();
        HeadTrackingUpdate();
    }

    void HeadTrackingUpdate()
    {
        //Storing the current head rotation
        Quaternion currentHeadRotation = headBone.rotation;

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
        Quaternion targetLocalHeadRotation = Quaternion.LookRotation(headToObjectWorldVector, Vector3.up);

        //Lerp to smooth the head rotation
        headBone.rotation = Quaternion.Slerp(
        currentHeadRotation,
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

    #region Leg Movement
    void LegStepping()
    {

        //We check which leg should move based on the last one who has moved
        if (lastLegWhoHasMoved == RightLegTarget && !RightLegTarget.gameObject.GetComponent<LegHandler>().legIsMoving)
        {
            TryMoveLeg(LeftLegTarget);
        }
        else if (!LeftLegTarget.gameObject.GetComponent<LegHandler>().legIsMoving)
        {
            TryMoveLeg(RightLegTarget);
        }

    }

    void TryMoveLeg(Transform legToMove)
    {
        //If the leg is currently moving, we shouldn't start the coroutine again
        if (legToMove.gameObject.GetComponent<LegHandler>().legIsMoving) return;


        float distanceFromHome = Vector3.Distance(legToMove.position, legToMove.gameObject.GetComponent<LegHandler>().legHome.position);


        if (distanceFromHome > distanceForStep)
        {
            StartCoroutine(LegMoveToHome(legToMove));
        }

    }

    IEnumerator LegMoveToHome(Transform targetLeg)
    {

        targetLeg.gameObject.GetComponent<LegHandler>().legIsMoving = true;

        //We store the last leg who has moved to make sure that they move alternatively between right and left
        lastLegWhoHasMoved = targetLeg;

        //Get the home position of the leg, it's the position that we want to reach if we're too far from it
        Transform currentLegHome = targetLeg.gameObject.GetComponent<LegHandler>().legHome;

        //The position and rotation before the movement
        Quaternion startRotation = targetLeg.rotation;
        Vector3 startPosition = targetLeg.position;

        //Vector from the foot to the home position that we want to reach
        Vector3 towardHome = currentLegHome.position - transform.position;

        //Calcultation of the total distance to reach with an overshoot in order to make it more natural : the foot won't step right under the body
        float stepWithOvershoot = distanceForStep;
            
        Vector3 stepWithOvershootVector = towardHome * stepWithOvershoot;
        
        stepWithOvershootVector = Vector3.ProjectOnPlane(stepWithOvershootVector, Vector3.up);
        //stepWithOvershootVector = new Vector3(0, 0, stepWithOvershootVector.z);


        //We apply the overshoot to the position we want to reach
        //Vector3 endPosition = currentLegHome.position + stepWithOvershootVector;
        //The overshoot is scaled on the velocity of the character
        Vector3 endPosition = currentLegHome.position + (currentVelocity * 0.3f);

        //Calculating the center point of the distance in order to make a curve with the leg
        Vector3 stepCenterPoint = (startPosition + endPosition) / 2;
        //We have to lift the leg up to make a curve
        stepCenterPoint += -currentLegHome.forward * Vector3.Distance(startPosition, endPosition) * 0.5f;

        //The end rotation that we want to reach
        Quaternion endRotation = currentLegHome.localRotation;


        //Start timer for the lerp
        float timeElapsed = 0;

        // We make the movement of the leg towards the home position using a lerp to smooth it down. 
        // The while is here to stop when we reach the timer.
        do
        {
            // Add time since last frame to the time elapsed
            timeElapsed += Time.deltaTime;
            //We clamp the time value between 0 and 1
            float normalizedTime = timeElapsed / footMovementDuration;

            //Applying a bezier curve to the movement
            targetLeg.position =
                Vector3.Lerp(
                    Vector3.Lerp(startPosition, stepCenterPoint, normalizedTime),
                    Vector3.Lerp(stepCenterPoint, endPosition, normalizedTime),
                    normalizedTime
            );

            //Smoothing the movement of the rotation if the leg needs one
            //targetLeg.rotation = Quaternion.Slerp(startRotation, endRotation, normalizedTime);
            targetLeg.rotation = Quaternion.LookRotation(-transform.forward, Vector3.up);

            // Wait for the end of the frame
            yield return null;
        }
        while (timeElapsed < footMovementDuration);

        // Done moving
        targetLeg.gameObject.GetComponent<LegHandler>().legIsMoving = false;
    }

    #endregion
    void RootMotionUpdate()
    {
        // Get the direction toward our target
        Vector3 towardTarget = targetTransform.position - transform.position;
        // Vector toward target on the local XZ plane
        Vector3 towardTargetProjected = Vector3.ProjectOnPlane(towardTarget, transform.up);
        // Get the angle from the gecko's forward direction to the direction toward toward our target
        // Here we get the signed angle around the up vector so we know which direction to turn in
        float angToTarget = Vector3.SignedAngle(transform.forward, towardTargetProjected, transform.up);

        float targetAngularVelocity = 0;

        // If we are within the max angle (facing the target)
        // leave the target angular velocity at zero
        if (Mathf.Abs(angToTarget) > maxAngToTarget)
        {
            if (angToTarget > 0)
            {
                targetAngularVelocity = turnSpeed;
            }
            // Invert angular speed if target is to our left
            else
            {
                targetAngularVelocity = -turnSpeed;
            }
        }

        // Use our smoothing function to gradually change the velocity
        currentAngularVelocity = Mathf.Lerp(
          currentAngularVelocity,
          targetAngularVelocity,
          1 - Mathf.Exp(-turnAcceleration * Time.deltaTime)
        );

        // Rotate the transform around the Y axis in world space, 
        // making sure to multiply by delta time to get a consistent angular velocity
        transform.Rotate(0, Time.deltaTime * currentAngularVelocity, 0, Space.World);

        Vector3 targetVelocity = Vector3.zero;

        //No need to move if we're facing the target
        if(Mathf.Abs(angToTarget) < 90)
        {
            //Distance from character to target
            float distToTarget = Vector3.Distance(pointToCalculateDistance.position, targetTransform.position);

            //If the target is too far away, the character will walk towards it
            if(distToTarget > maxDistToTarget)
            {
                targetVelocity = moveSpeed * towardTargetProjected.normalized;
            }

            //If the target is too close, the character steps back
            else if(distToTarget < minDistToTarget)
            {
                targetVelocity = moveSpeed * -towardTargetProjected.normalized;
            }
        }
        //Lerp to smooth the movement and apply acceleration
        currentVelocity =
            Vector3.Lerp(
                currentVelocity,
                targetVelocity,
                1 - Mathf.Exp(-moveAcceleration * Time.deltaTime)
        );

        //Applying the velocity
        transform.position += currentVelocity * Time.deltaTime;
    }

    void ArmMovement()
    {
        Vector3 currentLeftArmPosition = LeftArmTarget.position;
        Vector3 currentRightArmPosition = RightArmTarget.position;
        Quaternion currentLeftArmRotation = LeftArmTarget.rotation;
        Quaternion currentRightArmRotation = RightArmTarget.rotation;

        //Set the position and rotation of the arms target to the handles of the target objects (on the side of the target)
        LeftArmTarget.position =
            Vector3.Lerp(
                currentLeftArmPosition,
                LeftHandGrab.position,
                1 - Mathf.Exp(-ArmMovementSpeed * Time.deltaTime)
            );

        RightArmTarget.position =
            Vector3.Lerp(
                currentRightArmPosition,
                RightHandGrab.position,
                1 - Mathf.Exp(-ArmMovementSpeed * Time.deltaTime)
        );

        LeftArmTarget.rotation =
            Quaternion.Slerp(
                currentLeftArmRotation,
                LeftHandGrab.rotation,
                1 - Mathf.Exp(-ArmMovementSpeed * Time.deltaTime)
        );
        RightArmTarget.rotation = 
            Quaternion.Slerp(
                currentRightArmRotation,
                RightHandGrab.rotation,
                1 - Mathf.Exp(-ArmMovementSpeed * Time.deltaTime)
        );
    }


    //For the facial expressions, we check how far the character is from the target and makes him happy or sad depending on this
    void CheckIfEmotionChanges()
    {
        //Calculate the distance between the character and the target
        float distToTarget = Vector3.Distance(pointToCalculateDistance.position, targetTransform.position);

        //We check if we aren't already in the emotion state that we want to change in, to avoid multiply coroutine calls
        // and then we check the distance with the target to know if it should be sad or happy
        if(isSad && distToTarget < distanceToHappy && animCoroutine == null)
        {
            isHappy = true;
            isSad = false;
            //We store the coroutine in the value to make sure that it's finished before calling it again
            animCoroutine = StartCoroutine(HappyFaceAnimation());
        }
        else if (isHappy && distToTarget > distanceToHappy && animCoroutine == null)
        {
            isSad = true;
            isHappy = false;
            animCoroutine = StartCoroutine(SadFaceAnimation());
        }

        
    }

    //The coroutines lerp the blend shapes values to reach in order to make the facial expressions change smoothly
    IEnumerator SadFaceAnimation()
    {

        float elapsedTime = 0f;
        float normalizedTime;
        float blendShapeWeight = 0f;
        
        //While the time that has elapsed has not reached the end of the timer, we loop
        while(elapsedTime < facialAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            normalizedTime = elapsedTime / facialAnimationDuration;

            blendShapeWeight = Mathf.Lerp(0, 100, normalizedTime);

            //We smoothly changes the facial expression from one to another
            meshRenderer.SetBlendShapeWeight(Small_Pupil_Index, blendShapeWeight);
            meshRenderer.SetBlendShapeWeight(Sad_Face_Index, blendShapeWeight);
            meshRenderer.SetBlendShapeWeight(Small_Pupil_Index, blendShapeWeight);

            meshRenderer.SetBlendShapeWeight(Happy_Face_Index, 100 - blendShapeWeight);
            meshRenderer.SetBlendShapeWeight(Open_Mouth_1_Index, 100 - blendShapeWeight);

            yield return null;
        }

        animCoroutine = null;
        
    }

    IEnumerator HappyFaceAnimation()
    {

        float elapsedTime = 0f;
        float normalizedTime;
        float blendShapeWeight = 0f;
        while (elapsedTime < facialAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            normalizedTime = elapsedTime / facialAnimationDuration;

            blendShapeWeight = Mathf.Lerp(0, 100, normalizedTime);

            meshRenderer.SetBlendShapeWeight(Happy_Face_Index, blendShapeWeight);
            meshRenderer.SetBlendShapeWeight(Open_Mouth_1_Index, blendShapeWeight);

            meshRenderer.SetBlendShapeWeight(Small_Pupil_Index, 100 - blendShapeWeight);
            meshRenderer.SetBlendShapeWeight(Sad_Face_Index, 100 - blendShapeWeight);
            meshRenderer.SetBlendShapeWeight(Small_Pupil_Index, 100 - blendShapeWeight);

            yield return null;

        }

        animCoroutine = null;
        
    }
}
 


