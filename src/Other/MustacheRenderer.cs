using Mustache;

namespace DnxMigrater.Other
{
    public class MustacheRenderer : ITemplateRenderer
    {
        public string Render(string template, object model)
        {
            FormatCompiler compiler = new FormatCompiler();
            Generator generator = compiler.Compile(template);
            string result = generator.Render(model);
            return result;  // Hello, Bob!!!
        }

    }
}