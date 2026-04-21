using NUnit.Framework;
using UnityEngine;
using Smartex.AR.Contracts;
using Smartex.AR.Contracts.Mocks;

namespace Smartex.Tests.EditMode
{
    /// <summary>
    /// Integration test for the B-module contract via its mock implementation.
    /// This is what consumers (C, D, E, F) rely on while Vuforia isn't wired —
    /// if this ever stops working the whole parallel-development plan breaks.
    /// </summary>
    public class MockMachineRecognizerTests
    {
        GameObject _go;
        MockMachineRecognizer _mock;

        [SetUp]
        public void SetUp()
        {
            _go   = new GameObject("MockRecognizer");
            _mock = _go.AddComponent<MockMachineRecognizer>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            // ARServices holds a static reference — clear it so tests don't leak.
            ARServices.ClearAll();
        }

        [Test]
        public void EmitFake_FiresOnMachineRecognized_WithMatchingDeviceId()
        {
            RecognizedMachine captured = null;
            _mock.OnMachineRecognized += m => captured = m;

            _mock.EmitFake("ESP32_TEX_003");

            Assert.IsNotNull(captured, "OnMachineRecognized must fire synchronously.");
            Assert.AreEqual("ESP32_TEX_003", captured.DeviceId);
            Assert.IsNotNull(captured.AnchorTransform,
                "AnchorTransform is the contract consumers parent UI under — never null.");
            Assert.IsNotNull(captured.Data);
            Assert.AreEqual("ESP32_TEX_003", captured.Data.device_id);
        }

        [Test]
        public void EmitFake_ReusesAnchor_ForSameDeviceId()
        {
            Transform first  = null;
            Transform second = null;
            _mock.OnMachineRecognized += m => { if (first == null) first = m.AnchorTransform; else second = m.AnchorTransform; };

            _mock.EmitFake("ESP32_TEX_001");
            _mock.EmitFake("ESP32_TEX_001");

            Assert.AreSame(first, second,
                "Re-emitting the same device must not orphan the previous anchor — consumers parent content under it.");
        }

        [Test]
        public void Awake_RegistersItselfWithARServices()
        {
            Assert.AreSame(_mock, ARServices.Recognizer,
                "Mock must self-register so consumers can resolve it without scene wiring.");
        }

        [Test]
        public void EmitFake_ProducesDeterministicData()
        {
            RecognizedMachine a = null, b = null;
            _mock.OnMachineRecognized += m => { if (a == null) a = m; else b = m; };

            _mock.EmitFake("ESP32_TEX_004");
            _mock.EmitFake("ESP32_TEX_004");

            // Same seed → same power. Keeps UI stable across editor replays.
            Assert.AreEqual(a.Data.avg_power_watts, b.Data.avg_power_watts, 0.001f);
        }
    }
}
