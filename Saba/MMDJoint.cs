﻿using BulletSharp;
using Evergine.Mathematics;
using Saba.Helpers;
using Silk.NET.Maths;

namespace Saba;

public class MMDJoint : IDisposable
{
    public TypedConstraint Constraint { get; }

    public MMDJoint(PmxJoint pmxJoint, MMDRigidBody rigidBodyA, MMDRigidBody rigidBodyB)
    {
        Matrix4X4<float> matrix = Matrix4X4.CreateRotationX(pmxJoint.Rotate.X)
                                  * Matrix4X4.CreateRotationY(pmxJoint.Rotate.Y)
                                  * Matrix4X4.CreateRotationZ(pmxJoint.Rotate.Z)
                                  * Matrix4X4.CreateTranslation(pmxJoint.Translate);

        Matrix4x4 transform = matrix.ToBtTransform();

        Matrix4x4 invA = Matrix4x4.Invert(rigidBodyA.RigidBody.WorldTransform);
        Matrix4x4 invB = Matrix4x4.Invert(rigidBodyB.RigidBody.WorldTransform);
        invA = transform * invA;
        invB = transform * invB;

        Generic6DofSpringConstraint constraint = new(rigidBodyA.RigidBody, rigidBodyB.RigidBody, invA, invB, true)
        {
            LinearLowerLimit = new Vector3(pmxJoint.TranslateLowerLimit.X,
                                           pmxJoint.TranslateLowerLimit.Y,
                                           pmxJoint.TranslateLowerLimit.Z),
            LinearUpperLimit = new Vector3(pmxJoint.TranslateUpperLimit.X,
                                           pmxJoint.TranslateUpperLimit.Y,
                                           pmxJoint.TranslateUpperLimit.Z),
            AngularLowerLimit = new Vector3(pmxJoint.RotateLowerLimit.X,
                                            pmxJoint.RotateLowerLimit.Y,
                                            pmxJoint.RotateLowerLimit.Z),
            AngularUpperLimit = new Vector3(pmxJoint.RotateUpperLimit.X,
                                            pmxJoint.RotateUpperLimit.Y,
                                            pmxJoint.RotateUpperLimit.Z)
        };

        if (pmxJoint.SpringTranslate.X != 0.0f)
        {
            constraint.EnableSpring(0, true);
            constraint.SetStiffness(0, pmxJoint.SpringTranslate.X);
        }

        if (pmxJoint.SpringTranslate.Y != 0.0f)
        {
            constraint.EnableSpring(1, true);
            constraint.SetStiffness(1, pmxJoint.SpringTranslate.Y);
        }

        if (pmxJoint.SpringTranslate.Z != 0.0f)
        {
            constraint.EnableSpring(2, true);
            constraint.SetStiffness(2, pmxJoint.SpringTranslate.Z);
        }

        if (pmxJoint.SpringRotate.X != 0.0f)
        {
            constraint.EnableSpring(3, true);
            constraint.SetStiffness(3, pmxJoint.SpringRotate.X);
        }

        if (pmxJoint.SpringRotate.Y != 0.0f)
        {
            constraint.EnableSpring(4, true);
            constraint.SetStiffness(4, pmxJoint.SpringRotate.Y);
        }

        if (pmxJoint.SpringRotate.Z != 0.0f)
        {
            constraint.EnableSpring(5, true);
            constraint.SetStiffness(5, pmxJoint.SpringRotate.Z);
        }

        Constraint = constraint;
    }

    public void Dispose()
    {
        Constraint.Dispose();

        GC.SuppressFinalize(this);
    }
}
