using System;
using System.Threading.Tasks;

namespace Smartex.AR.Contracts
{
    /// <summary>
    /// Module E owns this.
    /// Production: WebRTC + WebSocket signaling via /ws/ar-session/{id}.
    /// Dev/editor: MockRemoteAssistService plays a canned loop.
    /// </summary>
    public interface IRemoteAssistService
    {
        Task<Session> StartSession(string deviceId);

        event Action<Annotation> OnAnnotationReceived;  // target-local position
        event Action<string>     OnExpertMessage;       // chat / IEIA push

        Task EndSession();
    }
}
