using System.Collections.Generic;
using TaleWorlds.SaveSystem;
using TaleWorlds.CampaignSystem;

namespace FreelancerTemplate
{
    public class SaveDefiner : SaveableTypeDefiner
    {
        public SaveDefiner() : base(1436500012)
        {
        }

        protected override void DefineEnumTypes()
        {
            base.AddEnumDefinition(typeof(Test.Assignment), 1);
        }

        protected override void DefineClassTypes()
        {
        }

        protected override void DefineContainerDefinitions()
        {
        }
    }
}