using System;
using System.Collections.Generic;
using UnityEngine;
using Smartex.Core.Models;

namespace Smartex.AR.Contracts
{
    // ─── Recognition ──────────────────────────────────────────────────────────

    /// <summary>
    /// A machine that has been recognized in the AR camera feed.
    /// AnchorTransform is the Vuforia ImageTarget's transform — parent any AR
    /// content you want "stuck to this machine" under it, and you get Vuforia
    /// tracking for free.
    /// </summary>
    public class RecognizedMachine
    {
        public string      DeviceId        { get; }
        public Transform   AnchorTransform { get; }
        public MachineData Data            { get; }

        public RecognizedMachine(string deviceId, Transform anchor, MachineData data)
        {
            DeviceId        = deviceId;
            AnchorTransform = anchor;
            Data            = data;
        }
    }

    // ─── Maintenance ──────────────────────────────────────────────────────────

    [Serializable]
    public class ProcedureStep
    {
        public int     id;
        public string  text;
        /// <summary>Target-local offset where the callout should appear, in metres.</summary>
        public Vector3 hotspot_position;
        public string  image_url;   // optional reference photo
    }

    [Serializable]
    public class Procedure
    {
        public string              procedure_id;
        public string              device_id;
        public string              title;
        public int                 schema_version = 1;
        public List<ProcedureStep> steps = new();
    }

    [Serializable]
    public class MaintenanceLog
    {
        public string   device_id;
        public string   procedure_id;
        public string   user_id;
        public int[]    completed_steps;
        public DateTime completed_at_utc;
    }

    // ─── Remote assist ────────────────────────────────────────────────────────

    public class Session
    {
        public string SessionId  { get; set; }
        public string DeviceId   { get; set; }
        public string StunUrl    { get; set; }
        public string TurnUrl    { get; set; }
        public string TurnUser   { get; set; }
        public string TurnSecret { get; set; }
    }

    /// <summary>
    /// An annotation drawn by the remote expert.
    /// Position is TARGET-LOCAL, so consumers spawn it as a child of the
    /// machine's AnchorTransform and get tracking for free.
    /// </summary>
    public class Annotation
    {
        public string  AnnotationId  { get; set; }
        public string  DeviceId      { get; set; }
        public Vector3 LocalPosition { get; set; }
        public Color   Color         { get; set; } = Color.yellow;
        public string  Label         { get; set; } = "";
    }

    // ─── Training ─────────────────────────────────────────────────────────────

    public enum Locale { En, Fr, Ar }

    [Serializable]
    public class Hotspot
    {
        public string  component_id;
        public string  display_name;       // already localized
        public Vector3 target_local_pos;   // in metres
    }

    [Serializable]
    public class QuizQuestion
    {
        public string   question_id;
        public string   prompt;            // already localized
        public string   correct_hotspot_id;
    }

    [Serializable]
    public class TrainingModule
    {
        public string             device_type;     // "loom", "dyer", "spinner"
        public Locale             locale;
        public List<Hotspot>      hotspots  = new();
        public List<QuizQuestion> questions = new();
    }

    [Serializable]
    public class Assessment
    {
        public string user_id;
        public string device_type;
        public int    score_percent;
        public int    duration_seconds;
    }

    [Serializable]
    public class UserProgress
    {
        public string                user_id;
        public List<CertifiedModule> certifications = new();
    }

    [Serializable]
    public class CertifiedModule
    {
        public string   device_type;
        public int      score_percent;
        public DateTime completed_at_utc;
    }
}
