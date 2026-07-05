using UnityEngine;
using Unity.Netcode;
using UnityEngine.Animations.Rigging;

public class PlayerAimIK : NetworkBehaviour
{
    [Header("IK Settings")]
    [SerializeField] private float aimSmoothSpeed = 15f;
    [SerializeField] private float targetDistance = 50f;
    [SerializeField] private float maxAngleLimit = 75f;
    
    [Header("Right Hand Aim Offset")]
    [Tooltip("Tweak this while playing to make the gun point perfectly forward!")]
    public Vector3 rightHandAimRotation = new Vector3(90f, 0f, 270f);

    [Header("Left Hand Aim Offset (Two-Handed)")]
    public Vector3 leftHandPositionOffset = new Vector3(-0.1f, 0.05f, 0.45f);
    public Vector3 leftHandAimRotation = new Vector3(0f, 90f, 0f);

    private RigBuilder rigBuilder;
    private Rig rig;
    
    private MultiAimConstraint aimConstraint;
    private Transform aimTarget;
    private Transform spineBone;

    private UnityEngine.Animations.Rigging.TwoBoneIKConstraint rightArmIK;
    private Transform rightArmPivot;
    private Transform rightArmTarget;
    private Transform rightArmHint;

    private UnityEngine.Animations.Rigging.TwoBoneIKConstraint leftArmIK;
    private Transform leftArmTarget;
    private Transform leftArmHint;

    private NetworkVariable<Vector3> netAimTarget = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private NetworkVariable<bool> netIsAiming = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private bool isSetup = false;

    public override void OnNetworkSpawn()
    {
        SetupRig();
    }

    public void SetAimInput(Vector3 targetPos, bool isAiming)
    {
        if (!IsOwner && !IsServer) return;

        netAimTarget.Value = targetPos;
        netIsAiming.Value = isAiming;
    }

    private void SetupRig()
    {
        var anim = GetComponentInChildren<Animator>();
        if (anim == null) return;

        spineBone = anim.GetBoneTransform(HumanBodyBones.Chest);
        if (spineBone == null) 
            spineBone = anim.GetBoneTransform(HumanBodyBones.Spine);

        if (spineBone == null) return;

        var targetObj = new GameObject("IK_AimTarget");
        targetObj.transform.SetParent(transform);
        aimTarget = targetObj.transform;

        var rigObj = new GameObject("IK_Rig");
        rigObj.transform.SetParent(anim.transform);
        rig = rigObj.AddComponent<Rig>();
        rig.weight = 1f;

        var aimObj = new GameObject("Spine_MultiAim");
        aimObj.transform.SetParent(rigObj.transform);
        aimConstraint = aimObj.AddComponent<MultiAimConstraint>();

        var data = aimConstraint.data;
        data.constrainedObject = spineBone;
        
        var sourceObjects = new WeightedTransformArray();
        sourceObjects.Add(new WeightedTransform(aimTarget, 1f));
        data.sourceObjects = sourceObjects;
        
        data.aimAxis = MultiAimConstraintData.Axis.Z;
        data.upAxis = MultiAimConstraintData.Axis.Y;
        
        data.constrainedXAxis = true;
        data.constrainedYAxis = true;
        data.constrainedZAxis = false; 

        data.limits = new Vector2(-maxAngleLimit, maxAngleLimit);

        aimConstraint.data = data;

        var rightArm = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
        var rightForeArm = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
        var rightHand = anim.GetBoneTransform(HumanBodyBones.RightHand);

        if (rightArm != null && rightForeArm != null && rightHand != null)
        {
            var pivotObj = new GameObject("RightArm_Pivot");
            pivotObj.transform.SetParent(transform);
            pivotObj.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            rightArmPivot = pivotObj.transform;

            var armTargetObj = new GameObject("RightArm_Target");
            armTargetObj.transform.SetParent(rightArmPivot);
            
            armTargetObj.transform.localPosition = new Vector3(0.35f, 0.0f, 1.0f); 
            
            armTargetObj.transform.localRotation = Quaternion.Euler(rightHandAimRotation);

            rightArmTarget = armTargetObj.transform;

            var armHintObj = new GameObject("RightArm_Hint");
            armHintObj.transform.SetParent(rightArmPivot);
            armHintObj.transform.localPosition = new Vector3(1.0f, 0.0f, 0.0f);
            rightArmHint = armHintObj.transform;

            var armIKObj = new GameObject("RightArm_TwoBoneIK");
            armIKObj.transform.SetParent(rigObj.transform);
            rightArmIK = armIKObj.AddComponent<UnityEngine.Animations.Rigging.TwoBoneIKConstraint>();

            var ikData = rightArmIK.data;
            ikData.root = rightArm;
            ikData.mid = rightForeArm;
            ikData.tip = rightHand;
            ikData.target = rightArmTarget;
            ikData.hint = rightArmHint;
            ikData.targetPositionWeight = 1f;
            ikData.targetRotationWeight = 1f;
            ikData.hintWeight = 1f;
            rightArmIK.data = ikData;
        }

        var leftArm = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        var leftForeArm = anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        var leftHand = anim.GetBoneTransform(HumanBodyBones.LeftHand);

        if (leftArm != null && leftForeArm != null && leftHand != null && rightArmTarget != null)
        {
            var leftTargetObj = new GameObject("LeftArm_Target");
            leftTargetObj.transform.SetParent(rightArmTarget);
            leftTargetObj.transform.localPosition = leftHandPositionOffset;
            leftTargetObj.transform.localRotation = Quaternion.Euler(leftHandAimRotation);
            leftArmTarget = leftTargetObj.transform;

            var leftHintObj = new GameObject("LeftArm_Hint");
            leftHintObj.transform.SetParent(transform);
            leftHintObj.transform.localPosition = new Vector3(-1.0f, 1.4f, 0.0f);
            leftArmHint = leftHintObj.transform;

            var leftIKObj = new GameObject("LeftArm_TwoBoneIK");
            leftIKObj.transform.SetParent(rigObj.transform);
            leftArmIK = leftIKObj.AddComponent<UnityEngine.Animations.Rigging.TwoBoneIKConstraint>();

            var ikDataL = leftArmIK.data;
            ikDataL.root = leftArm;
            ikDataL.mid = leftForeArm;
            ikDataL.tip = leftHand;
            ikDataL.target = leftArmTarget;
            ikDataL.hint = leftArmHint;
            ikDataL.targetPositionWeight = 1f;
            ikDataL.targetRotationWeight = 1f;
            ikDataL.hintWeight = 1f;
            leftArmIK.data = ikDataL;
        }

        rigBuilder = anim.gameObject.GetComponent<RigBuilder>();
        if (rigBuilder == null)
            rigBuilder = anim.gameObject.AddComponent<RigBuilder>();

        rigBuilder.layers.Add(new RigLayer(rig));
        rigBuilder.Build();
        
        isSetup = true;
    }

    private void LateUpdate()
    {
        if (!isSetup || aimTarget == null) return;

        if (netAimTarget.Value != Vector3.zero)
        {
            aimTarget.position = Vector3.Lerp(aimTarget.position, netAimTarget.Value, Time.deltaTime * aimSmoothSpeed);
        }

        if (rightArmIK != null)
        {
            float targetWeight = netIsAiming.Value ? 1f : 0f;
            rightArmIK.weight = Mathf.Lerp(rightArmIK.weight, targetWeight, Time.deltaTime * 10f);
            
            if (aimConstraint != null)
            {
                aimConstraint.weight = Mathf.Lerp(aimConstraint.weight, targetWeight, Time.deltaTime * 10f);
            }
            
            if (rightArmPivot != null && aimTarget != null)
            {
                rightArmPivot.LookAt(aimTarget.position);
            }

            if (rightArmTarget != null)
            {
                rightArmTarget.localRotation = Quaternion.Euler(rightHandAimRotation);
            }
        }

        if (leftArmIK != null)
        {
            if (leftArmTarget != null)
            {
                leftArmTarget.localPosition = leftHandPositionOffset;
                leftArmTarget.localRotation = Quaternion.Euler(leftHandAimRotation);
            }

            float leftWeight = 0f;
            if (netIsAiming.Value)
            {
                var inv = GetComponent<PlayerInventory>();
                if (inv != null && inv.EquippedWeaponIndex.Value == 1)
                {
                    leftWeight = 1f;
                }
            }
            leftArmIK.weight = Mathf.Lerp(leftArmIK.weight, leftWeight, Time.deltaTime * 10f);
        }
    }
}
