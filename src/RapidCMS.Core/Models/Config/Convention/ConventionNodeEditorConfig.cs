using RapidCMS.Core.Abstractions.Config;
using RapidCMS.Core.Abstractions.Data;
using RapidCMS.Core.Enums;

namespace RapidCMS.Core.Models.Config.Convention
{
    internal class ConventionNodeEditorConfig<TEntity> : NodeConfig, IIsConventionBased
        where TEntity : IEntity
    {
        public ConventionNodeEditorConfig(bool allowsNodeEditing) : base(typeof(TEntity))
        {
            AllowsNodeEditing = allowsNodeEditing;
        }

        public bool AllowsNodeEditing { get; }

        public Features GetFeatures()
        {
            return AllowsNodeEditing ? Features.CanEdit : Features.None;
        }
    }
}
