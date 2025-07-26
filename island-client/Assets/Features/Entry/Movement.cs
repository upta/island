using System.Collections.Generic;
using UnityEngine;

public class TransformInterpolator
{
    public struct TransformState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public float timestamp;

        public TransformState(Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angVel, float time)
        {
            position = pos;
            rotation = rot;
            velocity = vel;
            angularVelocity = angVel;
            timestamp = time;
        }
    }

    public class TimeSync
    {
        private readonly Queue<float> offsetSamples;
        private readonly int maxSamples;
        private float averageOffset;
        private float lastUpdateTime;

        public float TimeOffset => averageOffset;
        public bool IsInitialized => offsetSamples.Count > 0;

        public TimeSync(int sampleCount = 10)
        {
            maxSamples = sampleCount;
            offsetSamples = new Queue<float>(maxSamples);
            averageOffset = 0f;
            lastUpdateTime = 0f;
        }

        /// <summary>
        /// Updates time sync with a new sender timestamp
        /// </summary>
        /// <param name="senderTime">Timestamp from sender</param>
        /// <param name="localReceiveTime">Local time when received</param>
        public void UpdateTimeSync(float senderTime, float localReceiveTime)
        {
            // Calculate offset (how much ahead/behind sender time is)
            float offset = senderTime - localReceiveTime;

            // Add new sample
            offsetSamples.Enqueue(offset);

            // Remove old samples if we exceed max
            while (offsetSamples.Count > maxSamples)
            {
                offsetSamples.Dequeue();
            }

            // Calculate running average
            float sum = 0f;
            foreach (float sample in offsetSamples)
            {
                sum += sample;
            }
            averageOffset = sum / offsetSamples.Count;

            lastUpdateTime = localReceiveTime;
        }

        /// <summary>
        /// Converts sender time to local time
        /// </summary>
        public float SenderToLocalTime(float senderTime)
        {
            return senderTime - averageOffset;
        }

        /// <summary>
        /// Converts local time to sender time
        /// </summary>
        public float LocalToSenderTime(float localTime)
        {
            return localTime + averageOffset;
        }

        /// <summary>
        /// Gets estimated current sender time
        /// </summary>
        public float GetCurrentSenderTime(float currentLocalTime)
        {
            return LocalToSenderTime(currentLocalTime);
        }
    }

    public class StateBuffer
    {
        private readonly List<TransformState> states;
        private readonly int maxBufferSize;
        private readonly float maxAge;
        private readonly TimeSync timeSync;
        private readonly float interpolationDelay;

        public int Count => states.Count;
        public bool IsEmpty => states.Count == 0;
        public TimeSync TimeSynchronizer => timeSync;

        /// <summary>
        /// Creates a new state buffer with time synchronization
        /// </summary>
        /// <param name="maxSize">Maximum number of states to keep</param>
        /// <param name="maxStateAge">Maximum age of states in seconds</param>
        /// <param name="interpDelay">Interpolation delay for smoother playback</param>
        /// <param name="timeSyncSamples">Number of samples for time sync averaging</param>
        public StateBuffer(
            int maxSize = 64,
            float maxStateAge = 5f,
            float interpDelay = 0.1f,
            int timeSyncSamples = 10
        )
        {
            maxBufferSize = maxSize;
            maxAge = maxStateAge;
            interpolationDelay = interpDelay;
            timeSync = new TimeSync(timeSyncSamples);
            states = new List<TransformState>(maxSize);
        }

        /// <summary>
        /// Adds a new state with sender timestamp, automatically handles time sync
        /// </summary>
        /// <param name="state">State with sender timestamp</param>
        /// <param name="localReceiveTime">Local time when this state was received</param>
        public void AddStateWithTimeSync(TransformState state, float localReceiveTime)
        {
            // Update time synchronization
            timeSync.UpdateTimeSync(state.timestamp, localReceiveTime);

            // Convert sender timestamp to local time for storage
            var localState = new TransformState(
                state.position,
                state.rotation,
                state.velocity,
                state.angularVelocity,
                timeSync.SenderToLocalTime(state.timestamp)
            );

            AddState(localState);
        }

        /// <summary>
        /// Adds a state that's already in local time
        /// </summary>
        public void AddState(TransformState state)
        {
            // Insert in chronological order
            int insertIndex = states.Count;
            for (int i = states.Count - 1; i >= 0; i--)
            {
                if (states[i].timestamp <= state.timestamp)
                {
                    insertIndex = i + 1;
                    break;
                }
                insertIndex = i;
            }

            states.Insert(insertIndex, state);

            // Remove old states based on buffer size
            while (states.Count > maxBufferSize)
            {
                states.RemoveAt(0);
            }

            // Remove old states based on age
            if (maxAge > 0f)
            {
                float cutoffTime = state.timestamp - maxAge;
                states.RemoveAll(s => s.timestamp < cutoffTime);
            }
        }

        /// <summary>
        /// Gets current interpolated state using automatic time reconciliation
        /// </summary>
        /// <param name="currentLocalTime">Current local time</param>
        /// <returns>Interpolated state</returns>
        public TransformState GetCurrentState(float currentLocalTime)
        {
            // Apply interpolation delay for smoother playback
            float targetTime = currentLocalTime - interpolationDelay;
            return GetStateAtTime(targetTime);
        }

        /// <summary>
        /// Gets interpolated/extrapolated state at a specific local time
        /// </summary>
        public TransformState GetStateAtTime(float targetTime)
        {
            if (states.Count == 0)
                return default;

            if (states.Count == 1)
            {
                float deltaTime = targetTime - states[0].timestamp;
                return deltaTime >= 0 ? Extrapolate(states[0], deltaTime) : states[0];
            }

            // Find the two states that bracket our target time
            TransformState beforeState = states[0];
            TransformState afterState = states[states.Count - 1];

            for (int i = 0; i < states.Count - 1; i++)
            {
                if (states[i].timestamp <= targetTime && states[i + 1].timestamp >= targetTime)
                {
                    beforeState = states[i];
                    afterState = states[i + 1];
                    break;
                }
            }

            // If target time is before all states, extrapolate backwards from earliest
            if (targetTime < states[0].timestamp)
            {
                float deltaTime = targetTime - states[0].timestamp;
                return Extrapolate(states[0], deltaTime);
            }

            // If target time is after all states, extrapolate forwards from latest
            if (targetTime > states[states.Count - 1].timestamp)
            {
                float deltaTime = targetTime - states[states.Count - 1].timestamp;
                return Extrapolate(states[states.Count - 1], deltaTime);
            }

            // Interpolate between the two bracketing states
            return InterpolateToTime(beforeState, afterState, targetTime);
        }

        /// <summary>
        /// Gets debug info about time synchronization
        /// </summary>
        public string GetTimeSyncInfo()
        {
            return $"Time Offset: {timeSync.TimeOffset:F3}s, Initialized: {timeSync.IsInitialized}";
        }

        public TransformState GetLatestState()
        {
            return states.Count > 0 ? states[states.Count - 1] : default;
        }

        public TransformState GetOldestState()
        {
            return states.Count > 0 ? states[0] : default;
        }

        public void Clear()
        {
            states.Clear();
        }

        public IReadOnlyList<TransformState> GetAllStates()
        {
            return states.AsReadOnly();
        }
    }

    // Static interpolation methods (unchanged)
    public static TransformState Interpolate(TransformState from, TransformState to, float t)
    {
        t = Mathf.Clamp01(t);

        return new TransformState(
            Vector3.Lerp(from.position, to.position, t),
            Quaternion.Slerp(from.rotation, to.rotation, t),
            Vector3.Lerp(from.velocity, to.velocity, t),
            Vector3.Lerp(from.angularVelocity, to.angularVelocity, t),
            Mathf.Lerp(from.timestamp, to.timestamp, t)
        );
    }

    public static TransformState Extrapolate(TransformState state, float deltaTime)
    {
        Vector3 newPosition = state.position + state.velocity * deltaTime;

        Quaternion deltaRotation =
            state.angularVelocity.magnitude > 0.001f
                ? Quaternion.AngleAxis(
                    state.angularVelocity.magnitude * deltaTime * Mathf.Rad2Deg,
                    state.angularVelocity.normalized
                )
                : Quaternion.identity;

        Quaternion newRotation = deltaRotation * state.rotation;

        return new TransformState(
            newPosition,
            newRotation,
            state.velocity,
            state.angularVelocity,
            state.timestamp + deltaTime
        );
    }

    public static TransformState InterpolateToTime(
        TransformState from,
        TransformState to,
        float targetTime
    )
    {
        float timeDiff = to.timestamp - from.timestamp;

        if (timeDiff <= 0f)
            return to;

        float t = (targetTime - from.timestamp) / timeDiff;

        if (t <= 1f)
        {
            return Interpolate(from, to, t);
        }
        else
        {
            float extrapolateTime = targetTime - to.timestamp;
            return Extrapolate(to, extrapolateTime);
        }
    }
}
