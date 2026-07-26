using System;
using System.Collections.Generic;
using System.Text;

namespace fintrack_netcore_app.Domain.Entities
{
    public class EntityBase
    {
        public string ItemId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreateDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime LastUpdateDate { get; set; }
        public int TimeZoneOffsetInMinutes { get; set; }
    }
}
