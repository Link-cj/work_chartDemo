using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace work_chartDemo.ulites
{
    class XMLUlites
    {
        public static XmlNode GetNode(string typeName)
        {
            string context = Application.StartupPath;        
            string pathXML = string.Concat(context, @"\config\config.xml");

            XmlDocument xml = new XmlDocument();

            try
            {
                xml.Load(pathXML);
                XmlNode firstNode = xml.SelectSingleNode("root/system");
                if (firstNode != null)
                {
                    foreach (XmlNode tempNode in firstNode.ChildNodes)
                    {
                        if (tempNode.Attributes["type"].Value == typeName)
                        {
                            return tempNode;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
                throw;
            }
            return null;
        }
    }
}
