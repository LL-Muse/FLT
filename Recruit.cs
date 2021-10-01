using System;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace FreelancerTemplate
{
    [Serializable]
    public class Recruit
    {
        [XmlAttribute]
        public string Id;
        public CharacterObject getCharacter()
        {
            return MBObjectManager.Instance.GetObject<CharacterObject>(Id);
        }
    }
}