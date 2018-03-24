using System.Collections.Generic;
using DDD.Core.Domain;

namespace DDD.SessionizeWorker.Sync
{
    public class SessionSync : IEqualityComparer<Session>
    {
        public bool Equals(Session x, Session y)
        {
            return x.ExternalId == y.ExternalId;
        }

        public int GetHashCode(Session obj)
        {
            return obj.ExternalId.GetHashCode();
        }
    }
}