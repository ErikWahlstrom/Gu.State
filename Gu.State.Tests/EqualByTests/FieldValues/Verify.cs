﻿namespace Gu.State.Tests.EqualByTests.FieldValues
{
    using System;

    public class Verify : VerifyTests
    {
        public override void VerifyMethod<T>(
            ReferenceHandling referenceHandling = ReferenceHandling.Structural,
            string excludedMembers = null,
            Type ignoredType = null,
            Type immutableType = null)
        {
            var builder = FieldsSettings.Build();
            if (excludedMembers != null)
            {
                builder.AddIgnoredField<T>(excludedMembers);
            }

            if (ignoredType != null)
            {
                builder.AddImmutableType(ignoredType);
            }

            if (immutableType != null)
            {
                builder.AddImmutableType(immutableType);
            }

            var settings = builder.CreateSettings(referenceHandling);
            EqualBy.VerifyCanEqualByFieldValues<T>(settings);
        }
    }
}