// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;

#if NVE_HAS_VRCHAT_AVATAR_SDK
using VRC.Dynamics;
using VRC.SDK3.Dynamics.Constraint.Components;
#endif // NVE_HAS_VRCHAT_AVATAR_SDK

// ReSharper disable once CheckNamespace
namespace com.github.hkrn
{
    internal sealed class CircularDependentNodeConstraintDetector
    {
        public CircularDependentNodeConstraintDetector(IDictionary<Transform, gltf.ObjectID> transformNodeIDs)
        {
            foreach (var (transform, _) in transformNodeIDs)
            {
                if (transform.TryGetComponent<AimConstraint>(out var aimConstraint))
                {
                    _constraintTransforms.Add((transform, aimConstraint));
                }
                else if (transform.TryGetComponent<RotationConstraint>(out var rotationConstraint))
                {
                    _constraintTransforms.Add((transform, rotationConstraint));
                }
            }
        }

        public void Visit()
        {
            foreach (var (transform, constraint) in _constraintTransforms)
            {
                VisitRecursive(transform, constraint);
            }
        }

        private void VisitRecursive(Transform transform, IConstraint constraint)
        {
            if (!transform || _doneConstraints.Contains(transform))
            {
                return;
            }

            if (!_pendingConstraints.Add(transform))
            {
                FoundAllTransforms.Add(transform);
                return;
            }

            if (constraint is AimConstraint)
            {
                VisitInner(transform.parent);
            }

            if (constraint.sourceCount > 0)
            {
                VisitInner(constraint.GetSource(0).sourceTransform);
            }

            _pendingConstraints.Remove(transform);
            _doneConstraints.Add(transform);
        }

        private void VisitInner(Transform transform)
        {
            if (!transform)
            {
                return;
            }

            var ancestorTransforms = new List<Transform> { transform };
            var parentTransform = transform.parent;
            while (parentTransform)
            {
                ancestorTransforms.Insert(0, parentTransform);
                parentTransform = parentTransform.parent;
            }

            foreach (var item in ancestorTransforms)
            {
                if (transform.TryGetComponent<AimConstraint>(out var aimConstraint))
                {
                    VisitRecursive(item, aimConstraint);
                }
                else if (transform.TryGetComponent<RotationConstraint>(out var rotationConstraint))
                {
                    VisitRecursive(item, rotationConstraint);
                }
            }
        }

        public ISet<Transform> FoundAllTransforms { get; } = new HashSet<Transform>();

        private readonly IList<(Transform, IConstraint)> _constraintTransforms = new List<(Transform, IConstraint)>();
        private readonly ISet<Transform> _pendingConstraints = new HashSet<Transform>();
        private readonly ISet<Transform> _doneConstraints = new HashSet<Transform>();
    }

#if NVE_HAS_VRCHAT_AVATAR_SDK
    internal sealed class VrcCircularDependentNodeConstraintDetector
    {
        public VrcCircularDependentNodeConstraintDetector(IDictionary<Transform, gltf.ObjectID> transformNodeIDs)
        {
            foreach (var (transform, _) in transformNodeIDs)
            {
                if (transform.TryGetComponent<VRCConstraintBase>(out var vcb))
                {
                    _constraintTransforms.Add((transform, vcb));
                }
            }
        }

        public void Visit()
        {
            foreach (var (transform, constraint) in _constraintTransforms)
            {
                VisitRecursive(transform, constraint);
            }
        }

        private void VisitRecursive(Transform transform, VRCConstraintBase constraint)
        {
            if (!transform || _doneConstraints.Contains(transform))
            {
                return;
            }

            if (!_pendingConstraints.Add(transform))
            {
                FoundAllTransforms.Add(transform);
                return;
            }

            if (constraint is VRCAimConstraint)
            {
                VisitInner(transform.parent);
            }

            if (constraint && constraint.Sources.Count > 0)
            {
                VisitInner(constraint.Sources.First().SourceTransform);
            }

            _pendingConstraints.Remove(transform);
            _doneConstraints.Add(transform);
        }

        private void VisitInner(Transform transform)
        {
            if (!transform)
            {
                return;
            }

            var ancestorTransforms = new List<Transform> { transform };
            var parentTransform = transform.parent;
            while (parentTransform)
            {
                ancestorTransforms.Insert(0, parentTransform);
                parentTransform = parentTransform.parent;
            }

            foreach (var item in ancestorTransforms)
            {
                if (item.TryGetComponent<VRCConstraintBase>(out var constraint))
                {
                    VisitRecursive(item, constraint);
                }
            }
        }

        public ISet<Transform> FoundAllTransforms { get; } = new HashSet<Transform>();

        private readonly IList<(Transform, VRCConstraintBase)> _constraintTransforms =
            new List<(Transform, VRCConstraintBase)>();

        private readonly ISet<Transform> _pendingConstraints = new HashSet<Transform>();
        private readonly ISet<Transform> _doneConstraints = new HashSet<Transform>();
    }
#endif // NVE_HAS_VRCHAT_AVATAR_SDK
}
