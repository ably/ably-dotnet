using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace IO.Ably.Realtime
{
    internal sealed class PresenceMap
    {
        private readonly object _lock = new object();
        private readonly ILogger _logger;
        private readonly string _channelName;
        private readonly ConcurrentDictionary<string, PresenceMessage> _members;

        private ICollection<string> _residualMembers;
        private bool _isSyncInProgress;

        public PresenceMap(string channelName, ILogger logger)
        {
            _logger = logger;
            _channelName = channelName;
            _members = new ConcurrentDictionary<string, PresenceMessage>();
        }

        internal event EventHandler SyncNoLongerInProgress;

        // Exposed internally to allow for testing.
        internal ConcurrentDictionary<string, PresenceMessage> Members => _members;

        public bool IsSyncInProgress
        {
            get => _isSyncInProgress;
            private set
            {
                lock (_lock)
                {
                    var previous = _isSyncInProgress;
                    _isSyncInProgress = value;

                    // if we have gone from true to false then fire SyncNoLongerInProgress
                    if (previous && !_isSyncInProgress)
                    {
                        OnSyncNoLongerInProgress();
                    }
                }
            }
        }

        public bool InitialSyncCompleted { get; private set; }

        public PresenceMessage[] Values
        {
            get
            {
                return _members.Values.Where(c => c.Action != PresenceAction.Absent)
                    .ToArray();
            }
        }

        public bool Put(PresenceMessage item)
        {
            lock (_lock)
            {
                // we've seen this member, so do not remove it at the end of sync
                _residualMembers?.Remove(item.MemberKey);
            }

            try
            {
                if (_members.TryGetValue(item.MemberKey, out var existingItem) && existingItem.IsNewerThan(item))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"PresenceMap.Put | Channel: {_channelName}, Error: {ex.Message}");
                throw;
            }

            switch (item.Action)
            {
                case PresenceAction.Enter:
                case PresenceAction.Update:
                    item = item.ShallowClone();
                    item.Action = PresenceAction.Present;
                    break;
            }

            _members[item.MemberKey] = item;

            return true;
        }

        public bool Remove(PresenceMessage item)
        {
            PresenceMessage existingItem;
            if (_members.TryGetValue(item.MemberKey, out existingItem) && existingItem.IsNewerThan(item))
            {
                return false;
            }

            _members.TryRemove(item.MemberKey, out PresenceMessage _);
            if (existingItem?.Action == PresenceAction.Absent)
            {
                return false;
            }

            return true;
        }

        public void StartSync()
        {
            if (_logger.IsDebug)
            {
                _logger.Debug($"StartSync | Channel: {_channelName}, SyncInProgress: {IsSyncInProgress}");
            }

            if (!IsSyncInProgress)
            {
                lock (_lock)
                {
                    _residualMembers = new HashSet<string>(_members.Keys);
                    IsSyncInProgress = true;
                }
            }
        }

        public PresenceMessage[] EndSync()
        {
            if (_logger.IsDebug)
            {
                _logger.Debug($"EndSync | Channel: {_channelName}, SyncInProgress: {IsSyncInProgress}");
            }

            List<PresenceMessage> removed = new List<PresenceMessage>();
            try
            {
                if (!IsSyncInProgress)
                {
                    return removed.ToArray();
                }

                // We can now strip out the ABSENT members, as we have
                // received all of the out-of-order sync messages
                foreach (var member in _members.ToArray())
                {
                    if (member.Value.Action == PresenceAction.Absent)
                    {
                        _members.TryRemove(member.Key, out PresenceMessage _);
                    }
                }

                lock (_lock)
                {
                    if (_residualMembers != null)
                    {
                        // Any members that were present at the start of the sync,
                        // and have not been seen in sync, can be removed
                        foreach (var member in _residualMembers)
                        {
                            if (_members.TryRemove(member, out PresenceMessage pm))
                            {
                                removed.Add(pm);
                            }
                        }

                        _residualMembers = null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"PresenceMap.EndSync | Channel: {_channelName}, Error: {ex.Message}");
                throw;
            }
            finally
            {
                lock (_lock)
                {
                    InitialSyncCompleted = true;
                    IsSyncInProgress = false;
                }
            }

            return removed.ToArray();
        }

        public void Clear()
        {
            lock (_lock)
            {
                _members?.Clear();
                _residualMembers?.Clear();
            }
        }

        internal JObject GetState()
        {
            var state = new JObject();
            state["channelName"] = _channelName;
            state["syncInProgress"] = _isSyncInProgress;
            state["initialSyncComplete"] = InitialSyncCompleted;
            state["members"] = new JArray(_members.Select(x => JObject.FromObject(new { Name = x.Key, Data = x.Value })));
            return state;
        }

        private void OnSyncNoLongerInProgress()
        {
            SyncNoLongerInProgress?.Invoke(this, EventArgs.Empty);
        }
    }
}
