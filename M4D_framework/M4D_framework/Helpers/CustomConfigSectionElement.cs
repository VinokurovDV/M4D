using System.Configuration;

namespace M4D_framework.Helpers
{
    public class CustomConfigSectionElement : ConfigurationElement
    {
        [ConfigurationProperty("Name", IsKey = false, IsRequired = true)]
        public string Name
        {
            get { return (string)this["Name"]; }
        }

        [ConfigurationProperty("ApiKey", IsKey = true, IsRequired = true)]
        public string ApiKey
        {
            get { return (string)this["ApiKey"]; }
        }
    }

    [ConfigurationCollection(typeof(CustomConfigSectionElement), AddItemName = "node")]
    public class CustomConfigSectionCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new CustomConfigSectionElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((CustomConfigSectionElement)element).Name;
        }
    }

    public class CustomConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("Clients", IsDefaultCollection = true)]
        public CustomConfigSectionCollection Clients
        {
            get { return (CustomConfigSectionCollection)this["Clients"]; }
        }
    }
}