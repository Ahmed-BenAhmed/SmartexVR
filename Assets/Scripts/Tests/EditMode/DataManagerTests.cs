using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Smartex.Core;
using Smartex.Core.Models;

namespace Smartex.Tests.EditMode
{
    /// <summary>
    /// Unit tests for DataManager.GetMachine.
    ///
    /// DataManager is a MonoBehaviour singleton that normally hydrates itself
    /// from an HTTP poll in Start(). For tests we skip all that and drive it
    /// directly: instantiate on a throwaway GameObject, then reflect onto the
    /// private-setter `LastSnapshot` property and inject a hand-built snapshot.
    ///
    /// Why reflection rather than a public test seam? Because shipping code
    /// shouldn't expose a "SetSnapshot()" method just for tests — the contract
    /// is that snapshots come from the poll loop. Reflection keeps the
    /// production API clean.
    /// </summary>
    public class DataManagerTests
    {
        DataManager _dm;
        GameObject  _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestDataManager");
            _dm = _go.AddComponent<DataManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        static void SetLastSnapshot(DataManager dm, FactorySnapshot snap)
        {
            // The property has a private setter — reach it via reflection.
            var prop = typeof(DataManager).GetProperty(
                "LastSnapshot",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(prop, "LastSnapshot property missing — rename detected?");
            prop.SetValue(dm, snap);
        }

        [Test]
        public void GetMachine_ReturnsNull_WhenNoSnapshot()
        {
            Assert.IsNull(_dm.GetMachine("ESP32_TEX_001"),
                "GetMachine must cope with a pre-poll call (LastSnapshot still null) without throwing.");
        }

        [Test]
        public void GetMachine_ReturnsMachine_WhenDeviceIdKnown()
        {
            var snap = new FactorySnapshot();
            snap.machines.Add(new MachineData { device_id = "ESP32_TEX_001", display_name = "Loom 001" });
            snap.machines.Add(new MachineData { device_id = "ESP32_TEX_002", display_name = "Loom 002" });
            SetLastSnapshot(_dm, snap);

            var m = _dm.GetMachine("ESP32_TEX_002");
            Assert.IsNotNull(m);
            Assert.AreEqual("Loom 002", m.display_name);
        }

        [Test]
        public void GetMachine_ReturnsNull_WhenDeviceIdUnknown()
        {
            var snap = new FactorySnapshot();
            snap.machines.Add(new MachineData { device_id = "ESP32_TEX_001" });
            SetLastSnapshot(_dm, snap);

            Assert.IsNull(_dm.GetMachine("ESP32_TEX_999"));
        }

        [Test]
        public void GetMachine_IsCaseSensitive()
        {
            // Documenting current behaviour: device IDs are matched exactly.
            // If we ever decide to relax this, both the test and the production
            // code change together.
            var snap = new FactorySnapshot();
            snap.machines.Add(new MachineData { device_id = "ESP32_TEX_001" });
            SetLastSnapshot(_dm, snap);

            Assert.IsNull(_dm.GetMachine("esp32_tex_001"));
        }
    }
}
