namespace DnxMigrater.Other
{
    public interface ITemplateRenderer
    {
        string Render(string template, object model);
    }
}