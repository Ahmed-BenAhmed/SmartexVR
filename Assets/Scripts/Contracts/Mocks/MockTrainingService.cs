using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Smartex.AR.Contracts.Mocks
{
    public class MockTrainingService : MonoBehaviour, ITrainingService
    {
        readonly List<Assessment> _submitted = new();

        void Awake() => ARServices.Register((ITrainingService)this);

        public Task<TrainingModule> GetModule(string deviceType, Locale locale)
        {
            var m = new TrainingModule
            {
                device_type = deviceType,
                locale      = locale,
                hotspots = new List<Hotspot>
                {
                    new() { component_id = "tension_sensor", display_name = L(locale, "Tension sensor", "Capteur de tension", "مستشعر الشد"),     target_local_pos = new Vector3( 0.00f,  0.20f, 0f) },
                    new() { component_id = "heddle",         display_name = L(locale, "Heddle",         "Lisse",              "نول"),              target_local_pos = new Vector3( 0.12f,  0.05f, 0f) },
                    new() { component_id = "shuttle",        display_name = L(locale, "Shuttle",        "Navette",            "مكوك"),              target_local_pos = new Vector3( 0.18f, -0.05f, 0f) },
                },
                questions = new List<QuizQuestion>
                {
                    new() { question_id = "q1", prompt = L(locale, "Tap the tension sensor", "Appuie sur le capteur de tension", "اضغط على مستشعر الشد"), correct_hotspot_id = "tension_sensor" },
                    new() { question_id = "q2", prompt = L(locale, "Tap the shuttle",        "Appuie sur la navette",            "اضغط على المكوك"),        correct_hotspot_id = "shuttle" },
                }
            };
            return Task.FromResult(m);
        }

        public Task SubmitAssessment(Assessment a)
        {
            _submitted.Add(a);
            Debug.Log($"[MockTraining] {a.user_id} scored {a.score_percent}% on {a.device_type}");
            return Task.CompletedTask;
        }

        public Task<UserProgress> GetProgress(string userId)
        {
            var p = new UserProgress { user_id = userId };
            foreach (var a in _submitted)
                if (a.user_id == userId && a.score_percent >= 80)
                    p.certifications.Add(new CertifiedModule
                    {
                        device_type      = a.device_type,
                        score_percent    = a.score_percent,
                        completed_at_utc = DateTime.UtcNow,
                    });
            return Task.FromResult(p);
        }

        static string L(Locale l, string en, string fr, string ar) =>
            l switch { Locale.Fr => fr, Locale.Ar => ar, _ => en };
    }
}
