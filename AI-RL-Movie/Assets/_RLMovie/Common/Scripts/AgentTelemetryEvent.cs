using System;
using UnityEngine;

namespace RLMovie.Common
{
    public enum AgentTelemetryEventType
    {
        EpisodeBegin,
        Reward,
        Success,
        Failure,
        Marker,
        Info
    }

    [Serializable]
    public struct AgentTelemetryEvent
    {
        public AgentTelemetryEvent(
            long sequenceId,
            int episodeNumber,
            float timestampSeconds,
            AgentTelemetryEventType eventType,
            string reason,
            string message,
            float value,
            Vector3 worldPosition)
        {
            SequenceId = sequenceId;
            EpisodeNumber = episodeNumber;
            TimestampSeconds = timestampSeconds;
            EventType = eventType;
            Reason = reason;
            Message = message;
            Value = value;
            WorldPosition = worldPosition;
        }

        public long SequenceId;
        public int EpisodeNumber;
        public float TimestampSeconds;
        public AgentTelemetryEventType EventType;
        public string Reason;
        public string Message;
        public float Value;
        public Vector3 WorldPosition;
    }
}
