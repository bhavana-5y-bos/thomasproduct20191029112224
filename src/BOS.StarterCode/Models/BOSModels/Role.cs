using BOS.Auth.Client.ClientModels;
using System;

namespace BOS.StarterCode.Models.BOSModels
{
    public class Role : IRole
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Rank { get; set; }
        public bool IsDefault { get; set; }
        public bool Deleted { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset LastModifiedOn { get; set; }
    }
}
