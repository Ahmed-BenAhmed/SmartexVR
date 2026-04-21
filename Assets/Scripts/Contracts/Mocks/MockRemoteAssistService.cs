using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Smartex.AR.Contracts.Mocks
{
    /// <summary>
    /// Emits a fake annotation + a fake IEIA message ~3 s after StartSession.
    /// Lets Module E's UI be built without WebRTC / backend.
    /// </summary>
    public class MockRemoteAssistService : MonoBehaviour, IRemoteAssistService
    {
        public event Action<Annotation> OnAnnotationReceived;
        public event Action<string>     OnExpertMessage;

        void Awake() => ARServices.Register((IRemoteAssistService)this);

        public Task<Session> StartSession(string deviceId)
        {
            var s = new Session
            {
                SessionId = "mock-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                DeviceId  = deviceId,
                StunUrl   = "stun:stun.l.google.com:19302",
            };
            StartCoroutine(FakeTrafficRoutine(deviceId));
            return Task.FromResult(s);
        }

        public Task EndSession() { StopAllCoroutines(); return Task.CompletedTask; }

        IEnumerator FakeTrafficRoutine(string deviceId)
        {
            yield return new WaitForSeconds(3f);
            OnExpertMessage?.Invoke("Expert: I see it — check the tension sensor on the left.");

            yield return new WaitForSeconds(2f);
            OnAnnotationReceived?.Invoke(new Annotation
            {
                AnnotationId  = "a1",
                DeviceId      = deviceId,
                LocalPosition = new Vector3(-0.12f, 0.18f, 0f),
                Color         = Color.yellow,
                Label         = "here"
            });

            yield return new WaitForSeconds(4f);
            OnExpertMessage?.Invoke("IEIA: recommend scheduling cleaning within 24 h (MAD 180 saved).");
        }
    }
}
