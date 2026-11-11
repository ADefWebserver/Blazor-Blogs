namespace BlazorBlogs;

public sealed class CampaignSendRegistry
{
    private readonly object _gate = new();
    private readonly HashSet<int> _activeCampaigns = [];

    public bool IsActive(int campaignId)
    {
        lock (_gate)
            return _activeCampaigns.Contains(campaignId);
    }

    public CampaignSendLease TryBegin(int campaignId)
    {
        lock (_gate)
        {
            if (!_activeCampaigns.Add(campaignId))
                return null;
        }

        return new CampaignSendLease(this, campaignId);
    }

    private void Release(int campaignId)
    {
        lock (_gate)
            _activeCampaigns.Remove(campaignId);
    }

    public sealed class CampaignSendLease : IDisposable
    {
        private readonly CampaignSendRegistry _owner;
        private readonly int _campaignId;
        private int _disposed;

        internal CampaignSendLease(CampaignSendRegistry owner, int campaignId)
        {
            _owner = owner;
            _campaignId = campaignId;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                _owner.Release(_campaignId);
        }
    }
}
