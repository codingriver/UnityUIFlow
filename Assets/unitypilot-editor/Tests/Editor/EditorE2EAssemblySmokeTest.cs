// -----------------------------------------------------------------------
// UnityPilot — M26 Phase B: EditMode test assembly smoke (UTF).
// SPDX-License-Identifier: MIT
// -----------------------------------------------------------------------

using NUnit.Framework;

namespace codingriver.unity.pilot.tests
{
    /// <summary>
    /// Verifies the Editor Tests assembly loads; extend with VisualElement-driven checks as needed.
    /// </summary>
    public sealed class EditorE2EAssemblySmokeTest
    {
        [Test]
        public void EditorTests_Assembly_Resolves()
        {
            Assert.Pass("UnityPilot.Editor.Tests loaded.");
        }
    }
}
