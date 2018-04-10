using System.Collections.Generic;
using DDD.Core.Domain;

namespace DDD.Sessionize.Sync
{
    public class PresenterSync : IEqualityComparer<Presenter>
    {
        public bool Equals(Presenter x, Presenter y)
        {
            return x.ExternalId == y.ExternalId;
        }

        public int GetHashCode(Presenter obj)
        {
            return obj.ExternalId.GetHashCode();
        }
    }
}