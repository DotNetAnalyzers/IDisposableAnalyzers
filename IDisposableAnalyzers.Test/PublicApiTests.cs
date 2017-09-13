// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace IDisposableAnalyzers.Test
{
    using System.Text;

    using NUnit.Framework;

    /// <summary>
    /// Unit tests related to the public API surface of StyleCop.Analyzers.dll.
    /// </summary>
    public class PublicApiTests
    {
        /// <summary>
        /// This test ensures all types in StyleCop.Analyzers.dll are marked internal.
        /// </summary>
        [Test]
        public void TestAllTypesAreInternal()
        {
            var publicTypes = new StringBuilder();
            foreach (var type in typeof(AnalyzerCategory).Assembly.ExportedTypes)
            {
                if (publicTypes.Length > 0)
                {
                    publicTypes.Append(", ");
                }

                publicTypes.Append(type.Name);
            }

            Assert.AreEqual(string.Empty, publicTypes.ToString());
        }
    }
}
