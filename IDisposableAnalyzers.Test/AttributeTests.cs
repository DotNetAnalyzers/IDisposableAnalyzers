// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace IDisposableAnalyzers.Test
{
    using NUnit.Framework;

    public class AttributeTests
    {
        [Test]
        public void TestNoCodeFixAttributeReason()
        {
            var reason = "Reason";
            var attribute = new NoCodeFixAttribute(reason);
            Assert.AreSame(reason, attribute.Reason);
        }

        [Test]
        public void TestNoDiagnosticAttributeReason()
        {
            var reason = "Reason";
            var attribute = new NoDiagnosticAttribute(reason);
            Assert.AreSame(reason, attribute.Reason);
        }
    }
}
