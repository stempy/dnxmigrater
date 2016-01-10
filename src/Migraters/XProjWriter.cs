using System.IO;
using System.Text;
using System.Xml.Linq;
using DnxMigrater.Models.Dest;
using DnxMigrater.Other;

namespace DnxMigrater.Migraters
{
    public class XProjWriter
    {
        private string _xProjTemplate;
        private readonly ITemplateRenderer _templateRenderer;

        public XProjWriter(ITemplateRenderer templateRenderer)
        {
            _templateRenderer = templateRenderer;
        }

        public string CreateXProjString(projectXProjModel model)
        {
            if (string.IsNullOrEmpty(_xProjTemplate))
                _xProjTemplate = File.ReadAllText(@"project_xproj_template.xml");

            var xProj = _templateRenderer.Render(_xProjTemplate, model);

            var xDoc= XDocument.Parse(xProj);

            return "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\r\n" +  xDoc.ToString();
        }

        /// <summary>
        /// Write XProj file
        /// </summary>
        /// <param name="model"></param>
        /// <param name="filepath"></param>
        public void WriteXProjFile(projectXProjModel model, string filepath)
        {
            File.WriteAllText(filepath, CreateXProjString(model), Encoding.UTF8);
        }
    }
}